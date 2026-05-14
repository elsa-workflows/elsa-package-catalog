using System.Text.Json;
using Elsa.Catalog.Core.Packages;
using Elsa.PackageManifests;

namespace Elsa.Catalog.Core.Manifests;

public sealed class ManifestIngestionService
{
    public IngestedManifest Ingest(PackageVersion packageVersion, string manifestJson)
    {
        var manifest = JsonSerializer.Deserialize<ElsaPackageManifest>(manifestJson, ManifestJsonSerializerOptions.Default)
            ?? throw new InvalidOperationException("Manifest JSON did not produce a manifest.");

        packageVersion.SchemaVersion = manifest.SchemaVersion;
        packageVersion.Features.Clear();

        foreach (var feature in manifest.Features)
        {
            var featureRecord = new FeatureRecord
            {
                PackageVersion = packageVersion,
                PackageVersionId = packageVersion.Id,
                FeatureId = feature.Id,
                TypeName = feature.TypeName,
                DisplayName = feature.DisplayName,
                Description = feature.Description,
                Category = feature.Category,
                Advanced = feature.Advanced,
                Experimental = feature.Experimental,
                RequiredCapabilitiesJson = JsonSerializer.Serialize(feature.RequiredCapabilities),
                DependenciesJson = JsonSerializer.Serialize(feature.Dependencies, ManifestJsonSerializerOptions.Default),
                ConflictsJson = JsonSerializer.Serialize(feature.Conflicts, ManifestJsonSerializerOptions.Default),
                ExtensionsJson = JsonSerializer.Serialize(feature.Extensions, ManifestJsonSerializerOptions.Default)
            };

            foreach (var setting in feature.Settings)
            {
                featureRecord.Settings.Add(new FeatureSettingRecord
                {
                    FeatureRecord = featureRecord,
                    FeatureRecordId = featureRecord.Id,
                    Name = setting.Name,
                    ClrType = setting.ClrType,
                    JsonType = setting.JsonType,
                    Required = setting.Required,
                    DefaultValueJson = setting.DefaultValue is null ? null : JsonSerializer.Serialize(setting.DefaultValue, ManifestJsonSerializerOptions.Default),
                    DisplayName = setting.DisplayName,
                    Description = setting.Description,
                    Category = setting.Category,
                    ValidationJson = JsonSerializer.Serialize(setting.Validation, ManifestJsonSerializerOptions.Default),
                    Secret = setting.Secret,
                    RestartRequired = setting.RestartRequired,
                    EnvironmentVariable = setting.EnvironmentVariable,
                    UiJson = JsonSerializer.Serialize(setting.Ui, ManifestJsonSerializerOptions.Default),
                    ExtensionsJson = JsonSerializer.Serialize(setting.Extensions, ManifestJsonSerializerOptions.Default)
                });
            }

            packageVersion.Features.Add(featureRecord);
        }

        return new IngestedManifest(manifest);
    }
}

public sealed record IngestedManifest(ElsaPackageManifest Manifest);
