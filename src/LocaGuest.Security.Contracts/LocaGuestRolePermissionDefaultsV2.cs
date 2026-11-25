using static LocaGuest.Security.Contracts.LocaGuestPermissionCodesV2;

namespace LocaGuest.Security.Contracts;

public static class LocaGuestRolePermissionDefaultsV2
{
    public static IReadOnlyList<string> GetPermissionsForRole(string role) =>
        LocaGuestPermissionHierarchyV2.Expand(role switch
        {
            LocaGuestRoleCodesV2.TenantOwner => new string[] { PortfolioRead, PortfolioManage, RentalRead, RentalManage, DocumentsRead, DocumentsManage, SignaturesRead, SignaturesManage, SeasonalRead, SeasonalManage, FinanceRead, FinanceManage, FinanceExport, FieldRead, FieldManage, AssistantUse, AssistantManage, OrganizationRead, OrganizationManage, TeamRead, TeamManage, TeamSecurityManage, AuditRead, BillingRead, BillingManage },
            LocaGuestRoleCodesV2.TenantAdmin => new string[] { PortfolioRead, PortfolioManage, RentalRead, RentalManage, DocumentsRead, DocumentsManage, SignaturesRead, SignaturesManage, SeasonalRead, SeasonalManage, FinanceRead, FinanceManage, FinanceExport, FieldRead, FieldManage, AssistantUse, AssistantManage, OrganizationRead, OrganizationManage, TeamRead, TeamManage, TeamSecurityManage, AuditRead, BillingRead },
            LocaGuestRoleCodesV2.TenantManager => new string[] { PortfolioManage, RentalManage, DocumentsManage, SignaturesManage, SeasonalManage, FinanceManage, FinanceExport, FieldManage, AssistantUse, OrganizationRead, TeamRead },
            LocaGuestRoleCodesV2.TenantUser => new string[] { PortfolioRead, RentalManage, DocumentsManage, SignaturesRead, SeasonalManage, FinanceRead, FieldManage, AssistantUse },
            LocaGuestRoleCodesV2.Accountant => new string[] { PortfolioRead, RentalRead, DocumentsRead, SeasonalRead, FinanceManage, FinanceExport, AssistantUse },
            LocaGuestRoleCodesV2.FieldAgent => new string[] { PortfolioRead, RentalRead, DocumentsManage, FieldManage, AssistantUse },
            LocaGuestRoleCodesV2.ReadOnly => new string[] { PortfolioRead, RentalRead, DocumentsRead, SignaturesRead, SeasonalRead, FinanceRead, FieldRead },
            LocaGuestRoleCodesV2.Occupant => new string[] {  },
            _ => Array.Empty<string>()
        });
}
