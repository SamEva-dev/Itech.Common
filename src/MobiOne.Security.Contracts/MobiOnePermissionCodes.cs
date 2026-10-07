namespace MobiOne.Security.Contracts;

/// <summary>
/// Stable permission codes owned by MobiOne and shared with AuthGate and clients.
/// Existing values must never be renamed after publication.
/// </summary>
public static class MobiOnePermissionCodes
{
    public static class Organizations
    {
        public const string Read = "organizations.read";
        public const string Manage = "organizations.manage";
        public const string SitesRead = "sites.read";
        public const string SitesManage = "sites.manage";
        public static readonly string[] All = [Read, Manage, SitesRead, SitesManage];
    }

    public static class Access
    {
        public const string UsersRead = "users.read";
        public const string UsersManage = "users.manage";
        public const string RolesRead = "roles.read";
        public const string RolesManage = "roles.manage";
        public static readonly string[] All = [UsersRead, UsersManage, RolesRead, RolesManage];
    }

    public static class Services
    {
        public const string Read = "services.read";
        public const string Manage = "services.manage";
        public static readonly string[] All = [Read, Manage];
    }

    public static class Drivers
    {
        public const string Read = "drivers.read";
        public const string Manage = "drivers.manage";
        public const string FleetsRead = "fleets.read";
        public const string FleetsManage = "fleets.manage";
        public const string VehiclesRead = "vehicles.read";
        public const string VehiclesManage = "vehicles.manage";
        public const string DocumentsRead = "documents.read";
        public const string DocumentsReview = "documents.review";
        public static readonly string[] All =
        [
            Read, Manage, FleetsRead, FleetsManage,
            VehiclesRead, VehiclesManage, DocumentsRead, DocumentsReview
        ];
    }

    public static class Pricing
    {
        public const string Read = "pricing.read";
        public const string Manage = "pricing.manage";
        public static readonly string[] All = [Read, Manage];
    }

    public static class Missions
    {
        public const string Read = "missions.read";
        public const string Manage = "missions.manage";
        public static readonly string[] All = [Read, Manage];
    }

    public static readonly string[] All =
    [
        .. Organizations.All,
        .. Access.All,
        .. Services.All,
        .. Drivers.All,
        .. Pricing.All,
        .. Missions.All
    ];
}
