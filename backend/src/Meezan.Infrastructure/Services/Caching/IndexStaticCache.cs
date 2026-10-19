using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Meezan.Application.Common.Interfaces.Caching;
using Meezan.Application.Common.Models.Caching;
using Meezan.Domain.Enums;
using Meezan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Meezan.Infrastructure.Services.Caching;

/// <summary>
/// Static index data (index header + constituents, no prices): L1 memory + L2 Redis, one entry per
/// index code, single-flight per key, fail-open. Only complete, non-empty entries are stored; a
/// cancelled or failed build stores nothing. A failed build puts the key on a 30 s cooldown during
/// which requests use the direct (uncached) SQL build.
/// </summary>
public sealed class IndexStaticCache : IIndexStaticCache
{
    private const string SchemaVersion = "v1";
    private static readonly TimeSpan L1Ttl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan L2Ttl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan FailureCooldown = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RedisCallTimeout = TimeSpan.FromMilliseconds(500);

    private readonly IServiceScopeFactory _scopes;
    private readonly IMemoryCache _l1;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<IndexStaticCache> _logger;
    private readonly string _prefix;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly ConcurrentDictionary<string, DateTime> _failedUntil = new();
    private DateTime _redisDownUntilUtc = DateTime.MinValue;

    public IndexStaticCache(
        IServiceScopeFactory scopes,
        IMemoryCache memoryCache,
        IOptions<RedisOptions> redisOptions,
        IConfiguration configuration,
        ILogger<IndexStaticCache> logger,
        IConnectionMultiplexer? redis = null)
    {
        _scopes = scopes;
        _l1 = memoryCache;
        _redis = redis;
        _logger = logger;

        var keyPrefix = redisOptions.Value.KeyPrefix;
        if (!string.IsNullOrWhiteSpace(keyPrefix))
        {
            _prefix = keyPrefix.TrimEnd(':');
        }
        else
        {
            var connStr = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(connStr))).Substring(0, 12).ToLowerInvariant();
            _prefix = $"meezan:{hash}";
        }
    }

    public async Task<IndexStaticEntry?> GetAsync(string indexCode, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var key = (indexCode ?? string.Empty).Trim().ToUpperInvariant();
        var l1Key = $"{_prefix}:L1:index_static:{key}";
        if (_l1.TryGetValue(l1Key, out IndexStaticEntry? hit) && IsComplete(hit)) return hit;

        var gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            if (_l1.TryGetValue(l1Key, out hit) && IsComplete(hit)) return hit;

            // Cooldown after a failed build: go straight to SQL, no cache interaction.
            if (_failedUntil.TryGetValue(key, out var until) && until > DateTime.UtcNow)
                return await BuildAsync(key, ct);

            var fromL2 = await TryReadL2Async(key, ct);
            if (IsComplete(fromL2))
            {
                _l1.Set(l1Key, fromL2, L1Ttl);
                return fromL2;
            }

            IndexStaticEntry? built;
            try
            {
                built = await BuildAsync(key, ct);
            }
            catch (OperationCanceledException)
            {
                throw; // cancelled build: nothing stored, no cooldown
            }
            catch (Exception ex)
            {
                _failedUntil[key] = DateTime.UtcNow + FailureCooldown;
                _logger.LogWarning(ex, "Index static build failed for {Index}; direct SQL for {Seconds}s.", key, FailureCooldown.TotalSeconds);
                throw;
            }

            if (IsComplete(built))
            {
                _l1.Set(l1Key, built, L1Ttl);
                await TryWriteL2Async(key, built!, ct);
            }
            return built;
        }
        finally
        {
            gate.Release();
        }
    }

    private static bool IsComplete(IndexStaticEntry? e) => e is { Complete: true } && e.Rows.Count > 0;

    private string L2Key(string key) => $"{_prefix}:{SchemaVersion}:index_static:{key}";

    private bool RedisUsable => _redis is { IsConnected: true } && DateTime.UtcNow >= _redisDownUntilUtc;

    private async Task<IndexStaticEntry?> TryReadL2Async(string key, CancellationToken ct)
    {
        if (!RedisUsable) return null;
        try
        {
            var raw = await _redis!.GetDatabase().StringGetAsync(L2Key(key)).WaitAsync(RedisCallTimeout, ct);
            if (raw.IsNullOrEmpty) return null;
            return JsonSerializer.Deserialize<IndexStaticEntry>(raw.ToString());
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _redisDownUntilUtc = DateTime.UtcNow + FailureCooldown;
            _logger.LogWarning(ex, "Redis read failed for index static cache; fail-open.");
            return null;
        }
    }

    private async Task TryWriteL2Async(string key, IndexStaticEntry entry, CancellationToken ct)
    {
        if (!RedisUsable) return;
        try
        {
            var json = JsonSerializer.Serialize(entry);
            await _redis!.GetDatabase().StringSetAsync(L2Key(key), json, L2Ttl).WaitAsync(RedisCallTimeout, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _redisDownUntilUtc = DateTime.UtcNow + FailureCooldown;
            _logger.LogWarning(ex, "Redis write failed for index static cache; fail-open.");
        }
    }

    /// <summary>Set-based build: exactly 3 SQL commands, whatever the constituent count.</summary>
    private async Task<IndexStaticEntry?> BuildAsync(string upperCode, CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var idx = await db.Indices.AsNoTracking()
            .Where(i => i.Code.ToUpper() == upperCode)
            .Select(i => new { i.Id, i.Code, i.NameAr, i.NameEn, i.LastUpdated })
            .FirstOrDefaultAsync(ct);
        if (idx == null) return null;

        var rows = await db.IndexConstituents.AsNoTracking()
            .Where(ic => ic.IndexId == idx.Id && ic.Stock != null && ic.Stock.DataStatus == StockDataStatus.Active)
            .Select(ic => new IndexStaticRow
            {
                StockId = ic.StockId,
                Ticker = ic.Stock!.Ticker,
                NameAr = ic.Stock.NameAr,
                NameEn = ic.Stock.NameEn,
                SectorNameAr = ic.Stock.Sector != null ? ic.Stock.Sector.NameAr : null,
                Weight = ic.Weight
            })
            .ToListAsync(ct);

        var codePairs = await db.IndexConstituents.AsNoTracking()
            .Where(c => c.Index != null
                && db.IndexConstituents.Any(x => x.IndexId == idx.Id && x.StockId == c.StockId))
            .Select(c => new { c.StockId, Code = c.Index!.Code })
            .ToListAsync(ct);

        ct.ThrowIfCancellationRequested();
        var codesByStock = codePairs.ToLookup(p => p.StockId, p => p.Code);
        foreach (var r in rows) r.IndexCodes = codesByStock[r.StockId].Distinct().ToList();

        return new IndexStaticEntry
        {
            IndexId = idx.Id,
            Code = idx.Code,
            NameAr = idx.NameAr,
            NameEn = idx.NameEn,
            LastUpdated = idx.LastUpdated,
            Rows = rows,
            Complete = true
        };
    }
}
