using Xunit;

namespace LocaGuest.Security.Contracts.Tests;

public sealed class PermissionCatalogTests
{
    [Fact]
    public void Catalog_contains_every_declared_permission_once()
    {
        Assert.Equal(LocaGuestPermissionCodes.All.Length, LocaGuestPermissionCatalog.All.Count);
        Assert.All(
            LocaGuestPermissionCodes.All,
            code => Assert.Single(LocaGuestPermissionCatalog.All, item => item.Code == code));
    }

    [Fact]
    public void Catalog_is_owned_by_locaguest()
    {
        Assert.All(
            LocaGuestPermissionCatalog.All,
            item => Assert.Equal(LocaGuestApplication.Code, item.ApplicationCode));
    }
}
