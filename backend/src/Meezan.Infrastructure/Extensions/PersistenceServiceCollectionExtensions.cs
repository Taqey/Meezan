using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Persistence.Queries.RemovalCandidates;
using Meezan.Infrastructure.Persistence.Queries.Stocks;
using Meezan.Infrastructure.Persistence.Repositories.Common;
using Meezan.Infrastructure.Persistence.Repositories.Indices;
using Meezan.Infrastructure.Persistence.Repositories.Sectors;
using Meezan.Infrastructure.Persistence.Repositories.Stocks;
using Meezan.Infrastructure.Persistence.Repositories.SystemSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Meezan.Infrastructure.Extensions;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructurePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IIndexRepository, IndexRepository>();
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<ISectorRepository, SectorRepository>();
        services.AddScoped<IStockManagementReadQuery, StockManagementReadQuery>();
        services.AddScoped<IRemovalCandidatesReadQuery, RemovalCandidatesReadQuery>();
        services.AddScoped<IUploadHistoryRepository, UploadHistoryRepository>();
        services.AddScoped<IJobSettingRepository, JobSettingRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
