namespace LocaGuest.Security.Contracts;

/// <summary>
/// Policy names
/// </summary>
public static class LocaGuestPolicyNameCodes
{
    // Tenant Management Policies
    public const string ManageTenantSettings = "ManageTenantSettings";
    public const string ViewTenantSettings = "ViewTenantSettings";
    public const string DeleteTenant = "DeleteTenant";

    // Billing Policies
    public const string ManageBilling = "ManageBilling";
    public const string ViewBilling = "ViewBilling";

    // User Management Policies
    public const string ManageUsers = "ManageUsers";
    public const string ViewUsers = "ViewUsers";
    public const string InviteUsers = "InviteUsers";

    // Role Management Policies
    public const string ManageRoles = "ManageRoles";
    public const string AssignRoles = "AssignRoles";
    public const string ViewRoles = "ViewRoles";

    // Properties Policies
    public const string ManageProperties = "ManageProperties";
    public const string ViewProperties = "ViewProperties";

    // Tenants (Locataires) Policies
    public const string ManageTenants = "ManageTenants";
    public const string ViewTenants = "ViewTenants";

    // Contracts Policies
    public const string ManageContracts = "ManageContracts";
    public const string ViewContracts = "ViewContracts";
    public const string TerminateContracts = "TerminateContracts";

    // Documents Policies
    public const string ManageDocuments = "ManageDocuments";
    public const string ViewDocuments = "ViewDocuments";
    public const string GenerateDocuments = "GenerateDocuments";

    // Analytics Policies
    public const string ViewAnalytics = "ViewAnalytics";
    public const string ExportAnalytics = "ExportAnalytics";

    // Audit Policies
    public const string ViewAuditLogs = "ViewAuditLogs";
    public const string ViewSystemLogs = "ViewSystemLogs";

    // Role-based Policies
    public const string IsSuperAdmin = "IsSuperAdmin";
    public const string IsTenantOwner = "IsTenantOwner";
    public const string IsTenantAdmin = "IsTenantAdmin";
    public const string IsAdminOrOwner = "IsAdminOrOwner";

    public static string ViewRentability = "ViewRentability";
}
