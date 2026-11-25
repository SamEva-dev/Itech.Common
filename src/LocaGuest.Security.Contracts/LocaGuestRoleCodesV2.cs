namespace LocaGuest.Security.Contracts;

public static class LocaGuestRoleCodesV2
{
    public const string TenantOwner = "TenantOwner";
    public const string TenantAdmin = "TenantAdmin";
    public const string TenantManager = "TenantManager";
    public const string TenantUser = "TenantUser";
    public const string Accountant = "Accountant";
    public const string FieldAgent = "FieldAgent";
    public const string ReadOnly = "ReadOnly";
    public const string Occupant = "Occupant";
    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(new string[] { TenantOwner, TenantAdmin, TenantManager, TenantUser, Accountant, FieldAgent, ReadOnly, Occupant });
    public static IReadOnlyList<string> Invitable { get; } = Array.AsReadOnly(new string[] { TenantAdmin, TenantManager, TenantUser, Accountant, FieldAgent, ReadOnly, Occupant });
}
