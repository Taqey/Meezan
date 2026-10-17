using System.Reflection;
using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Market;
using Meezan.Application.Common.Interfaces.Repositories.Scraping;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Interfaces.Security;
using Meezan.Application.Common.Interfaces.Storage;
using Meezan.WebApi.Controllers;
using NetArchTest.Rules;
using Xunit;

namespace Meezan.Infrastructure.Tests.Testing;

public class ArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(Meezan.Domain.Entities.Stock).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Meezan.Application.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Meezan.Infrastructure.Persistence.ApplicationDbContext).Assembly;
    private static readonly Assembly WebApiAssembly = typeof(StocksController).Assembly;

    [Fact]
    public void Application_ShouldNotReference_Infrastructure_Or_WebApi()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Meezan.Infrastructure", "Meezan.WebApi", "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void Domain_ShouldNotReference_OtherLayers_Or_Frameworks()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Meezan.Application", "Meezan.Infrastructure", "Meezan.WebApi", "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void MigratedControllers_ShouldNotReference_DbContext_Repositories_Or_Infrastructure()
    {
        var allowListPath = Path.Combine(AppContext.BaseDirectory, "Testing", "unmigrated-controllers.txt");
        if (!File.Exists(allowListPath))
        {
            allowListPath = Path.Combine(Directory.GetCurrentDirectory(), "Testing", "unmigrated-controllers.txt");
        }

        var unmigrated = File.Exists(allowListPath)
            ? File.ReadAllLines(allowListPath)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("#"))
                .ToHashSet()
            : new HashSet<string>();

        var controllerTypes = Types.InAssembly(WebApiAssembly)
            .That()
            .Inherit(typeof(Microsoft.AspNetCore.Mvc.ControllerBase))
            .GetTypes()
            .Where(t => !unmigrated.Contains(t.Name))
            .ToList();

        // Enforce boundary rules on all controllers that are NOT on the allow-list
        foreach (var controller in controllerTypes)
        {
            var dependencies = Types.InAssembly(WebApiAssembly)
                .That()
                .HaveName(controller.Name)
                .ShouldNot()
                .HaveDependencyOnAny(
                    "Meezan.Infrastructure",
                    "Microsoft.EntityFrameworkCore",
                    "Meezan.Application.Common.Interfaces.IUnitOfWork"
                )
                .GetResult();

            Assert.True(dependencies.IsSuccessful,
                $"Migrated controller {controller.Name} has forbidden dependencies: " +
                string.Join(", ", dependencies.FailingTypeNames ?? Array.Empty<string>()));
        }
    }
}
