using Itech.Security.Contracts.Authorization;

namespace DriveOS.Security.Contracts;

/// <summary>
/// Complete DriveOS permission catalog consumed by AuthGate.
/// </summary>
public static class DriveOsPermissionCatalog
{
    public static IReadOnlyList<PermissionDefinition> All { get; } =
        PermissionCatalogFactory.Create(DriveOsApplication.Code, DriveOsPermissionCodes.All);
}
