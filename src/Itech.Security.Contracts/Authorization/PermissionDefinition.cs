using Itech.Security.Contracts.Applications;

namespace Itech.Security.Contracts.Authorization;

public sealed record PermissionDefinition
{
    public PermissionDefinition(
        ApplicationCode applicationCode,
        string code,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        ApplicationCode = applicationCode;
        Code = code.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public ApplicationCode ApplicationCode { get; }
    public string Code { get; }
    public string? Description { get; }
}
