using Xunit;
using static LocaGuest.Security.Contracts.LocaGuestPermissionCodesV2;

namespace LocaGuest.Security.Contracts.Tests;

public sealed class SecurityContractsV2Tests
{
    [Fact]
    public void Catalog_has_25_unique_codes()
    {
        Assert.Equal(25, All.Count);
        Assert.Equal(25, All.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(25, LocaGuestPermissionCatalogV2.All.Count);
    }

    [Fact]
    public void Roles_have_only_known_grants()
    {
        Assert.Equal(8, LocaGuestRoleCodesV2.All.Count);
        Assert.Equal(7, LocaGuestRoleCodesV2.Invitable.Count);
        Assert.DoesNotContain(LocaGuestRoleCodesV2.TenantOwner, LocaGuestRoleCodesV2.Invitable);
        foreach (var role in LocaGuestRoleCodesV2.All)
            Assert.All(LocaGuestRolePermissionDefaultsV2.GetPermissionsForRole(role), grant => Assert.Contains(grant, All));
        Assert.Equal(25, LocaGuestRolePermissionDefaultsV2.GetPermissionsForRole(LocaGuestRoleCodesV2.TenantOwner).Count);
        Assert.Empty(LocaGuestRolePermissionDefaultsV2.GetPermissionsForRole(LocaGuestRoleCodesV2.Occupant));
        Assert.Empty(LocaGuestRolePermissionDefaultsV2.GetPermissionsForRole("OccupantOwner"));
    }

    [Fact]
    public void Manage_implies_read_but_not_export_or_other_domains()
    {
        Assert.True(LocaGuestPermissionHierarchyV2.Allows(new[] { PortfolioManage }, PortfolioRead));
        Assert.False(LocaGuestPermissionHierarchyV2.Allows(new[] { PortfolioRead }, PortfolioManage));
        Assert.False(LocaGuestPermissionHierarchyV2.Allows(new[] { FinanceManage }, FinanceExport));
        Assert.False(LocaGuestPermissionHierarchyV2.Allows(new[] { TeamManage }, TeamSecurityManage));
        Assert.False(LocaGuestPermissionHierarchyV2.Allows(new[] { "properties.write" }, PortfolioManage));
        Assert.True(LocaGuestPermissionHierarchyV2.Allows(new[] { AssistantManage }, AssistantUse));
    }

    [Fact]
    public void Every_legacy_code_has_exactly_one_review_candidate()
    {
        Assert.Equal(LocaGuestPermissionCodes.All.Length, LocaGuestLegacyPermissionMigrationV2.All.Count);
        foreach (var code in LocaGuestPermissionCodes.All)
            Assert.Single(LocaGuestLegacyPermissionMigrationV2.All, item => item.LegacyCode == code);
        foreach (var item in LocaGuestLegacyPermissionMigrationV2.All)
            if (item.TargetCode is not null) Assert.Contains(item.TargetCode, All);
    }
}
