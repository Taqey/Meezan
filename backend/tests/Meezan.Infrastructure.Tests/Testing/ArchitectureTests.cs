using Meezan.WebApi.Controllers.Public;
using NetArchTest.Rules;
using System.Reflection;

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

    [Fact]
    public void Namespaces_In_Application_Infrastructure_WebApi_Should_Match_Folder_Path()
    {
        var assemblies = new[]
        {
            (Assembly: ApplicationAssembly, ProjectFolder: "Meezan.Application"),
            (Assembly: InfrastructureAssembly, ProjectFolder: "Meezan.Infrastructure"),
            (Assembly: WebApiAssembly, ProjectFolder: "Meezan.WebApi")
        };

        var solutionDir = FindSolutionDirectory();
        Assert.NotNull(solutionDir);

        var violations = new List<string>();

        foreach (var (assembly, projectName) in assemblies)
        {
            var projectDir = Path.Combine(solutionDir, "src", projectName);
            var csFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) &&
                            !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) &&
                            !f.EndsWith("AssemblyInfo.cs") &&
                            !f.EndsWith(".AssemblyAttributes.cs"))
                .ToList();

            foreach (var file in csFiles)
            {
                var relativePath = Path.GetRelativePath(projectDir, file);
                var folderName = Path.GetDirectoryName(relativePath);

                // Skip special folders like Migrations or Snapshots if any in Infrastructure
                if (folderName != null && (folderName.StartsWith("Migrations") || folderName.StartsWith("Snapshots")))
                    continue;

                var expectedNamespace = string.IsNullOrEmpty(folderName)
                    ? projectName
                    : projectName + "." + folderName.Replace(Path.DirectorySeparatorChar, '.');

                var lines = File.ReadAllLines(file);
                var namespaceLine = lines.FirstOrDefault(l =>
                    l.TrimStart().StartsWith("namespace ") && !l.TrimStart().StartsWith("namespace ;"));

                if (namespaceLine == null) continue;

                var actualNamespace = namespaceLine.Trim().Substring("namespace ".Length).TrimEnd(';').Trim();
                if (actualNamespace != expectedNamespace)
                {
                    violations.Add($"{relativePath}: expected '{expectedNamespace}', got '{actualNamespace}'");
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} namespace/folder mismatches:{Environment.NewLine}" +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void No_Reorganized_Folder_Directly_Contains_More_Than_15_Files()
    {
        var solutionDir = FindSolutionDirectory();
        Assert.NotNull(solutionDir);

        // Folders targeted and reorganized by the reorganization specification
        var targetFoldersToCheck = new[]
        {
            Path.Combine(solutionDir, "src", "Meezan.Domain", "Entities"),
            Path.Combine(solutionDir, "src", "Meezan.Domain", "Enums"),
            Path.Combine(solutionDir, "src", "Meezan.WebApi", "Controllers"),
            Path.Combine(solutionDir, "src", "Meezan.Application", "Common", "Services"),
            Path.Combine(solutionDir, "src", "Meezan.Application", "Common", "Models"),
            Path.Combine(solutionDir, "src", "Meezan.Application", "Common", "Interfaces", "Integrations"),
            Path.Combine(solutionDir, "src", "Meezan.Application", "Common", "Interfaces", "Persistence"),
            Path.Combine(solutionDir, "src", "Meezan.Application", "Common", "Interfaces", "Queries"),
            Path.Combine(solutionDir, "src", "Meezan.Application", "Common", "Interfaces", "Security"),
            Path.Combine(solutionDir, "src", "Meezan.Application", "Common", "Interfaces", "Storage"),
            Path.Combine(solutionDir, "src", "Meezan.Application", "Features", "Funds"),
            Path.Combine(solutionDir, "src", "Meezan.Infrastructure", "Services"),
            Path.Combine(solutionDir, "src", "Meezan.Infrastructure", "Persistence", "Configurations"),
            Path.Combine(solutionDir, "src", "Meezan.Infrastructure", "Persistence", "Repositories"),
            Path.Combine(solutionDir, "src", "Meezan.Infrastructure", "Persistence", "Queries"),
            Path.Combine(solutionDir, "tests", "Meezan.Infrastructure.Tests")
        };

        var violations = new List<string>();

        foreach (var targetRoot in targetFoldersToCheck)
        {
            if (!Directory.Exists(targetRoot)) continue;

            var allDirs = Directory.GetDirectories(targetRoot, "*", SearchOption.AllDirectories)
                .Append(targetRoot)
                .Where(d => !d.Contains(Path.DirectorySeparatorChar + "bin") &&
                            !d.Contains(Path.DirectorySeparatorChar + "obj") &&
                            !d.Contains(Path.DirectorySeparatorChar + ".git") &&
                            !d.Contains(Path.DirectorySeparatorChar + "Migrations") &&
                            !d.Contains(Path.DirectorySeparatorChar + "Snapshots"))
                .ToList();

            foreach (var dir in allDirs)
            {
                var fileCount = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly)
                    .Count(f => !f.EndsWith(".csproj") && !f.EndsWith(".user") && !f.EndsWith(".slnx"));

                if (fileCount > 15)
                {
                    var relativeDir = Path.GetRelativePath(solutionDir, dir);
                    violations.Add($"{relativeDir} contains {fileCount} files (max 15 allowed)");
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"Folders exceeding 15 files limit:{Environment.NewLine}" +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void All_Application_Requests_Must_Return_Result_Or_ResultOfT()
    {
        var requestTypes = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(MediatR.IBaseRequest))
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        var violations = new List<string>();

        foreach (var req in requestTypes)
        {
            var irequestInterface = req.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(MediatR.IRequest<>));

            if (irequestInterface == null)
            {
                violations.Add($"{req.FullName} does not implement IRequest<TResponse>");
                continue;
            }

            var responseType = irequestInterface.GetGenericArguments()[0];
            bool isResult = responseType == typeof(Meezan.Application.Common.Results.Result) ||
                            (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Meezan.Application.Common.Results.Result<>));

            if (!isResult)
            {
                violations.Add($"{req.FullName} returns {responseType.Name} instead of Result or Result<T>");
            }
        }

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} requests not returning Result/Result<T>:{Environment.NewLine}" +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Application_Handlers_Must_Not_Throw_Forbidden_Exceptions()
    {
        var solutionDir = FindSolutionDirectory();
        Assert.NotNull(solutionDir);

        var featuresDir = Path.Combine(solutionDir, "src", "Meezan.Application", "Features");
        var handlerFiles = Directory.GetFiles(featuresDir, "*Handler.cs", SearchOption.AllDirectories);

        var forbiddenPatterns = new[]
        {
            "throw new NotFoundException",
            "throw new BadRequestException",
            "throw new ConflictException",
            "throw new UnauthorizedException",
            "throw new ForbiddenException",
            "throw new KeyNotFoundException",
            "throw new ArgumentException"
        };

        var violations = new List<string>();

        foreach (var file in handlerFiles)
        {
            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                foreach (var pattern in forbiddenPatterns)
                {
                    if (line.Contains(pattern))
                    {
                        var relativePath = Path.GetRelativePath(solutionDir, file);
                        violations.Add($"{relativePath}:{i + 1} uses forbidden '{pattern}'");
                    }
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} forbidden exception throws in Application handlers:{Environment.NewLine}" +
            string.Join(Environment.NewLine, violations));
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
