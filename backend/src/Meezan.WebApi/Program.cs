using Meezan.Application;
using Meezan.Infrastructure;
using Meezan.Infrastructure.Persistence;
using Meezan.WebApi.Auth;
using Meezan.WebApi.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

// Add Clean Architecture Layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Meezan.Application.Common.Interfaces.Security.ICurrentUser, CurrentUser>();

// Add Controllers with Unicode-safe JSON (Arabic chars must NOT be escaped to \uXXXX)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Encoder =
            JavaScriptEncoder.Create(UnicodeRanges.All);
    });

// Admin authentication: shared-secret API key (X-Admin-Key header or Bearer token).
// Without this every /api/admin/* route was world-readable — see AdminFundsController.
builder.Services.AddAuthentication(AdminApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, AdminApiKeyAuthenticationHandler>(
        AdminApiKeyAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();

// Add CORS for Next.js frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                    "http://localhost:3000", "https://localhost:3000",
                    "http://localhost:4200", "https://localhost:4200",
                    "https://meezan-beta-six.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
// Add Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Meezan API",
        Version = "v1",
        Description = "Meezan - Egyptian Stock Exchange indices & stocks tracker backend"
    });
});

var app = builder.Build();

// Check for pending migrations on startup
if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Database:SkipMigrationCheck"))
{
    // Skipped in test/isolated environments
}
else
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (dbContext.Database.IsRelational())
        {
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Database has {Count} pending migration(s): {Migrations}",
                    pendingMigrations.Count(), string.Join(", ", pendingMigrations));
                if (!app.Environment.IsDevelopment())
                {
                    throw new InvalidOperationException($"Database has {pendingMigrations.Count()} pending migration(s). Apply them before starting in production.");
                }
            }
        }
    }
}

// Global Exception Handling Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure HTTP pipeline
if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Meezan API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowFrontend");

// Serve locally downloaded fund logos so production does not depend on Supabase.
var logoRoot = builder.Configuration["Funds:Logos:RootPath"];
if (!string.IsNullOrWhiteSpace(logoRoot))
{
    // LocalLogoPath is stored as "funds/<name>", so serve the funds subfolder
    // directly: /fund-logos/<name>  ->  <root>/funds/<name>
    var rootFull = Path.GetFullPath(logoRoot);
    var serveDir = Path.Combine(rootFull, "funds");
    if (Directory.Exists(serveDir))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(serveDir),
            RequestPath = "/fund-logos",
            ServeUnknownFileTypes = false,
            ContentTypeProvider = ImageContentTypeProvider.Instance,
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers.CacheControl = "public, max-age=86400";
                ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            }
        });
    }
    else
    {
        app.Logger.LogWarning("Funds logo folder '{Path}' does not exist — logos will not be served.", serveDir);
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }

/// <summary>Serves ONLY real image types from /fund-logos (never HTML/SVG-executed content).</summary>
internal sealed class ImageContentTypeProvider : FileExtensionContentTypeProvider
{
    public static readonly ImageContentTypeProvider Instance = new();

    public ImageContentTypeProvider()
    {
        // Remove every mapping, then allow only the five raster formats we store.
        foreach (var ext in Mappings.Keys.ToList())
        {
            if (ext is ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif") continue;
            Mappings.Remove(ext);
        }
        Mappings[".svg"] = "text/plain";
    }
}
