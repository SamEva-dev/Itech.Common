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
}

