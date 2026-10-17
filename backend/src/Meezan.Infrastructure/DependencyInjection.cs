using Meezan.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Meezan.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructurePersistence(configuration);
        services.AddParsingServices();
        services.AddShariahServices();
        services.AddScrapingServices();
        services.AddFundServices(configuration);

        return services;
    }
}
