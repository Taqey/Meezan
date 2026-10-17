using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Infrastructure.Services.Excel;
using Microsoft.Extensions.DependencyInjection;

namespace Meezan.Infrastructure.Extensions;

public static class ParsingServiceCollectionExtensions
{
    public static IServiceCollection AddParsingServices(this IServiceCollection services)
    {
        services.AddScoped<IExcelParserService, ExcelParserService>();
        return services;
    }
}
