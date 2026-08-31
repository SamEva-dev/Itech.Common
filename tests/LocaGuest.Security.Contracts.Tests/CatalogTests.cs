using LocaGuest.Security.Contracts;
using Xunit;

namespace LocaGuest.Security.Contracts.Tests;

public sealed class CatalogTests
{
    [Fact]
    public void Application_code_is_stable() =>
        Assert.Equal("locaguest", LocaGuestApplication.ApplicationCode.ToString());

    [Fact]
    public void Permission_codes_are_unique() =>
        Assert.Equal(
            LocaGuestPermissionCodes.All.Length,
            LocaGuestPermissionCodes.All.Distinct(StringComparer.Ordinal).Count());

    [Fact]
    public void Permission_catalog_contains_expected_number_of_permissions() =>
        Assert.Equal(75, LocaGuestPermissionCodes.All.Length);

    [Fact]
    public void Tenant_owner_has_all_new_sensitive_permissions()
    {
        var permissions = LocaGuestRolePermissionDefaults.TenantOwnerPermissions;

        Assert.Contains(LocaGuestPermissionCodes.BankingWrite, permissions);
        Assert.Contains(LocaGuestPermissionCodes.AssistantActionsManage, permissions);
        Assert.Contains(LocaGuestPermissionCodes.KnowledgeManage, permissions);
        Assert.Contains(LocaGuestPermissionCodes.TemplatesDelete, permissions);
    }

    [Fact]
    public void Tenant_admin_does_not_receive_knowledge_management()
    {
        var permissions = LocaGuestRolePermissionDefaults.TenantAdminPermissions;

        Assert.Contains(LocaGuestPermissionCodes.KnowledgeRead, permissions);
        Assert.DoesNotContain(LocaGuestPermissionCodes.KnowledgeManage, permissions);
    }

    [Fact]
    public void Tenant_manager_has_operational_banking_but_not_banking_administration()
    {
        var permissions = LocaGuestRolePermissionDefaults.TenantManagerPermissions;

        Assert.Contains(LocaGuestPermissionCodes.BankingRead, permissions);
        Assert.Contains(LocaGuestPermissionCodes.BankingImport, permissions);
        Assert.Contains(LocaGuestPermissionCodes.BankingReconcile, permissions);
        Assert.DoesNotContain(LocaGuestPermissionCodes.BankingWrite, permissions);
    }

    [Fact]
    public void Tenant_user_can_use_assistant_but_cannot_manage_assistant_actions()
    {
        var permissions = LocaGuestRolePermissionDefaults.TenantUserPermissions;

        Assert.Contains(LocaGuestPermissionCodes.AssistantUse, permissions);
        Assert.DoesNotContain(LocaGuestPermissionCodes.AssistantActionsManage, permissions);
    }

    [Fact]
    public void Read_only_can_read_templates_but_cannot_modify_them()
    {
        var permissions = LocaGuestRolePermissionDefaults.ReadOnlyPermissions;

        Assert.Contains(LocaGuestPermissionCodes.TemplatesRead, permissions);
        Assert.DoesNotContain(LocaGuestPermissionCodes.TemplatesWrite, permissions);
        Assert.DoesNotContain(LocaGuestPermissionCodes.TemplatesDelete, permissions);
    }

    [Fact]
    public void Global_super_admin_is_not_a_locaguest_role() =>
        Assert.DoesNotContain("SuperAdmin", LocaGuestRoleCodes.All);

    [Fact]
    public void Every_role_mapping_has_only_known_permissions()
    {
        foreach (var role in LocaGuestRoleCodes.All)
        {
            Assert.All(
                LocaGuestRolePermissionDefaults.GetPermissionsForRole(role),
                permission => Assert.Contains(permission, LocaGuestPermissionCodes.All));
        }
    }
}

