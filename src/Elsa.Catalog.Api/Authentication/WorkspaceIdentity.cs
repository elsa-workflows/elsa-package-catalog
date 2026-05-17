using Elsa.Catalog.Core.Accounts;

namespace Elsa.Catalog.Api.Authentication;

public interface IWorkspaceIdentityReader
{
    TrustedWorkspaceIdentity? Read(HttpContext context);
}

public sealed class TrustedHeaderWorkspaceIdentityReader(IConfiguration configuration) : IWorkspaceIdentityReader
{
    public const string EnabledConfigurationKey = "Authentication:WorkspaceTrustedHeaders:Enabled";
    public const string IssuerHeader = "X-Catalog-Identity-Issuer";
    public const string SubjectHeader = "X-Catalog-Identity-Subject";
    public const string EmailHeader = "X-Catalog-Identity-Email";
    public const string NameHeader = "X-Catalog-Identity-Name";

    public TrustedWorkspaceIdentity? Read(HttpContext context)
    {
        if (!configuration.GetValue<bool>(EnabledConfigurationKey))
            return null;

        var request = context.Request;
        var issuer = request.Headers[IssuerHeader].FirstOrDefault();
        var subject = request.Headers[SubjectHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subject))
            return null;

        return new TrustedWorkspaceIdentity(
            issuer,
            subject,
            request.Headers[NameHeader].FirstOrDefault(),
            request.Headers[EmailHeader].FirstOrDefault());
    }
}

public static class WorkspaceIdentityHttpContextExtensions
{
    public static IResult UnauthorizedWorkspaceIdentity() =>
        Results.Problem(
            title: "Trusted workspace identity is required.",
            statusCode: StatusCodes.Status401Unauthorized);
}
