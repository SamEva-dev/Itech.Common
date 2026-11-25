namespace LocaGuest.Security.Contracts;

/// <summary>Canonical V2 catalog. Do not mix with legacy grants without migration.</summary>
public static class LocaGuestPermissionCodesV2
{
    public const string PortfolioRead = "portfolio.read";
    public const string PortfolioManage = "portfolio.manage";
    public const string RentalRead = "rental.read";
    public const string RentalManage = "rental.manage";
    public const string DocumentsRead = "documents.read";
    public const string DocumentsManage = "documents.manage";
    public const string SignaturesRead = "signatures.read";
    public const string SignaturesManage = "signatures.manage";
    public const string SeasonalRead = "seasonal.read";
    public const string SeasonalManage = "seasonal.manage";
    public const string FinanceRead = "finance.read";
    public const string FinanceManage = "finance.manage";
    public const string FinanceExport = "finance.export";
    public const string FieldRead = "field.read";
    public const string FieldManage = "field.manage";
    public const string AssistantUse = "assistant.use";
    public const string AssistantManage = "assistant.manage";
    public const string OrganizationRead = "organization.read";
    public const string OrganizationManage = "organization.manage";
    public const string TeamRead = "team.read";
    public const string TeamManage = "team.manage";
    public const string TeamSecurityManage = "team.security.manage";
    public const string AuditRead = "audit.read";
    public const string BillingRead = "billing.read";
    public const string BillingManage = "billing.manage";

    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(new string[]
    {
        PortfolioRead,
        PortfolioManage,
        RentalRead,
        RentalManage,
        DocumentsRead,
        DocumentsManage,
        SignaturesRead,
        SignaturesManage,
        SeasonalRead,
        SeasonalManage,
        FinanceRead,
        FinanceManage,
        FinanceExport,
        FieldRead,
        FieldManage,
        AssistantUse,
        AssistantManage,
        OrganizationRead,
        OrganizationManage,
        TeamRead,
        TeamManage,
        TeamSecurityManage,
        AuditRead,
        BillingRead,
        BillingManage,
    });
}
