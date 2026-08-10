using Xunit;

namespace DriveOS.Security.Contracts.Tests;

public sealed class PermissionCatalogTests
{
    [Fact]
    public void Catalog_contains_every_declared_permission_once()
    {
        Assert.Equal(DriveOsPermissionCodes.All.Length, DriveOsPermissionCatalog.All.Count);
        Assert.All(
            DriveOsPermissionCodes.All,
            code => Assert.Single(DriveOsPermissionCatalog.All, item => item.Code == code));
    }

    [Fact]
    public void Catalog_is_owned_by_driveos()
    {
        Assert.All(
            DriveOsPermissionCatalog.All,
            item => Assert.Equal(DriveOsApplication.Code, item.ApplicationCode));
    }
}
