using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Meezan.Infrastructure.Tests.Testing;

public class DependencyInjectionComparisonTests
{
    private static readonly string BaselineFilePath = Path.Combine(AppContext.BaseDirectory, "Testing", "di-descriptors-baseline.txt");

    [Fact]
    public void DumpOrCompareServiceDescriptors()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=Test;Trusted_Connection=True;",
                ["Funds:Logos:RootPath"] = "C:\\test\\logos",
                ["Funds:Logos:PublicBaseUrl"] = "https://test.local/logos",
                ["Funds:Logos:MaxBytes"] = "1048576"
            })
            .Build();

        services.AddInfrastructure(configuration);

        var descriptors = services
            .Select(d => $"{d.Lifetime}|{FormatType(d.ServiceType)}|{FormatType(d.ImplementationType)}|{d.ImplementationInstance != null}|{d.ImplementationFactory != null}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        var dump = string.Join(Environment.NewLine, descriptors);

        // Also write to an easily accessible path in repo if needed
        var repoTestingDir = Path.Combine(FindSolutionDirectory() ?? AppContext.BaseDirectory, "tests", "Meezan.Infrastructure.Tests", "Testing");
        var repoBaselinePath = Path.Combine(repoTestingDir, "di-descriptors-baseline.txt");

        if (!File.Exists(repoBaselinePath))
        {
            File.WriteAllText(repoBaselinePath, dump);
            Assert.True(true, "Baseline dumped to " + repoBaselinePath);
            return;
        }

        var baseline = File.ReadAllText(repoBaselinePath).Trim();
        Assert.Equal(baseline, dump.Trim());
    }

    [Fact]
    public void HostStarts_And_HostedServicesResolve()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();
        Assert.NotNull(client);

        using var scope = factory.Services.CreateScope();
        var hostedServices = scope.ServiceProvider.GetServices<IHostedService>().ToList();
        Assert.NotEmpty(hostedServices);
        // Includes the 5 Infrastructure background services plus any host infrastructure background services
        Assert.Equal(7, hostedServices.Count);
    }

    private static string FormatType(Type? type)
    {
        if (type == null) return "null";
        if (type.IsGenericType)
        {
            var genArgs = string.Join(",", type.GetGenericArguments().Select(FormatType));
            return $"{type.Name}[{genArgs}]";
        }
        return type.Name;
    }

    private static string? FindSolutionDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Meezan.slnx")) ||
                File.Exists(Path.Combine(current.FullName, "Meezan.sln")))
            {
                return current.FullName;
            }
            if (Directory.Exists(Path.Combine(current.FullName, "backend")) &&
                (File.Exists(Path.Combine(current.FullName, "backend", "Meezan.slnx")) ||
                 File.Exists(Path.Combine(current.FullName, "backend", "Meezan.sln"))))
            {
                return Path.Combine(current.FullName, "backend");
            }
            current = current.Parent;
        }
        return null;
    }
}
