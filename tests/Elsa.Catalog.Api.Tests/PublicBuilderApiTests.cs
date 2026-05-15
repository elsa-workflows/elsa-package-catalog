using System.Net.Http.Json;
using Elsa.Catalog.Api.Public.Builder;
using Elsa.Catalog.Core.Manifests;
using Elsa.Catalog.Testing;
using FluentAssertions;

namespace Elsa.Catalog.Api.Tests;

public sealed class PublicBuilderApiTests
{
    [Fact]
    public async Task Get_builder_catalog_returns_package_provenance_and_infrastructure()
    {
        await using var app = new CatalogApiTestApplication();
        await app.SeedAsync(db =>
        {
            var source = PublicCatalogSeedData.CreatePackageSource();
            var package = PublicCatalogSeedData.CreatePackage(source, "Elsa.RabbitMq");
            var version = PublicCatalogSeedData.AddVersion(package);
            PublicCatalogSeedData.AddFeature(version, "rabbitmq-messaging", "RabbitMQ Messaging");
            version.Features[0].InfrastructureJson = """
            [
              {
                "id": "message-broker",
                "kind": "message-broker",
                "providers": ["rabbitmq"],
                "configurationKeys": ["RabbitMq:ConnectionString"]
              }
            ]
            """;

            db.PackageSources.Add(source);
            return Task.CompletedTask;
        });

        var catalog = await app.CreateClient().GetFromJsonAsync<BuilderCatalogResponse>("/api/builder/catalog");

        catalog.Should().NotBeNull();
        var package = catalog!.Packages.Should().ContainSingle(x => x.PackageId == "Elsa.RabbitMq").Subject;
        package.Source.Name.Should().Be("Test NuGet");
        var feature = package.Versions.Single().Features.Should().ContainSingle(x => x.FeatureId == "rabbitmq-messaging").Subject;
        feature.Infrastructure.Should().ContainSingle(x => x.Kind == "message-broker" && x.ConfigurationKeys.Contains("RabbitMq:ConnectionString"));
        catalog.InfrastructureProviders.Should().Contain(x => x.Kind == "message-broker" && x.Provider == "rabbitmq");
    }
}
