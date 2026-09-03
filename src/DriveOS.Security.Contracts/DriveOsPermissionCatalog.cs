using Itech.Security.Contracts.Authorization;

namespace DriveOS.Security.Contracts;

/// <summary>
/// Complete permission catalog owned and versioned by DriveOS.
/// Identity providers and administration tools may mirror this catalog but
/// must not redefine its codes.
/// </summary>
public static class DriveOsPermissionCatalog
{
    public static IReadOnlyList<PermissionDefinition> All { get; } =
        PermissionCatalogFactory.Create(DriveOsApplication.Code, DriveOsPermissionCodes.All);
}
