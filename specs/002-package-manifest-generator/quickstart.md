# Quickstart: Elsa Package Manifest Generator

This quickstart describes how an implementer should validate the first usable
slice once tasks are generated and implemented.

## Prerequisites

- .NET 10 SDK.
- Local shell with `dotnet`.
- A sample class library project that produces a NuGet package.

## Build The Solution

```bash
dotnet restore
dotnet build
```

Expected result:

- Generator projects build.
- Existing catalog and manifest contract tests still compile.

## Run Tests

```bash
dotnet test
```

Expected coverage areas:

- Metadata-only assembly inspection.
- Feature discovery through configured base/interface type names.
- Explicit source-only annotations.
- Setting discovery and exclusions.
- XML documentation enrichment.
- Override file merge and validation.
- JSON Schema Draft 2020-12 setting schema generation.
- Manifest validation through `Elsa.PackageManifests`.
- Pack integration and package inspection.
- Deterministic output across repeated builds.
- Safety checks proving constructors and property getters are not invoked.

## Create A Sample Package Project

Create or use a fixture class library that references the generator:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <PackageId>Elsa.Samples.EmailFeature</PackageId>
    <Version>1.0.0</Version>
    <Title>Elsa Sample Email Feature</Title>
    <Description>Sample package for manifest generator validation.</Description>
    <Authors>Elsa</Authors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Elsa.PackageManifest.Generator" Version="x.y.z" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

## Add A Feature Class

```csharp
using Elsa.PackageManifest.Generator.Annotations;

[ElsaFeature(
    Id = "Elsa.Samples.Email",
    DisplayName = "Email",
    Category = "Communication",
    Description = "Adds email delivery support.")]
public sealed class EmailFeature : Feature
{
    /// <summary>
    /// SMTP server host name.
    /// </summary>
    [FeatureSetting(
        DisplayName = "SMTP host",
        EnvironmentVariable = "ELSA_EMAIL_SMTP_HOST",
        RestartRequired = true)]
    public string? SmtpHost { get; set; }
}
```

Expected result:

- Annotation attributes compile from source-only assets.
- The consuming package does not emit a runtime dependency for annotations.

## Build The Sample

```bash
dotnet build
```

Expected result:

- `obj/{configuration}/{targetframework}/elsa-package.json` is generated.
- Build diagnostics include the generated path and discovered feature count.
- No feature constructors or property getters are invoked.

## Pack The Sample

```bash
dotnet pack
```

Expected result:

- The produced `.nupkg` contains exactly one root `elsa-package.json`.
- The manifest uses the `Elsa.PackageManifests` contract.
- The manifest is no larger than 1 MB.

## Validate Override Behavior

Add `elsa-package.overrides.json`:

```json
{
  "package": {
    "documentation": {
      "url": "https://docs.example.com/elsa/email"
    },
    "tags": ["email", "communication"]
  },
  "features": [
    {
      "id": "Elsa.Samples.Email",
      "settings": [
        {
          "name": "SmtpHost",
          "required": true,
          "uiHint": "text"
        }
      ]
    }
  ]
}
```

Expected result:

- Override values win over inferred, XML, and attribute metadata.
- Override file references resolve to discovered features and settings.
- Override files larger than 256 KB fail validation.
- Override package ID/version conflicts fail validation.

## Validate Multi-Targeting

Change the fixture to:

```xml
<TargetFrameworks>net10.0;net8.0</TargetFrameworks>
```

Expected result:

- Equivalent feature surfaces produce one canonical package manifest.
- Divergent feature or setting surfaces warn or fail according to configured
  severity.
- The `.nupkg` still contains one root `elsa-package.json` by default.

## Disable Generation

```xml
<GenerateElsaPackageManifest>false</GenerateElsaPackageManifest>
```

Expected result:

- No manifest is generated.
- No manifest is included in the package.

## Inspect Generated Package

Use any NuGet package inspection method to verify:

```text
elsa-package.json
```

Expected result:

- The file is at the package root.
- The manifest package ID/version match the NuGet package ID/version.
- Feature and setting metadata are deterministic across repeated builds.
