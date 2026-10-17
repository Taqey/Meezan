using Meezan.Application.Common.Interfaces.Caching;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Infrastructure.Persistence.Queries.Funds;
using Meezan.Infrastructure.Services.Caching;
using Meezan.Infrastructure.Services.Funds.Queries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Meezan.Infrastructure.Extensions;

public static class CachingServiceCollectionExtensions
{
    public static IServiceCollection AddCachingServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));

        var redisSection = configuration.GetSection(RedisOptions.SectionName);
        var host = redisSection["Host"];
        var portStr = redisSection["Port"];
        int.TryParse(portStr, out var port);
        if (port <= 0) port = 6379;
        var password = redisSection["Password"];
        var user = redisSection["User"] ?? "default";
        var ssl = bool.TryParse(redisSection["Ssl"], out var s) && s;

        if (!string.IsNullOrWhiteSpace(host))
        {
            try
            {
                var redisConfig = new ConfigurationOptions
                {
                    EndPoints = { { host, port } },
                    User = user,
                    Password = password,
                    Ssl = ssl,
                    AbortOnConnectFail = false,
                    ConnectTimeout = 3000,
                    SyncTimeout = 2000,
                    AsyncTimeout = 2000,
                    KeepAlive = 60
                };

                var multiplexer = ConnectionMultiplexer.Connect(redisConfig);
                services.AddSingleton<IConnectionMultiplexer>(multiplexer);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to initialize Redis connection at startup: {ex.Message}. Continuing with L1 memory cache only.");
            }
        }

        services.AddSingleton<IFundCacheService, FundCacheService>();
        services.AddHostedService<CacheWarmupHostedService>();

        // Wire price cache invalidation to real cache
        services.AddScoped<IFundPriceCacheInvalidator, FundPriceCacheInvalidator>();

        // Decorators for zero SQL on warm cache
        services.AddScoped<FundReadQuery>();
        services.AddScoped<IFundReadQuery>(sp =>
            new CachedFundReadQueryDecorator(
                sp.GetRequiredService<FundReadQuery>(),
                sp.GetRequiredService<IFundCacheService>()));

        services.AddScoped<FundDetailQuery>();
        services.AddScoped<IFundDetailQuery>(sp =>
            new CachedFundDetailQueryDecorator(
                sp.GetRequiredService<FundDetailQuery>(),
                sp.GetRequiredService<IFundCacheService>()));

        return services;
    }
}
