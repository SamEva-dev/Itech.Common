using Xunit;

namespace DriveOS.Security.Contracts.Tests;

public sealed class CatalogTests
{
    [Fact]
    public void Application_code_is_stable() =>
        Assert.Equal("driveos", DriveOsApplication.ApplicationCode.ToString());

    [Fact]
    public void Permission_codes_are_unique() =>
        Assert.Equal(
            DriveOsPermissionCodes.All.Length,
            DriveOsPermissionCodes.All.Distinct(StringComparer.Ordinal).Count());

    [Fact]
    public void Every_role_has_only_known_permissions()
    {
        var known = DriveOsPermissionCodes.All.ToHashSet(StringComparer.Ordinal);

        foreach (var permissions in DriveOsRolePermissionDefaults.All.Values)
        {
            Assert.All(permissions, permission => Assert.Contains(permission, known));
        }
    }

    [Fact]
    public void Organization_sequence_permission_codes_are_stable() =>
        Assert.Equal(
            [
                "OrganizationSequences.Read",
                "OrganizationSequences.Create",
                "OrganizationSequences.Reserve",
                "OrganizationSequences.Suspend",
                "OrganizationSequences.Reactivate",
                "OrganizationSequences.Archive"
            ],
            DriveOsPermissionCodes.OrganizationSequences.All);

    [Fact]
    public void Crm_dashboard_permission_codes_are_stable() =>
        Assert.Equal(
            [
                "Crm.Dashboard.Read",
                "Crm.Dashboard.Tabs.Nominal",
                "Crm.Dashboard.Tabs.Empty",
                "Crm.Dashboard.Tabs.PartialData",
                "Crm.Dashboard.Tabs.RestrictedFinancial",
                "Crm.Dashboard.Tabs.ActiveFilters",
                "Crm.Dashboard.Tabs.IntegrationIncident",
                "Crm.Dashboard.Tabs.Loading",
                "Crm.Dashboard.Tabs.WidgetError",
                "Crm.Dashboard.Scope.Branch",
                "Crm.Dashboard.Scope.Organization",
                "Crm.Dashboard.Scope.Network",
                "Crm.Dashboard.Financial.Read"
            ],
            DriveOsPermissionCodes.CrmDashboard.All);
}
