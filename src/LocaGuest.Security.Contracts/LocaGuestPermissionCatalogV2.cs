using Itech.Security.Contracts.Authorization;

namespace LocaGuest.Security.Contracts;

public static class LocaGuestPermissionCatalogV2
{
    public static IReadOnlyList<PermissionDefinition> All { get; } =
        Array.AsReadOnly(PermissionCatalogFactory.Create(LocaGuestApplication.Code, LocaGuestPermissionCodesV2.All).ToArray());
}
