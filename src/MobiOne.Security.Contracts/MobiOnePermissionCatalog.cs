using Itech.Security.Contracts.Authorization;

namespace MobiOne.Security.Contracts;

/// <summary>
/// Product-owned permission catalog. IAM adapters mirror this catalog; they do not redefine it.
/// </summary>
public static class MobiOnePermissionCatalog
{
    public static IReadOnlyList<PermissionDefinition> All { get; } =
        PermissionCatalogFactory.Create(MobiOneApplication.Code, MobiOnePermissionCodes.All);
}
