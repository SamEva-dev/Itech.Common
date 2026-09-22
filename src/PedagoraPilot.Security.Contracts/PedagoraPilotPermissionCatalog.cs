using Itech.Security.Contracts.Authorization;

namespace PedagoraPilot.Security.Contracts;

public static class PedagoraPilotPermissionCatalog
{
    public static IReadOnlyList<PermissionDefinition> All { get; } =
        PermissionCatalogFactory.Create(
            PedagoraPilotApplication.Code,
            PedagoraPilotPermissionCodes.All);
}
