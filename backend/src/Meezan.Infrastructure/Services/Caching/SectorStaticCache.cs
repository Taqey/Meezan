using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Meezan.Application.Common.Interfaces.Caching;
using Meezan.Application.Common.Models.Caching;
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
/// Static sector data (Id, NameAr, NameEn): L1 memory + L2 Redis, single-flight, fail-open.
/// Only complete, non-empty lists are stored; cancelled or failed builds store nothing.
/// </summary>
public sealed class SectorStaticCache : ISectorStaticCache
{
    private const string SchemaVersion = "v1";
    private static readonly TimeSpan L1Ttl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan L2Ttl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan FailureCooldown = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RedisCallTimeout = TimeSpan.FromMilliseconds(500);

    private readonly IServiceScopeFactory _scopes;
    private readonly IMemoryCache _l1;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<SectorStaticCache> _logger;
    private readonly string _prefix;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTime _failedUntilUtc = DateTime.MinValue;
    private DateTime _redisDownUntilUtc = DateTime.MinValue;

    public SectorStaticCache(
        IServiceScopeFactory scopes,
        IMemoryCache memoryCache,
        IOptions<RedisOptions> redisOptions,
        IConfiguration configuration,
        ILogger<SectorStaticCache> logger,
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

    public async Task<List<SectorStaticEntry>> GetAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var l1Key = $"{_prefix}:L1:sectors_static";
        if (_l1.TryGetValue(l1Key, out List<SectorStaticEntry>? hit) && hit is { Count: > 0 }) return hit;

        await _gate.WaitAsync(ct);
        try
        {
            if (_l1.TryGetValue(l1Key, out hit) && hit is { Count: > 0 }) return hit;

            if (_failedUntilUtc > DateTime.UtcNow)
                return await BuildAsync(ct);

            var fromL2 = await TryReadL2Async(ct);
            if (fromL2 is { Count: > 0 })
            {
                _l1.Set(l1Key, fromL2, L1Ttl);
                return fromL2;
            }

            List<SectorStaticEntry> built;
            try
            {
                built = await BuildAsync(ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _failedUntilUtc = DateTime.UtcNow + FailureCooldown;
                _logger.LogWarning(ex, "Sector static build failed; direct SQL for {Seconds}s.", FailureCooldown.TotalSeconds);
                throw;
            }

            if (built is { Count: > 0 })
            {
                _l1.Set(l1Key, built, L1Ttl);
                await TryWriteL2Async(built, ct);
            }
            return built;
        }
        finally
        {
            _gate.Release();
        }
    }

    private string L2Key => $"{_prefix}:{SchemaVersion}:sectors_static";

    private bool RedisUsable => _redis is { IsConnected: true } && DateTime.UtcNow >= _redisDownUntilUtc;

    private async Task<List<SectorStaticEntry>?> TryReadL2Async(CancellationToken ct)
    {
        if (!RedisUsable) return null;
        try
        {
            var raw = await _redis!.GetDatabase().StringGetAsync(L2Key).WaitAsync(RedisCallTimeout, ct);
            if (raw.IsNullOrEmpty) return null;
            return JsonSerializer.Deserialize<List<SectorStaticEntry>>(raw.ToString());
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _redisDownUntilUtc = DateTime.UtcNow + FailureCooldown;
            _logger.LogWarning(ex, "Redis read failed for sector static cache; fail-open.");
            return null;
        }
    }

    private async Task TryWriteL2Async(List<SectorStaticEntry> list, CancellationToken ct)
    {
        if (!RedisUsable) return;
        try
        {
            var json = JsonSerializer.Serialize(list);
            await _redis!.GetDatabase().StringSetAsync(L2Key, json, L2Ttl).WaitAsync(RedisCallTimeout, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _redisDownUntilUtc = DateTime.UtcNow + FailureCooldown;
            _logger.LogWarning(ex, "Redis write failed for sector static cache; fail-open.");
        }
    }

    private async Task<List<SectorStaticEntry>> BuildAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await db.Sectors.AsNoTracking()
            .Select(s => new SectorStaticEntry
            {
                Id = s.Id,
                NameAr = s.NameAr,
                NameEn = s.NameEn
            })
            .ToListAsync(ct);
    }
}
