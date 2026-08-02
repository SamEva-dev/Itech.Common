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

