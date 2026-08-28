using Xunit;

namespace DriveOS.Security.Contracts.Tests;

public sealed class SupplierAdvancedSettlementPermissionTests
{
    [Fact]
    public void Advanced_supplier_settlement_permissions_are_in_finance_catalog()
    {
        string[] expected =
        [
            DriveOsPermissionCodes.Finance.SupplierInvoicesRecordManualPayment,
            DriveOsPermissionCodes.Finance.SupplierInvoicesRefundPayment,
            DriveOsPermissionCodes.Finance.SupplierInvoicesBatchPayment
        ];

        foreach(string permission in expected)
            Assert.Contains(permission,DriveOsPermissionCodes.Finance.All);
    }
}
