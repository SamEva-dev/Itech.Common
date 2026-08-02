
namespace LocaGuest.Security.Contracts;

/// <summary>
/// Defines the matrix of permissions assigned to each role
/// </summary>
public static class LocaGuestRolePermissionDefaults
{
    /// <summary>
    /// Gets permissions for TenantOwner role
    /// Full access within tenant including billing and administration
    /// </summary>
    public static readonly string[] TenantOwnerPermissions =
    {
        // Tenant Management
        LocaGuestPermissionCodes.TenantSettingsRead,
        LocaGuestPermissionCodes.TenantSettingsWrite,
        LocaGuestPermissionCodes.TenantDelete,

        // Billing (owner only)
        LocaGuestPermissionCodes.BillingRead,
        LocaGuestPermissionCodes.BillingWrite,

        // User Management
        LocaGuestPermissionCodes.UsersRead,
        LocaGuestPermissionCodes.UsersWrite,
        LocaGuestPermissionCodes.UsersDelete,
        LocaGuestPermissionCodes.UsersInvite,

        // Role Management
        LocaGuestPermissionCodes.RolesRead,
        LocaGuestPermissionCodes.RolesAssign,
        LocaGuestPermissionCodes.RolesWrite,

        // Properties
        LocaGuestPermissionCodes.PropertiesRead,
        LocaGuestPermissionCodes.PropertiesWrite,
        LocaGuestPermissionCodes.PropertiesDelete,

        // Tenants (Locataires)
        LocaGuestPermissionCodes.TenantsRead,
        LocaGuestPermissionCodes.TenantsWrite,
        LocaGuestPermissionCodes.TenantsDelete,

        // Contracts
        LocaGuestPermissionCodes.ContractsRead,
        LocaGuestPermissionCodes.ContractsWrite,
        LocaGuestPermissionCodes.ContractsTerminate,
        LocaGuestPermissionCodes.ContractsDelete,

        // Documents
        LocaGuestPermissionCodes.DocumentsRead,
        LocaGuestPermissionCodes.DocumentsWrite,
        LocaGuestPermissionCodes.DocumentsUpload,
        LocaGuestPermissionCodes.DocumentsGenerate,
        LocaGuestPermissionCodes.DocumentsDelete,

        // Rooms
        LocaGuestPermissionCodes.RoomsRead,
        LocaGuestPermissionCodes.RoomsWrite,

        // Season (Mon Airbnb)
        LocaGuestPermissionCodes.SeasonRead,
        LocaGuestPermissionCodes.SeasonWrite,

        // Payments
        LocaGuestPermissionCodes.PaymentsRead,
        LocaGuestPermissionCodes.PaymentsWrite,

        // Deposits
        LocaGuestPermissionCodes.DepositsRead,
        LocaGuestPermissionCodes.DepositsWrite,

        // Team
        LocaGuestPermissionCodes.TeamRead,
        LocaGuestPermissionCodes.TeamManage,

        // Analytics
        LocaGuestPermissionCodes.AnalyticsRead,
        LocaGuestPermissionCodes.AnalyticsExport,
        LocaGuestPermissionCodes.FinancialReportingRead,

        LocaGuestPermissionCodes.FinanceRead,
        LocaGuestPermissionCodes.FinanceWrite,
        LocaGuestPermissionCodes.FinanceExport,

        LocaGuestPermissionCodes.TaxPreparationRead,
        LocaGuestPermissionCodes.TaxPreparationExport,

        // Audit
        LocaGuestPermissionCodes.AuditRead,
        LocaGuestPermissionCodes.AuditLogsRead,

        // Sessions
        LocaGuestPermissionCodes.SessionsRead,
        LocaGuestPermissionCodes.SessionsRevoke,
        LocaGuestPermissionCodes.SessionsRevokeAllExceptCurrent,

        // IAM
        LocaGuestPermissionCodes.PermissionsRead,

        // Signatre
        LocaGuestPermissionCodes.SignaturesWrite,
        LocaGuestPermissionCodes.SignaturesRead,

        // Rentability
        LocaGuestPermissionCodes.RentabilityRead,
        LocaGuestPermissionCodes.RentabilityWrite
    };

    /// <summary>
    /// Gets permissions for TenantAdmin role
    /// Administrative access but no billing
    /// </summary>
    public static readonly string[] TenantAdminPermissions =
    {
        // Tenant Management (read only)
        LocaGuestPermissionCodes.TenantSettingsRead,

        // User Management
        LocaGuestPermissionCodes.UsersRead,
        LocaGuestPermissionCodes.UsersWrite,
        LocaGuestPermissionCodes.UsersInvite,

        // Role Management (limited)
        LocaGuestPermissionCodes.RolesRead,
        LocaGuestPermissionCodes.RolesAssign,

        // Properties
        LocaGuestPermissionCodes.PropertiesRead,
        LocaGuestPermissionCodes.PropertiesWrite,
        LocaGuestPermissionCodes.PropertiesDelete,

        // Tenants (Locataires)
        LocaGuestPermissionCodes.TenantsRead,
        LocaGuestPermissionCodes.TenantsWrite,
        LocaGuestPermissionCodes.TenantsDelete,

        // Contracts
        LocaGuestPermissionCodes.ContractsRead,
        LocaGuestPermissionCodes.ContractsWrite,
        LocaGuestPermissionCodes.ContractsTerminate,
        LocaGuestPermissionCodes.ContractsDelete,

        // Documents
        LocaGuestPermissionCodes.DocumentsRead,
        LocaGuestPermissionCodes.DocumentsWrite,
        LocaGuestPermissionCodes.DocumentsUpload,
        LocaGuestPermissionCodes.DocumentsGenerate,
        LocaGuestPermissionCodes.DocumentsDelete,

        // Rooms
        LocaGuestPermissionCodes.RoomsRead,
        LocaGuestPermissionCodes.RoomsWrite,

        // Season (Mon Airbnb)
        LocaGuestPermissionCodes.SeasonRead,
        LocaGuestPermissionCodes.SeasonWrite,

        // Payments
        LocaGuestPermissionCodes.PaymentsRead,
        LocaGuestPermissionCodes.PaymentsWrite,

        // Deposits
        LocaGuestPermissionCodes.DepositsRead,
        LocaGuestPermissionCodes.DepositsWrite,

        // Team
        LocaGuestPermissionCodes.TeamRead,
        LocaGuestPermissionCodes.TeamManage,

        // Analytics
        LocaGuestPermissionCodes.AnalyticsRead,
        LocaGuestPermissionCodes.AnalyticsExport,
        LocaGuestPermissionCodes.FinancialReportingRead,

        LocaGuestPermissionCodes.FinanceRead,
        LocaGuestPermissionCodes.FinanceWrite,

        LocaGuestPermissionCodes.TaxPreparationRead,
        LocaGuestPermissionCodes.TaxPreparationExport,

        // Audit
        LocaGuestPermissionCodes.AuditRead,
        LocaGuestPermissionCodes.AuditLogsRead,

        // Sessions
        LocaGuestPermissionCodes.SessionsRead,
        LocaGuestPermissionCodes.SessionsRevoke,
        LocaGuestPermissionCodes.SessionsRevokeAllExceptCurrent,

        // IAM
        LocaGuestPermissionCodes.PermissionsRead,

        // Signatre
        LocaGuestPermissionCodes.SignaturesWrite,
        LocaGuestPermissionCodes.SignaturesRead,

         // Rentability
        LocaGuestPermissionCodes.RentabilityRead,
        LocaGuestPermissionCodes.RentabilityWrite
    };

    /// <summary>
    /// Gets permissions for TenantManager role
    /// Operational access without administration
    /// </summary>
    public static readonly string[] TenantManagerPermissions =
    {
        // Properties
        LocaGuestPermissionCodes.PropertiesRead,
        LocaGuestPermissionCodes.PropertiesWrite,

        // Tenants (Locataires)
        LocaGuestPermissionCodes.TenantsRead,
        LocaGuestPermissionCodes.TenantsWrite,

        // Contracts
        LocaGuestPermissionCodes.ContractsRead,
        LocaGuestPermissionCodes.ContractsWrite,
        LocaGuestPermissionCodes.ContractsTerminate,

        // Documents
        LocaGuestPermissionCodes.DocumentsRead,
        LocaGuestPermissionCodes.DocumentsUpload,
        LocaGuestPermissionCodes.DocumentsGenerate,

        // Rooms
        LocaGuestPermissionCodes.RoomsRead,
        LocaGuestPermissionCodes.RoomsWrite,

        // Season (Mon Airbnb)
        LocaGuestPermissionCodes.SeasonRead,
        LocaGuestPermissionCodes.SeasonWrite,

        // Payments
        LocaGuestPermissionCodes.PaymentsRead,
        LocaGuestPermissionCodes.PaymentsWrite,

        // Deposits
        LocaGuestPermissionCodes.DepositsRead,
        LocaGuestPermissionCodes.DepositsWrite,

        // Team
        LocaGuestPermissionCodes.TeamRead,

        // Analytics
        LocaGuestPermissionCodes.AnalyticsRead,

        // Signatre
        LocaGuestPermissionCodes.SignaturesWrite,
        LocaGuestPermissionCodes.SignaturesRead,

         // Rentability
        LocaGuestPermissionCodes.RentabilityRead,
        LocaGuestPermissionCodes.RentabilityWrite

    };

    /// <summary>
    /// Gets permissions for TenantUser role
    /// Standard user access
    /// </summary>
    public static readonly string[] TenantUserPermissions =
    {
        // Properties (read only)
        LocaGuestPermissionCodes.PropertiesRead,

        // Tenants (Locataires)
        LocaGuestPermissionCodes.TenantsRead,
        LocaGuestPermissionCodes.TenantsWrite,

        // Contracts (read only)
        LocaGuestPermissionCodes.ContractsRead,

        // Documents
        LocaGuestPermissionCodes.DocumentsRead,
        LocaGuestPermissionCodes.DocumentsUpload,

        // Rooms
        LocaGuestPermissionCodes.RoomsRead,

        // Season (Mon Airbnb)
        LocaGuestPermissionCodes.SeasonRead,

        // Payments
        LocaGuestPermissionCodes.PaymentsRead,

        // Deposits
        LocaGuestPermissionCodes.DepositsRead,

        // Team
        LocaGuestPermissionCodes.TeamRead,

        // Analytics (read only)
        LocaGuestPermissionCodes.AnalyticsRead
    };

    /// <summary>
    /// Gets permissions for ReadOnly role
    /// Read-only access to most resources
    /// </summary>
    public static readonly string[] ReadOnlyPermissions =
    {
        // Properties
        LocaGuestPermissionCodes.PropertiesRead,

        // Tenants (Locataires)
        LocaGuestPermissionCodes.TenantsRead,

        // Contracts
        LocaGuestPermissionCodes.ContractsRead,

        // Documents
        LocaGuestPermissionCodes.DocumentsRead,

        // Rooms
        LocaGuestPermissionCodes.RoomsRead,

        // Season (Mon Airbnb)
        LocaGuestPermissionCodes.SeasonRead,

        // Payments
        LocaGuestPermissionCodes.PaymentsRead,

        // Deposits
        LocaGuestPermissionCodes.DepositsRead,

        // Team
        LocaGuestPermissionCodes.TeamRead,

        // Analytics
        LocaGuestPermissionCodes.AnalyticsRead,
        LocaGuestPermissionCodes.AnalyticsExport,

        // Audit
        LocaGuestPermissionCodes.AuditRead,

        // Signatre
        LocaGuestPermissionCodes.SignaturesRead,
         // Rentability
        LocaGuestPermissionCodes.RentabilityRead
    };

    public static readonly string[] OccupantPermissions =
    {
        LocaGuestPermissionCodes.DocumentsRead
    };

    public static readonly string[] OccupantAdminPermissions = TenantAdminPermissions;

    public static readonly string[] OccupantOwnerPermissions = TenantOwnerPermissions;

    /// <summary>
    /// Gets all permissions for a specific role
    /// </summary>
    public static string[] GetPermissionsForRole(string roleName)
    {
        return roleName switch
        {
            LocaGuestRoleCodes.TenantOwner => TenantOwnerPermissions,
            LocaGuestRoleCodes.TenantAdmin => TenantAdminPermissions,
            LocaGuestRoleCodes.TenantManager => TenantManagerPermissions,
            LocaGuestRoleCodes.TenantUser => TenantUserPermissions,
            LocaGuestRoleCodes.ReadOnly => ReadOnlyPermissions,
            LocaGuestRoleCodes.Occupant => OccupantPermissions,
            LocaGuestRoleCodes.OccupantAdmin => OccupantAdminPermissions,
            LocaGuestRoleCodes.OccupantOwner => OccupantOwnerPermissions,
            _ => Array.Empty<string>()
        };
    }
}
