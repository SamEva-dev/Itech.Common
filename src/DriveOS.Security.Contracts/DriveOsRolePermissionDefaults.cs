namespace DriveOS.Security.Contracts;

/// <summary>
/// Default DriveOS role-to-permission matrix for initial AuthGate seeding.
///
/// This matrix is a bootstrap default only. AuthGate remains the source of
/// truth and may persist tenant-specific role customizations afterwards.
/// </summary>
public static class DriveOsRolePermissionDefaults
{
    private static readonly string[] DashboardBranch =
    [
        DriveOsPermissionCodes.CrmDashboard.Read,
        DriveOsPermissionCodes.CrmDashboard.Nominal,
        DriveOsPermissionCodes.CrmDashboard.BranchScope
    ];

    private static readonly string[] DashboardBranchFinancial =
    [
        .. DashboardBranch,
        DriveOsPermissionCodes.CrmDashboard.FinancialRead
    ];

    private static readonly string[] DashboardOrganizationFinancial =
    [
        .. DashboardBranchFinancial,
        DriveOsPermissionCodes.CrmDashboard.OrganizationScope
    ];

    private static readonly IReadOnlyDictionary<
        string,
        IReadOnlyCollection<string>> Matrix =
        new Dictionary<string, IReadOnlyCollection<string>>(
            StringComparer.Ordinal)
        {
            [DriveOsRoleCodes.PlatformAdministrator] =
                DriveOsPermissionCodes.All,

            [DriveOsRoleCodes.OrganizationOwner] =
            [
                DriveOsPermissionCodes.Organizations.Read,
                DriveOsPermissionCodes.Organizations.Create,
                DriveOsPermissionCodes.Organizations.StatusHistoryRead,
                DriveOsPermissionCodes.Organizations.SubmitForActivation,
                DriveOsPermissionCodes.Organizations.Close,

                .. DriveOsPermissionCodes.Branches.All,
                .. DriveOsPermissionCodes.BranchManagers.All,
                .. DriveOsPermissionCodes.BranchAssignments.All,
                .. DriveOsPermissionCodes.OrganizationSettings.All,
                .. DriveOsPermissionCodes.OrganizationSubscriptions.All,
                .. DriveOsPermissionCodes.OrganizationConfigurations.All,
                .. DriveOsPermissionCodes.BranchConfigurationOverrides.All,
                .. DriveOsPermissionCodes.OrganizationSequences.All,
                .. DriveOsPermissionCodes.OrganizationRepresentatives.All,
                .. DriveOsPermissionCodes.OrganizationLegalProfiles.All,
                .. DriveOsPermissionCodes.CrmLeads.All,
                .. DriveOsPermissionCodes.CrmActivities.All,
                .. DriveOsPermissionCodes.CrmAssessments.All,
                .. DriveOsPermissionCodes.CrmOffers.All,
                .. DriveOsPermissionCodes.CrmConversions.All,
                .. DashboardOrganizationFinancial
            ],

            [DriveOsRoleCodes.OrganizationAdministrator] =
            [
                DriveOsPermissionCodes.Organizations.Read,
                DriveOsPermissionCodes.Organizations.StatusHistoryRead,
                DriveOsPermissionCodes.Organizations.SubmitForActivation,

                .. DriveOsPermissionCodes.Branches.All,
                .. DriveOsPermissionCodes.BranchManagers.All,
                .. DriveOsPermissionCodes.BranchAssignments.All,
                .. DriveOsPermissionCodes.OrganizationSettings.All,
                .. DriveOsPermissionCodes.OrganizationSubscriptions.All,
                .. DriveOsPermissionCodes.OrganizationConfigurations.All,
                .. DriveOsPermissionCodes.BranchConfigurationOverrides.All,
                .. DriveOsPermissionCodes.OrganizationSequences.All,
                .. DriveOsPermissionCodes.OrganizationRepresentatives.All,
                .. DriveOsPermissionCodes.OrganizationLegalProfiles.All,
                .. DriveOsPermissionCodes.CrmLeads.All,
                .. DriveOsPermissionCodes.CrmActivities.All,
                .. DriveOsPermissionCodes.CrmAssessments.All,
                .. DriveOsPermissionCodes.CrmOffers.All,
                .. DriveOsPermissionCodes.CrmConversions.All,
                .. DashboardOrganizationFinancial
            ],

            [DriveOsRoleCodes.Director] =
            [
                DriveOsPermissionCodes.Organizations.Read,
                DriveOsPermissionCodes.Organizations.StatusHistoryRead,
                DriveOsPermissionCodes.Organizations.SubmitForActivation,

                .. DriveOsPermissionCodes.Branches.All,
                .. DriveOsPermissionCodes.BranchManagers.All,
                .. DriveOsPermissionCodes.BranchAssignments.All,
                .. DriveOsPermissionCodes.OrganizationSettings.All,
                .. DriveOsPermissionCodes.OrganizationSubscriptions.All,
                .. DriveOsPermissionCodes.OrganizationConfigurations.All,
                .. DriveOsPermissionCodes.BranchConfigurationOverrides.All,
                .. DriveOsPermissionCodes.OrganizationSequences.All,
                .. DriveOsPermissionCodes.OrganizationRepresentatives.All,
                .. DriveOsPermissionCodes.OrganizationLegalProfiles.All,
                .. DriveOsPermissionCodes.CrmLeads.All,
                .. DriveOsPermissionCodes.CrmActivities.All,
                .. DriveOsPermissionCodes.CrmAssessments.All,
                .. DriveOsPermissionCodes.CrmOffers.All,
                .. DriveOsPermissionCodes.CrmConversions.All,
                .. DashboardOrganizationFinancial
            ],

            [DriveOsRoleCodes.BranchManager] =
            [
                DriveOsPermissionCodes.Organizations.Read,
                DriveOsPermissionCodes.Organizations.StatusHistoryRead,

                DriveOsPermissionCodes.Branches.Read,
                DriveOsPermissionCodes.Branches.Update,
                DriveOsPermissionCodes.Branches.StatusHistoryRead,
                DriveOsPermissionCodes.Branches.Activate,
                DriveOsPermissionCodes.Branches.Restrict,
                DriveOsPermissionCodes.Branches.Suspend,
                DriveOsPermissionCodes.Branches.Reactivate,

                DriveOsPermissionCodes.BranchManagers.Read,
                DriveOsPermissionCodes.BranchManagers.HistoryRead,
                DriveOsPermissionCodes.OrganizationSettings.Read,
                DriveOsPermissionCodes.OrganizationSubscriptions.Read,
                DriveOsPermissionCodes.OrganizationSubscriptions.ReadEntitlements,
                DriveOsPermissionCodes.OrganizationSubscriptions.ReadLimits,
                DriveOsPermissionCodes.OrganizationConfigurations.Read,
                .. DriveOsPermissionCodes.BranchConfigurationOverrides.All,
                .. DriveOsPermissionCodes.OrganizationSequences.All,
                .. DriveOsPermissionCodes.OrganizationRepresentatives.All,

                .. DriveOsPermissionCodes.BranchAssignments.All,
                .. DriveOsPermissionCodes.CrmLeads.All,
                .. DriveOsPermissionCodes.CrmActivities.All,
                .. DriveOsPermissionCodes.CrmAssessments.All,
                .. DriveOsPermissionCodes.CrmOffers.All,
                .. DriveOsPermissionCodes.CrmConversions.All,
                .. DashboardBranchFinancial
            ],

            [DriveOsRoleCodes.PedagogicalManager] =
            [
                DriveOsPermissionCodes.Organizations.Read,
                DriveOsPermissionCodes.Branches.Read,
                DriveOsPermissionCodes.Branches.StatusHistoryRead,
                DriveOsPermissionCodes.BranchManagers.Read,
                DriveOsPermissionCodes.BranchManagers.HistoryRead,
                .. DriveOsPermissionCodes.BranchAssignments.All,
                DriveOsPermissionCodes.CrmLeads.Read,
                DriveOsPermissionCodes.CrmActivities.Read,
                .. DriveOsPermissionCodes.CrmAssessments.All,
                DriveOsPermissionCodes.CrmOffers.Read,
                .. DashboardBranch
            ],

            [DriveOsRoleCodes.AdministrativeManager] =
            [
                DriveOsPermissionCodes.Organizations.Read,
                DriveOsPermissionCodes.Branches.Read,
                DriveOsPermissionCodes.Branches.Update,
                DriveOsPermissionCodes.Branches.StatusHistoryRead,
                DriveOsPermissionCodes.BranchManagers.Read,
                DriveOsPermissionCodes.BranchManagers.HistoryRead,
                .. DriveOsPermissionCodes.BranchAssignments.All,
                DriveOsPermissionCodes.OrganizationRepresentatives.Read,
                .. DriveOsPermissionCodes.CrmLeads.All,
                .. DriveOsPermissionCodes.CrmActivities.All,
                .. DriveOsPermissionCodes.CrmAssessments.All,
                .. DriveOsPermissionCodes.CrmOffers.All,
                .. DriveOsPermissionCodes.CrmConversions.All,
                .. DashboardOrganizationFinancial
            ],

            [DriveOsRoleCodes.Secretary] =
            [
                DriveOsPermissionCodes.Organizations.Read,
                DriveOsPermissionCodes.Branches.Read,
                DriveOsPermissionCodes.BranchManagers.Read,
                DriveOsPermissionCodes.BranchAssignments.Read,
                DriveOsPermissionCodes.BranchAssignments.Create,
                DriveOsPermissionCodes.OrganizationSequences.Read,
                DriveOsPermissionCodes.OrganizationSequences.Reserve,
                DriveOsPermissionCodes.CrmLeads.Read,
                DriveOsPermissionCodes.CrmLeads.Create,
                DriveOsPermissionCodes.CrmLeads.Update,
                DriveOsPermissionCodes.CrmActivities.Read,
                DriveOsPermissionCodes.CrmActivities.Create,
                DriveOsPermissionCodes.CrmAssessments.Read,
                DriveOsPermissionCodes.CrmAssessments.Schedule,
                DriveOsPermissionCodes.CrmOffers.Read,
                .. DashboardBranch
            ],

            [DriveOsRoleCodes.Accountant] =
                DriveOsPermissionCodes.ReadOnly,

            [DriveOsRoleCodes.FleetManager] =
                DriveOsPermissionCodes.ReadOnly,

            [DriveOsRoleCodes.ExamCoordinator] =
                DriveOsPermissionCodes.ReadOnly,

            [DriveOsRoleCodes.Instructor] =
            [
                DriveOsPermissionCodes.Organizations.Read,
                DriveOsPermissionCodes.Branches.Read,
                DriveOsPermissionCodes.BranchManagers.Read,
                DriveOsPermissionCodes.BranchAssignments.Read
            ],

            [DriveOsRoleCodes.SalesAdvisor] =
            [
                DriveOsPermissionCodes.Organizations.Read,
                DriveOsPermissionCodes.Branches.Read,
                .. DriveOsPermissionCodes.CrmLeads.All,
                .. DriveOsPermissionCodes.CrmActivities.All,
                .. DriveOsPermissionCodes.CrmAssessments.All,
                .. DriveOsPermissionCodes.CrmOffers.All,
                .. DriveOsPermissionCodes.CrmConversions.All,
                .. DashboardBranch
            ],

            [DriveOsRoleCodes.ComplianceOfficer] =
                DriveOsPermissionCodes.ReadOnly,

            [DriveOsRoleCodes.TrainingCoordinator] =
            [
                DriveOsPermissionCodes.Organizations.Read,
                DriveOsPermissionCodes.Branches.Read,
                DriveOsPermissionCodes.BranchManagers.Read,
                DriveOsPermissionCodes.BranchAssignments.Read,
                DriveOsPermissionCodes.BranchAssignments.Create,
                DriveOsPermissionCodes.OrganizationSequences.Read,
                DriveOsPermissionCodes.OrganizationSequences.Reserve
            ],

            [DriveOsRoleCodes.Receptionist] =
            [
                DriveOsPermissionCodes.Organizations.Read,
                DriveOsPermissionCodes.Branches.Read,
                DriveOsPermissionCodes.BranchManagers.Read,
                DriveOsPermissionCodes.BranchAssignments.Read,
                DriveOsPermissionCodes.CrmLeads.Read,
                DriveOsPermissionCodes.CrmLeads.Create,
                DriveOsPermissionCodes.CrmLeads.Update,
                DriveOsPermissionCodes.CrmActivities.Read,
                DriveOsPermissionCodes.CrmActivities.Create,
                DriveOsPermissionCodes.CrmAssessments.Read,
                DriveOsPermissionCodes.CrmAssessments.Schedule,
                .. DashboardBranch
            ],

            [DriveOsRoleCodes.SupportAgent] =
                DriveOsPermissionCodes.ReadOnly,

            [DriveOsRoleCodes.ReadOnly] =
                DriveOsPermissionCodes.ReadOnly
        };

    public static IReadOnlyDictionary<
        string,
        IReadOnlyCollection<string>> All => Matrix;

    public static IReadOnlyCollection<string> GetPermissions(
        string roleCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleCode);

        return Matrix.TryGetValue(roleCode, out var permissions)
            ? permissions
            : Array.Empty<string>();
    }

    public static bool TryGetPermissions(
        string roleCode,
        out IReadOnlyCollection<string> permissions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleCode);

        if (Matrix.TryGetValue(roleCode, out var configuredPermissions))
        {
            permissions = configuredPermissions;
            return true;
        }

        permissions = Array.Empty<string>();
        return false;
    }
}
