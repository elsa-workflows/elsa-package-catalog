using System.Text.Json;
using Elsa.PackageManifests;

namespace Elsa.Catalog.Testing;

public sealed class ManifestFixtureBuilder
{
    private string _packageId = "Elsa.Email";
    private string _version = "1.0.0";
    private string _displayName = "Email";
    private readonly List<FeatureManifest> _features = [];

    public ManifestFixtureBuilder WithPackage(string packageId, string version)
    {
        _packageId = packageId;
        _version = version;
        return this;
    }

    public ManifestFixtureBuilder WithDisplayName(string displayName)
    {
        _displayName = displayName;
        return this;
    }

    public ManifestFixtureBuilder WithFeature(string featureId = "email", string typeName = "Elsa.Email.EmailFeature")
    {
        _features.Add(new FeatureManifest
        {
            Id = featureId,
            TypeName = typeName,
            DisplayName = "Email",
            Description = "Adds email activities and services.",
            Category = "Communication"
        });

        return this;
    }

    public ElsaPackageManifest Build() => new()
    {
        SchemaVersion = ManifestSchemaVersions.Current,
        Package = new PackageIdentityManifest { Id = _packageId, Version = _version },
        DisplayName = _displayName,
        Features = _features
    };

    public string BuildJson() => JsonSerializer.Serialize(Build(), ManifestJsonSerializerOptions.Default);
}
