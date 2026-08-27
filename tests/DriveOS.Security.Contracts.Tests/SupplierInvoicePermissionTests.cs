using Xunit;

namespace DriveOS.Security.Contracts.Tests;

public sealed class SupplierInvoicePermissionTests
{
    [Fact]
    public void Finance_catalog_contains_supplier_invoice_permissions()
    {
        string[] expected =
        [
            DriveOsPermissionCodes.Finance.SupplierInvoicesRead,
            DriveOsPermissionCodes.Finance.SupplierInvoicesCreate,
            DriveOsPermissionCodes.Finance.SupplierInvoicesMatch,
            DriveOsPermissionCodes.Finance.SupplierInvoicesApproveOperational,
            DriveOsPermissionCodes.Finance.SupplierInvoicesApproveFinancial,
            DriveOsPermissionCodes.Finance.SupplierInvoicesSchedulePayment
        ];

        foreach(string permission in expected)
            Assert.Contains(permission,DriveOsPermissionCodes.Finance.All);
    }
}
