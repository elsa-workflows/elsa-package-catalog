using Elsa.Catalog.Api.Authentication;
using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Persistence;
using Elsa.Catalog.Core.Sync;
using Elsa.Catalog.Persistence.EntityFrameworkCore;
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
builder.Services.AddSingleton<PublicCatalogVisibilityPolicy>();
builder.Services.AddSingleton<PackageVersionPolicy>();
builder.Services.AddSingleton<ISyncDiagnostics, NoopSyncDiagnostics>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/", () => "Elsa Package Catalog");

app.Run();

public partial class Program;
