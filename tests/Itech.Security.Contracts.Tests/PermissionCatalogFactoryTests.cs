using Itech.Security.Contracts.Authorization;

namespace Itech.Security.Contracts.Tests;

public sealed class PermissionCatalogFactoryTests
{
    [Fact]
    public void Create_derives_readable_metadata_from_permission_code()
    {
        var definition = Assert.Single(PermissionCatalogFactory.Create(
            "driveos",
            ["Crm.Offers.Send"]));

        Assert.Equal("driveos", definition.ApplicationCode);
        Assert.Equal("Crm.Offers.Send", definition.Code);
        Assert.Equal("Crm Offers", definition.Category);
        Assert.Equal("Send Crm Offers", definition.DisplayName);
    }

    [Fact]
    public void Create_removes_duplicate_codes_case_insensitively()
    {
        var catalog = PermissionCatalogFactory.Create(
            "locaguest",
            ["users.read", "USERS.READ"]);

        Assert.Single(catalog);
    }
}
