namespace LocaGuest.Security.Contracts;

public static class LocaGuestRoleCodes
{
    public const string TenantOwner = "TenantOwner";
    public const string TenantAdmin = "TenantAdmin";
    public const string TenantManager = "TenantManager";
    public const string TenantUser = "TenantUser";
    public const string ReadOnly = "ReadOnly";

    public const string Occupant = "Occupant";
    public const string OccupantAdmin = "OccupantAdmin";
    public const string OccupantOwner = "OccupantOwner";

    public static readonly string[] All =
    [
        TenantOwner,
        TenantAdmin,
        TenantManager,
        TenantUser,
        ReadOnly,
        Occupant,
        OccupantAdmin,
        OccupantOwner
    ];

    public static readonly string[] AdminRoles =
    [
        TenantOwner,
        TenantAdmin
    ];

    public static readonly string[] OperationalRoles =
    [
        TenantOwner,
        TenantAdmin,
        TenantManager
    ];
}
