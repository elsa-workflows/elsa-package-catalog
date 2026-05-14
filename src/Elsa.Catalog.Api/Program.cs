using Elsa.Catalog.Api.Authentication;
using Elsa.Catalog.Api.Admin.Sources;
using Elsa.Catalog.Api.Admin.Sync;
using Elsa.Catalog.Api.Public.Features;
using Elsa.Catalog.Api.Public.Packages;
using Elsa.Catalog.Core.Manifests;
using Elsa.Catalog.Core.Packaging;
using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Persistence;
using Elsa.Catalog.Core.Sources;
using Elsa.Catalog.Core.Sync;
using Elsa.Catalog.Packaging.NuGet;
using Elsa.Catalog.Persistence.EntityFrameworkCore;
using Elsa.PackageManifests.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddAuthentication(ApiKeyAuthenticationDefaults.Scheme)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationDefaults.Scheme, _ => { });
builder.Services.AddCatalogAuthorization();
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Catalog") ?? "Data Source=elsa-catalog.db"));
builder.Services.AddScoped<ICatalogStore, EfCoreCatalogStore>();
builder.Services.AddScoped<IPublicCatalogQueries, PublicCatalogQueries>();
builder.Services.AddScoped<PublicCatalogQueryService>();
builder.Services.AddScoped<IPackageSourceStore, PackageSourceStore>();
builder.Services.AddScoped<PackageSourceService>();
builder.Services.AddScoped<ISyncCatalogStore, SyncCatalogStore>();
builder.Services.AddScoped<ISyncRunStore, SyncRunStore>();
builder.Services.AddScoped<IPackageVersionDiscoveryClient, NuGetPackageSourceClient>();
builder.Services.AddScoped<IPackageArchiveDownloader, NuGetSyncPackageDownloader>();
builder.Services.AddScoped<IPackageArchiveManifestReader, PackageArchiveManifestReader>();
builder.Services.AddScoped<ManifestIngestionService>();
builder.Services.AddScoped<PackageSyncService>();
builder.Services.AddSingleton<PackageSourceValidator>();
builder.Services.AddSingleton<PackageSourcePatternMatcher>();
builder.Services.AddSingleton<ManifestValidator>();
builder.Services.AddSingleton<SyncConcurrencyGuard>();
builder.Services.AddSingleton<PublicCatalogVisibilityPolicy>();
builder.Services.AddSingleton<PackageVersionPolicy>();
builder.Services.AddSingleton<ISyncDiagnostics, NoopSyncDiagnostics>();
builder.Services.AddHostedService<ScheduledSyncHostedService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/", () => "Elsa Package Catalog");
app.MapPublicPackageEndpoints();
app.MapPublicFeatureEndpoints();
app.MapAdminSourceEndpoints();
app.MapAdminSyncEndpoints();

app.Run();

public partial class Program;
