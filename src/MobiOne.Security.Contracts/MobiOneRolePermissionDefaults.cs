namespace MobiOne.Security.Contracts;

/// <summary>
/// Default MobiOne role-to-permission matrix published to AuthGate.
/// </summary>
public static class MobiOneRolePermissionDefaults
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> Matrix =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal)
        {
            [MobiOneRoleCodes.PlatformAdministrator] = MobiOnePermissionCodes.All,
            [MobiOneRoleCodes.FleetAdministrator] =
            [
                MobiOnePermissionCodes.Organizations.Read,
                MobiOnePermissionCodes.Organizations.SitesRead,
                MobiOnePermissionCodes.Organizations.SitesManage,
                MobiOnePermissionCodes.Access.UsersRead,
                MobiOnePermissionCodes.Access.UsersManage,
                MobiOnePermissionCodes.Access.RolesRead,
                .. MobiOnePermissionCodes.Services.All,
                .. MobiOnePermissionCodes.Drivers.All,
                .. MobiOnePermissionCodes.Pricing.All,
                .. MobiOnePermissionCodes.Missions.All
            ],
            [MobiOneRoleCodes.BusinessAdministrator] =
            [
                MobiOnePermissionCodes.Organizations.Read,
                MobiOnePermissionCodes.Organizations.SitesRead,
                MobiOnePermissionCodes.Organizations.SitesManage,
                MobiOnePermissionCodes.Access.UsersRead,
                MobiOnePermissionCodes.Access.UsersManage,
                MobiOnePermissionCodes.Access.RolesRead,
                .. MobiOnePermissionCodes.Services.All,
                .. MobiOnePermissionCodes.Drivers.All,
                .. MobiOnePermissionCodes.Pricing.All,
                .. MobiOnePermissionCodes.Missions.All
            ],
            [MobiOneRoleCodes.EstablishmentAdministrator] =
            [
                MobiOnePermissionCodes.Organizations.Read,
                MobiOnePermissionCodes.Organizations.SitesRead,
                MobiOnePermissionCodes.Organizations.SitesManage,
                MobiOnePermissionCodes.Access.UsersRead,
                MobiOnePermissionCodes.Access.UsersManage,
                MobiOnePermissionCodes.Access.RolesRead,
                .. MobiOnePermissionCodes.Services.All,
                .. MobiOnePermissionCodes.Drivers.All,
                .. MobiOnePermissionCodes.Pricing.All,
                .. MobiOnePermissionCodes.Missions.All
            ],
            [MobiOneRoleCodes.Dispatcher] =
            [
                MobiOnePermissionCodes.Drivers.Read,
                MobiOnePermissionCodes.Drivers.FleetsRead,
                MobiOnePermissionCodes.Drivers.VehiclesRead,
                MobiOnePermissionCodes.Drivers.DocumentsRead,
                MobiOnePermissionCodes.Pricing.Read,
                .. MobiOnePermissionCodes.Missions.All
            ],
            [MobiOneRoleCodes.SupportAgent] =
            [
                MobiOnePermissionCodes.Access.UsersRead,
                MobiOnePermissionCodes.Access.RolesRead,
                MobiOnePermissionCodes.Drivers.Read,
                MobiOnePermissionCodes.Drivers.DocumentsRead,
                MobiOnePermissionCodes.Missions.Read
            ],
            [MobiOneRoleCodes.Driver] = [],
            [MobiOneRoleCodes.Customer] = []
        };

    public static bool TryGetPermissions(string roleCode, out IReadOnlyCollection<string> permissions)
        => Matrix.TryGetValue(roleCode, out permissions!);

    public static IReadOnlyCollection<string> GetPermissions(string roleCode)
        => Matrix.TryGetValue(roleCode, out var permissions) ? permissions : [];
}
