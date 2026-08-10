using Itech.Security.Contracts.Authorization;

namespace LocaGuest.Security.Contracts;

/// <summary>
/// Complete LocaGuest permission catalog consumed by AuthGate.
/// </summary>
public static class LocaGuestPermissionCatalog
{
    public static IReadOnlyList<PermissionDefinition> All { get; } =
        PermissionCatalogFactory.Create(LocaGuestApplication.Code, LocaGuestPermissionCodes.All);
}
