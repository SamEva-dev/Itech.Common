namespace LocaGuest.Security.Contracts;

public static class LocaGuestPermissionCodes
{
    public const string TenantSettingsRead = "tenant.settings.read";
    public const string TenantSettingsWrite = "tenant.settings.write";
    public const string TenantDelete = "tenant.delete";

    public const string BillingRead = "billing.read";
    public const string BillingWrite = "billing.write";

    public const string UsersRead = "users.read";
    public const string UsersWrite = "users.write";
    public const string UsersDelete = "users.delete";
    public const string UsersInvite = "users.invite";

    public const string RolesRead = "roles.read";
    public const string RolesAssign = "roles.assign";
    public const string RolesWrite = "roles.write";

    public const string PropertiesRead = "properties.read";
    public const string PropertiesWrite = "properties.write";
    public const string PropertiesDelete = "properties.delete";

    public const string TenantsRead = "tenants.read";
    public const string TenantsWrite = "tenants.write";
    public const string TenantsDelete = "tenants.delete";

    public const string ContractsRead = "contracts.read";
    public const string ContractsWrite = "contracts.write";
    public const string ContractsTerminate = "contracts.terminate";
    public const string ContractsDelete = "contracts.delete";

    public const string DocumentsRead = "documents.read";
    public const string DocumentsWrite = "documents.write";
    public const string DocumentsUpload = "documents.upload";
    public const string DocumentsGenerate = "documents.generate";
    public const string DocumentsDelete = "documents.delete";

    public const string TemplatesRead = "templates.read";
    public const string TemplatesWrite = "templates.write";
    public const string TemplatesDelete = "templates.delete";

    public const string RoomsRead = "rooms.read";
    public const string RoomsWrite = "rooms.write";

    public const string SeasonRead = "season.read";
    public const string SeasonWrite = "season.write";

    public const string PaymentsRead = "payments.read";
    public const string PaymentsWrite = "payments.write";

    public const string DepositsRead = "deposits.read";
    public const string DepositsWrite = "deposits.write";

    public const string TeamRead = "team.read";
    public const string TeamManage = "team.manage";

    public const string BankingRead = "banking.read";
    public const string BankingWrite = "banking.write";
    public const string BankingImport = "banking.import";
    public const string BankingReconcile = "banking.reconcile";

    public const string RentabilityRead = "rentability.read";
    public const string RentabilityWrite = "rentability.write";

    public const string AnalyticsRead = "analytics.read";
    public const string AnalyticsExport = "analytics.export";

    public const string FinancialReportingRead = "financial-reporting.read";

    public const string FinanceRead = "finance.read";
    public const string FinanceWrite = "finance.write";
    public const string FinanceExport = "finance.export";

    public const string TaxPreparationRead = "tax-preparation.read";
    public const string TaxPreparationExport = "tax-preparation.export";

    public const string AssistantUse = "assistant.use";
    public const string AssistantFinancial = "assistant.financial";
    public const string AssistantLegal = "assistant.legal";
    public const string AssistantTax = "assistant.tax";
    public const string AssistantActionsManage = "assistant.actions.manage";

    public const string KnowledgeRead = "knowledge.read";
    public const string KnowledgeManage = "knowledge.manage";

    public const string AuditRead = "audit.read";
    public const string AuditLogsRead = "auditlogs.read";

    public const string OpsLogsRead = "ops.logs.read";

    public const string SystemLogsRead = "system.logs.read";

    public const string SessionsRead = "sessions.read";
    public const string SessionsRevoke = "sessions.revoke";
    public const string SessionsRevokeAllExceptCurrent = "sessions.revoke_all_except_current";
    public const string SessionsAdminRead = "sessions.admin.read";
    public const string SessionsAdminRevoke = "sessions.admin.revoke";

    public const string ReferenceDataManage = "reference-data.manage";

    public const string PermissionsRead = "permissions.read";
    public const string PermissionsWrite = "permissions.write";

    public const string SignaturesRead = "signatures.read";
    public const string SignaturesWrite = "signatures.write";

    public static readonly string[] All =
    [
        TenantSettingsRead,
        TenantSettingsWrite,
        TenantDelete,
        BillingRead,
        BillingWrite,
        UsersRead,
        UsersWrite,
        UsersDelete,
        UsersInvite,
        RolesRead,
        RolesAssign,
        RolesWrite,
        PropertiesRead,
        PropertiesWrite,
        PropertiesDelete,
        TenantsRead,
        TenantsWrite,
        TenantsDelete,
        ContractsRead,
        ContractsWrite,
        ContractsTerminate,
        ContractsDelete,
        DocumentsRead,
        DocumentsWrite,
        DocumentsUpload,
        DocumentsGenerate,
        DocumentsDelete,
        TemplatesRead,
        TemplatesWrite,
        TemplatesDelete,
        RoomsRead,
        RoomsWrite,
        SeasonRead,
        SeasonWrite,
        PaymentsRead,
        PaymentsWrite,
        DepositsRead,
        DepositsWrite,
        TeamRead,
        TeamManage,
        BankingRead,
        BankingWrite,
        BankingImport,
        BankingReconcile,
        RentabilityRead,
        RentabilityWrite,
        AnalyticsRead,
        AnalyticsExport,
        FinancialReportingRead,
        FinanceRead,
        FinanceWrite,
        FinanceExport,
        TaxPreparationRead,
        TaxPreparationExport,
        AssistantUse,
        AssistantFinancial,
        AssistantLegal,
        AssistantTax,
        AssistantActionsManage,
        KnowledgeRead,
        KnowledgeManage,
        AuditRead,
        AuditLogsRead,
        OpsLogsRead,
        SystemLogsRead,
        SessionsRead,
        SessionsRevoke,
        SessionsRevokeAllExceptCurrent,
        SessionsAdminRead,
        SessionsAdminRevoke,
        ReferenceDataManage,
        PermissionsRead,
        PermissionsWrite,
        SignaturesRead,
        SignaturesWrite
    ];
}
