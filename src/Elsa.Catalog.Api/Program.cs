using System.Text.Json.Serialization;
using Elsa.Catalog.Api.Admin.Application;
using Elsa.Catalog.Api.Authentication;
using Elsa.Catalog.Api.Admin.Packages;
using Elsa.Catalog.Api.Admin.Sources;
using Elsa.Catalog.Api.Admin.Sync;
using Elsa.Catalog.Api.Public.Builder;
using Elsa.Catalog.Api.Public.Compatibility;
using Elsa.Catalog.Api.Public.Features;
using Elsa.Catalog.Api.Public.Packages;
using Elsa.Catalog.Core.Approvals;
using Elsa.Catalog.Core.Builder;
using Elsa.Catalog.Core.Compatibility;
using Elsa.Catalog.Core.Manifests;
using Elsa.Catalog.Core.Packaging;
using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Persistence;
using Elsa.Catalog.Core.Sources;
using Elsa.Catalog.Core.Sync;
using Elsa.Catalog.Packaging.NuGet;
using Elsa.Catalog.Persistence.EntityFrameworkCore;
using Elsa.PackageManifests.Validation;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
});
builder.Services.AddAuthentication(ApiKeyAuthenticationDefaults.Scheme)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationDefaults.Scheme, _ => { })
    .AddCookie(AdminDashboardAuthenticationDefaults.Scheme, options =>
    {
        options.Cookie.Name = AdminDashboardAuthenticationDefaults.CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.Path = "/";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsEnvironment("Testing")
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = AdminDashboardAuthenticationDefaults.SessionLifetime;
        options.LoginPath = AdminDashboardAuthenticationDefaults.LoginPath;
        options.LogoutPath = AdminDashboardAuthenticationDefaults.LogoutPath;
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });
builder.Services.AddCatalogAuthorization();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<AdminApiKeyValidator>();
builder.Services.AddSingleton<AdminDashboardLoginThrottle>();
builder.Services.AddCatalogDbContext(builder.Configuration);
builder.Services.AddScoped<ICatalogStore, EfCoreCatalogStore>();
builder.Services.AddScoped<IPublicCatalogQueries, PublicCatalogQueries>();
builder.Services.AddScoped<PublicCatalogQueryService>();
builder.Services.AddScoped<IPackageSourceStore, PackageSourceStore>();
builder.Services.AddScoped<PackageSourceService>();
builder.Services.AddScoped<ISyncCatalogStore, SyncCatalogStore>();
builder.Services.AddScoped<ISyncRunStore, SyncRunStore>();
builder.Services.AddScoped<IApprovalStore, ApprovalStore>();
builder.Services.AddScoped<ICompatibilityQueries, CompatibilityQueries>();
builder.Services.AddScoped<ApprovalService>();
builder.Services.AddScoped<CompatibilityCheckService>();
builder.Services.AddScoped<IPackageVersionDiscoveryClient, NuGetPackageSourceClient>();
builder.Services.AddScoped<IPackageArchiveDownloader, NuGetSyncPackageDownloader>();
builder.Services.AddScoped<IPackageArchiveManifestReader, PackageArchiveManifestReader>();
builder.Services.AddScoped<ManifestIngestionService>();
builder.Services.AddScoped<PackageSyncService>();
builder.Services.AddScoped<SyncRunCleanupService>();
builder.Services.AddSingleton<PackageSourceValidator>();
builder.Services.AddSingleton<PackageSourcePatternMatcher>();
builder.Services.AddSingleton<ManifestValidator>();
builder.Services.AddSingleton<ApprovalPolicy>();
builder.Services.AddSingleton<VersionRangeEvaluator>();
builder.Services.AddSingleton<InfrastructureProviderCatalog>();
builder.Services.AddSingleton<SyncConcurrencyGuard>();
builder.Services.AddSingleton<SourceSyncActivityTracker>();
builder.Services.AddSingleton<SyncRunCancellationRegistry>();
builder.Services.AddSingleton<ManualSyncQueue>();
builder.Services.AddSingleton<PublicCatalogVisibilityPolicy>();
builder.Services.AddSingleton<PackageVersionPolicy>();
builder.Services.AddSingleton<ISyncDiagnostics, NoopSyncDiagnostics>();
builder.Services.AddHostedService<ManualSyncHostedService>();
builder.Services.AddHostedService<ScheduledSyncHostedService>();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAdminDashboardAuthentication();
app.UseAdminDashboardRequestForgeryGuard();
app.UseStaticFiles();
app.UseAuthorization();

app.MapOpenApi();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/", () => "Elsa Package Catalog");
app.MapGet("/admin", () => Results.Redirect("/admin/overview"));
app.MapAdminDashboardAuthEndpoints();
app.MapPublicPackageEndpoints();
app.MapPublicFeatureEndpoints();
app.MapBuilderEndpoints();
app.MapCompatibilityEndpoints();
app.MapAdminApplicationEndpoints();
app.MapAdminSourceEndpoints();
app.MapAdminSyncEndpoints();
app.MapAdminPackageEndpoints();
app.MapAdminApprovalEndpoints();
app.MapAdminValidationEndpoints();
app.MapFallbackToFile("/admin/{*path:nonfile}", "admin/index.html");

app.Run();

[UsedImplicitly]
public partial class Program;
