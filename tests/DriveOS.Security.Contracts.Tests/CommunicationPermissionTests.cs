using Xunit;

namespace DriveOS.Security.Contracts.Tests;

public sealed class CommunicationPermissionTests
{
    [Fact]
    public void Communication_permissions_are_present_in_global_catalog()
    {
        string[] expected =
        [
            DriveOsPermissionCodes.Communication.Notifications.Read,
            DriveOsPermissionCodes.Communication.Notifications.Manage,
            DriveOsPermissionCodes.Communication.NotificationPreferences.Manage
        ];

        foreach (string permission in expected)
            Assert.Contains(permission, DriveOsPermissionCodes.All);
    }

    [Fact]
    public void Supplier_invoice_permissions_from_MKT_026_are_preserved()
    {
        Assert.Contains(
            DriveOsPermissionCodes.Finance.SupplierInvoicesApproveFinancial,
            DriveOsPermissionCodes.Finance.All);
    }

    [Fact]
    public void Professional_marketplace_permissions_are_preserved()
    {
        Assert.Contains(
            DriveOsPermissionCodes.ProfessionalMarketplace.Dashboard.Read,
            DriveOsPermissionCodes.All);
    }
}
