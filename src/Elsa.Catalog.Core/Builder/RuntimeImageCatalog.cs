namespace Elsa.Catalog.Core.Builder;

public sealed class RuntimeImageCatalog
{
    private static readonly IReadOnlyList<RuntimeImage> Images =
    [
        new(
            "elsa-pro-server",
            "Elsa Professional Server",
            "Professional Elsa Server runtime.",
            "elsaworkflows/elsa-pro-server",
            ["latest"],
            "latest",
            8080,
            8080,
            "elsa-pro-server",
            false,
            false,
            ["server"],
            [
                new("ASPNETCORE_ENVIRONMENT", "Environment", false, false, "Development", "Runtime", false)
            ],
            new(true, true, false, null)),
        new(
            "elsa-pro-studio",
            "Elsa Professional Studio",
            "Professional Elsa Studio runtime.",
            "elsaworkflows/elsa-pro-studio",
            ["latest"],
            "latest",
            8080,
            8081,
            "elsa-pro-studio",
            true,
            true,
            ["studio"],
            [
                new("Backend__Url", "Backend URL", false, false, "http://elsa-pro-server:8080", "Runtime", false)
            ],
            new(true, true, true, "elsa-pro-server")),
        new(
            "elsa-pro-combined",
            "Elsa Professional Combined",
            "Combined Elsa Server and Studio runtime for simple deployments.",
            "elsaworkflows/elsa-pro-combined",
            ["latest"],
            "latest",
            8080,
            8080,
            "elsa-pro-combined",
            false,
            false,
            ["server", "studio"],
            [
                new("ASPNETCORE_ENVIRONMENT", "Environment", false, false, "Development", "Runtime", false),
                new("Backend__Url", "Backend URL", false, false, "http://localhost:8080", "Runtime", false)
            ],
            new(true, true, false, null))
    ];

    public IReadOnlyList<RuntimeImage> ListImages() => Images;

    public RuntimeImage? Find(string slug) =>
        Images.FirstOrDefault(x => string.Equals(x.Slug, slug, StringComparison.OrdinalIgnoreCase));
}

public sealed record RuntimeImage(
    string Slug,
    string DisplayName,
    string Description,
    string Image,
    IReadOnlyList<string> AvailableTags,
    string DefaultTag,
    int DefaultPort,
    int HostPort,
    string ContainerName,
    bool NeedsSharedNetwork,
    bool RequiresServer,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<RuntimeImageEnvironmentVariable> EnvVars,
    RuntimeImageDeploymentHints DeploymentHints);

public sealed record RuntimeImageEnvironmentVariable(
    string Name,
    string DisplayName,
    bool Required,
    bool Secret,
    string? DefaultValue,
    string Group,
    bool Advanced);

public sealed record RuntimeImageDeploymentHints(
    bool SupportsDockerCompose,
    bool SupportsKubernetes,
    bool RequiresCompanionServer,
    string? CompanionImageSlug);
