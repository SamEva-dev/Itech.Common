using Itech.Security.Contracts.Applications;

namespace Itech.Security.Contracts.Authorization;

public sealed record RoleDefinition
{
    public RoleDefinition(
        ApplicationCode applicationCode,
        string code,
        RoleScope scope = RoleScope.Organization,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        ApplicationCode = applicationCode;
        Code = code.Trim();
        Scope = scope;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public ApplicationCode ApplicationCode { get; }
    public string Code { get; }
    public RoleScope Scope { get; }
    public string? Description { get; }
}
