namespace DriveOS.Security.Contracts;

/// <summary>
/// Permission codes used by DriveOS.
///
/// The values are public contracts shared by AuthGate, DriveOS APIs and
/// DriveOS frontends. Existing values must never be renamed after release.
/// Add new permissions instead of reusing an existing code for another
/// business operation.
/// </summary>
public static class DriveOsPermissionCodes
{
    /// <summary>
    /// ORG-001 and ORG-002: organization creation, consultation and lifecycle.
    /// </summary>
    public static class Organizations
    {
        public const string Read = "Organizations.Read";
        public const string Create = "Organizations.Create";

        public const string StatusHistoryRead =
            "Organizations.StatusHistory.Read";

        public const string SubmitForActivation =
            "Organizations.SubmitForActivation";

        public const string Activate = "Organizations.Activate";
        public const string Restrict = "Organizations.Restrict";
        public const string Suspend = "Organizations.Suspend";
        public const string Reactivate = "Organizations.Reactivate";
        public const string Close = "Organizations.Close";

        public static readonly string[] All =
        [
            Read,
            Create,
            StatusHistoryRead,
            SubmitForActivation,
            Activate,
            Restrict,
            Suspend,
            Reactivate,
            Close
        ];
    }

    public static class Networks
    {
        public const string Read = "Networks.Read";
        public const string Manage = "Networks.Manage";
        public static readonly string[] All = [Read, Manage];
    }

    /// <summary>
    /// ORG-003 and ORG-004: branch CRUD, primary branch and lifecycle.
    /// </summary>
    public static class Branches
    {
        public const string Read = "Branches.Read";
        public const string Create = "Branches.Create";
        public const string Update = "Branches.Update";
        public const string SetPrimary = "Branches.SetPrimary";

        public const string StatusHistoryRead =
            "Branches.StatusHistory.Read";

        public const string Activate = "Branches.Activate";
        public const string Restrict = "Branches.Restrict";
        public const string Suspend = "Branches.Suspend";
        public const string Reactivate = "Branches.Reactivate";
        public const string Close = "Branches.Close";

        public static readonly string[] All =
        [
            Read,
            Create,
            Update,
            SetPrimary,
            StatusHistoryRead,
            Activate,
            Restrict,
            Suspend,
            Reactivate,
            Close
        ];
    }

    /// <summary>
    /// ORG-005: current branch manager and manager assignment history.
    /// </summary>
    public static class BranchManagers
    {
        public const string Read = "BranchManagers.Read";
        public const string Assign = "BranchManagers.Assign";

        public const string HistoryRead =
            "BranchManagers.History.Read";

        public static readonly string[] All =
        [
            Read,
            Assign,
            HistoryRead
        ];
    }

    /// <summary>
    /// ORG-006: operational user assignments to branches.
    /// </summary>
    public static class BranchAssignments
    {
        public const string Read = "BranchAssignments.Read";
        public const string Create = "BranchAssignments.Create";
        public const string Suspend = "BranchAssignments.Suspend";
        public const string Reactivate = "BranchAssignments.Reactivate";
        public const string End = "BranchAssignments.End";

        public static readonly string[] All =
        [
            Read,
            Create,
            Suspend,
            Reactivate,
            End
        ];
    }


    /// <summary>
    /// ORG-007: organization profile, regional and operational settings.
    /// </summary>
    public static class OrganizationSettings
    {
        public const string Read = "OrganizationSettings.Read";
        public const string Create = "OrganizationSettings.Create";
        public const string Update = "OrganizationSettings.Update";

        public static readonly string[] All =
        [
            Read,
            Create,
            Update
        ];
    }


    /// <summary>
    /// ORG-008: organization SaaS subscription, lifecycle, entitlements and limits.
    /// </summary>
    public static class OrganizationSubscriptions
    {
        public const string Read = "OrganizationSubscriptions.Read";
        public const string Create = "OrganizationSubscriptions.Create";
        public const string ChangePlan = "OrganizationSubscriptions.ChangePlan";
        public const string Activate = "OrganizationSubscriptions.Activate";
        public const string MarkPastDue = "OrganizationSubscriptions.MarkPastDue";
        public const string Restrict = "OrganizationSubscriptions.Restrict";
        public const string Suspend = "OrganizationSubscriptions.Suspend";
        public const string Cancel = "OrganizationSubscriptions.Cancel";
        public const string Expire = "OrganizationSubscriptions.Expire";
        public const string ReadEntitlements = "OrganizationSubscriptions.Entitlements.Read";
        public const string ReadLimits = "OrganizationSubscriptions.Limits.Read";

        public static readonly string[] All =
        [
            Read, Create, ChangePlan, Activate, MarkPastDue,
            Restrict, Suspend, Cancel, Expire,
            ReadEntitlements, ReadLimits
        ];
    }


    /// <summary>
    /// ORG-009: versioned organization configurations.
    /// </summary>
    public static class OrganizationConfigurations
    {
        public const string Read = "OrganizationConfigurations.Read";
        public const string Create = "OrganizationConfigurations.Create";
        public const string Update = "OrganizationConfigurations.Update";
        public const string Publish = "OrganizationConfigurations.Publish";
        public const string Archive = "OrganizationConfigurations.Archive";

        public static readonly string[] All =
        [
            Read,
            Create,
            Update,
            Publish,
            Archive
        ];
    }



    /// <summary>
    /// ORG-010: versioned branch configuration overrides.
    /// </summary>
    public static class BranchConfigurationOverrides
    {
        public const string Read = "BranchConfigurationOverrides.Read";
        public const string Create = "BranchConfigurationOverrides.Create";
        public const string Update = "BranchConfigurationOverrides.Update";
        public const string Publish = "BranchConfigurationOverrides.Publish";
        public const string Archive = "BranchConfigurationOverrides.Archive";

        public static readonly string[] All =
        [
            Read,
            Create,
            Update,
            Publish,
            Archive
        ];
    }

    /// <summary>
    /// ORG-011: organization and branch business-number sequences.
    /// </summary>
    public static class OrganizationSequences
    {
        public const string Read = "OrganizationSequences.Read";
        public const string Create = "OrganizationSequences.Create";
        public const string Reserve = "OrganizationSequences.Reserve";
        public const string Suspend = "OrganizationSequences.Suspend";
        public const string Reactivate = "OrganizationSequences.Reactivate";
        public const string Archive = "OrganizationSequences.Archive";

        public static readonly string[] All =
        [
            Read, Create, Reserve, Suspend, Reactivate, Archive
        ];
    }


    /// <summary>
    /// ORG-012: owners, legal representatives and delegated organization authorities.
    /// </summary>
    public static class OrganizationRepresentatives
    {
        public const string Read = "OrganizationRepresentatives.Read";
        public const string Create = "OrganizationRepresentatives.Create";
        public const string Update = "OrganizationRepresentatives.Update";
        public const string Activate = "OrganizationRepresentatives.Activate";
        public const string Suspend = "OrganizationRepresentatives.Suspend";
        public const string Reactivate = "OrganizationRepresentatives.Reactivate";
        public const string End = "OrganizationRepresentatives.End";
        public const string SetPrimaryOwner = "OrganizationRepresentatives.SetPrimaryOwner";

        public static readonly string[] All =
        [
            Read, Create, Update, Activate, Suspend, Reactivate, End, SetPrimaryOwner
        ];
    }


    /// <summary>
    /// ORG-013: legal, registration and tax profile of an organization.
    /// </summary>
    public static class OrganizationLegalProfiles
    {
        public const string Read = "OrganizationLegalProfiles.Read";
        public const string Create = "OrganizationLegalProfiles.Create";
        public const string Update = "OrganizationLegalProfiles.Update";
        public const string Activate = "OrganizationLegalProfiles.Activate";
        public const string Archive = "OrganizationLegalProfiles.Archive";

        public static readonly string[] All =
        [
            Read, Create, Update, Activate, Archive
        ];
    }

    public static class OrganizationClosures
    {
        public const string Read = "OrganizationClosures.Read";
        public const string Create = "OrganizationClosures.Create";
        public const string Submit = "OrganizationClosures.Submit";
        public const string Approve = "OrganizationClosures.Approve";
        public const string Reject = "OrganizationClosures.Reject";
        public const string Schedule = "OrganizationClosures.Schedule";
        public const string Cancel = "OrganizationClosures.Cancel";
        public const string Complete = "OrganizationClosures.Complete";
        public const string Reopen = "OrganizationClosures.Reopen";

        public static readonly string[] All =
        [
            Read, Create, Submit, Approve, Reject, Schedule, Cancel, Complete, Reopen
        ];
    }


    /// <summary>
    /// CRM & Admissions: prospects/leads, qualification and ownership.
    /// </summary>
    public static class CrmLeads
    {
        public const string Read = "Crm.Leads.Read";
        public const string Create = "Crm.Leads.Create";
        public const string Update = "Crm.Leads.Update";
        public const string Assign = "Crm.Leads.Assign";
        public const string Qualify = "Crm.Leads.Qualify";
        public const string ChangeStatus = "Crm.Leads.ChangeStatus";
        public const string MarkLost = "Crm.Leads.MarkLost";
        public const string SetDormant = "Crm.Leads.SetDormant";
        public const string Reopen = "Crm.Leads.Reopen";
        public const string ReferToPartner = "Crm.Leads.ReferToPartner";

        public static readonly string[] All =
        [
            Read, Create, Update, Assign, Qualify, ChangeStatus,
            MarkLost, SetDormant, Reopen, ReferToPartner
        ];
    }

    /// <summary>
    /// CRM & Admissions: traceable interactions and activities around a lead.
    /// </summary>
    public static class CrmActivities
    {
        public const string Read = "Crm.Activities.Read";
        public const string Create = "Crm.Activities.Create";

        public static readonly string[] All = [Read, Create];
    }

    /// <summary>CRM &amp; Admissions: actions planifiées et relances d'un prospect.</summary>
    public static class CrmTasks
    {
        public const string Read = "Crm.Tasks.Read";
        public const string Create = "Crm.Tasks.Create";
        public const string Complete = "Crm.Tasks.Complete";
        public const string Cancel = "Crm.Tasks.Cancel";

        public static readonly string[] All = [Read, Create, Complete, Cancel];
    }

    /// <summary>
    /// CRM & Admissions: initial assessment appointments and assessments.
    /// </summary>
    public static class CrmAssessments
    {
        public const string Read = "Crm.Assessments.Read";
        public const string Create = "Crm.Assessments.Create";
        public const string Schedule = "Crm.Assessments.Schedule";
        public const string ReadAssigned = "Crm.Assessments.ReadAssigned";
        public const string Start = "Crm.Assessments.Start";
        public const string Complete = "Crm.Assessments.Complete";
        public const string Submit = "Crm.Assessments.Submit";
        public const string CreateNotes = "Crm.AssessmentNotes.Create";
        public const string ResultRead = "Crm.Assessments.Result.Read";
        public const string ResultCreate = "Crm.Assessments.Result.Create";
        public const string ResultValidate = "Crm.Assessments.Result.Validate";
        public const string ResultShare = "Crm.Assessments.Result.Share";
        public const string Cancel = "Crm.Assessments.Cancel";

        public static readonly string[] All =
        [
            Read, Create, Schedule, ReadAssigned, Start, Complete, Submit, CreateNotes,
            ResultRead, ResultCreate, ResultValidate, ResultShare, Cancel
        ];
    }

    /// <summary>
    /// CRM & Admissions: versioned training offers and commercial decisions.
    /// </summary>
    public static class CrmOffers
    {
        public const string Read = "Crm.Offers.Read";
        public const string Create = "Crm.Offers.Create";
        public const string Update = "Crm.Offers.Update";
        public const string UpdateDraft = "Crm.Offers.UpdateDraft";
        public const string SubmitForApproval = "Crm.Offers.SubmitForApproval";
        public const string Approve = "Crm.Offers.Approve";
        public const string ApplyDiscountWithinLimit = "Crm.Discounts.ApplyWithinLimit";
        public const string RequestDiscountApproval = "Crm.Discounts.RequestApproval";
        public const string Send = "Crm.Offers.Send";
        public const string Accept = "Crm.Offers.Accept";
        public const string Reject = "Crm.Offers.Reject";
        public const string Revise = "Crm.Offers.Revise";
        public const string Withdraw = "Crm.Offers.Withdraw";
        public const string MarkAccepted = "Crm.Offers.MarkAccepted";
        public const string MarkRejected = "Crm.Offers.MarkRejected";

        public static readonly string[] All =
        [
            Read, Create, Update, UpdateDraft, SubmitForApproval, Approve,
            ApplyDiscountWithinLimit, RequestDiscountApproval,
            Send, Accept, Reject, Revise, Withdraw, MarkAccepted, MarkRejected
        ];
    }

    /// <summary>
    /// CRM & Admissions: controlled conversion from an accepted lead into Student Administration.
    /// </summary>
    public static class CrmConversions
    {
        public const string ConvertToStudent = "Crm.Conversions.ConvertToStudent";

        public static readonly string[] All = [ConvertToStudent];
    }

    /// <summary>
    /// CRM dashboard: access, demonstrator states, data scopes and financial values.
    /// </summary>
    public static class CrmDashboard
    {
        public const string Read = "Crm.Dashboard.Read";
        public const string Nominal = "Crm.Dashboard.Tabs.Nominal";
        public const string Empty = "Crm.Dashboard.Tabs.Empty";
        public const string PartialData = "Crm.Dashboard.Tabs.PartialData";
        public const string RestrictedFinancial = "Crm.Dashboard.Tabs.RestrictedFinancial";
        public const string ActiveFilters = "Crm.Dashboard.Tabs.ActiveFilters";
        public const string IntegrationIncident = "Crm.Dashboard.Tabs.IntegrationIncident";
        public const string Loading = "Crm.Dashboard.Tabs.Loading";
        public const string WidgetError = "Crm.Dashboard.Tabs.WidgetError";
        public const string BranchScope = "Crm.Dashboard.Scope.Branch";
        public const string OrganizationScope = "Crm.Dashboard.Scope.Organization";
        public const string NetworkScope = "Crm.Dashboard.Scope.Network";
        public const string FinancialRead = "Crm.Dashboard.Financial.Read";

        public static readonly string[] All =
        [
            Read, Nominal, Empty, PartialData, RestrictedFinancial,
            ActiveFilters, IntegrationIncident, Loading, WidgetError,
            BranchScope, OrganizationScope, NetworkScope, FinancialRead
        ];
    }


    /// <summary>
    /// Every DriveOS permission delivered from ORG-001 through ORG-013.
    /// </summary>
    public static readonly string[] All =
    [
        .. Organizations.All,
        .. Networks.All,
        .. Branches.All,
        .. BranchManagers.All,
        .. BranchAssignments.All,
        .. OrganizationSettings.All,
        .. OrganizationSubscriptions.All,
        .. OrganizationConfigurations.All,
        .. BranchConfigurationOverrides.All,
        .. OrganizationSequences.All,
        .. OrganizationRepresentatives.All,
        .. OrganizationLegalProfiles.All,
        .. OrganizationClosures.All,
        .. CrmLeads.All,
        .. CrmActivities.All,
        .. CrmTasks.All,
        .. CrmAssessments.All,
        .. CrmOffers.All,
        .. CrmConversions.All,
        .. CrmDashboard.All
    ];

    /// <summary>
    /// Permissions that only expose data and do not mutate state.
    /// </summary>
    public static readonly string[] ReadOnly =
    [
        Organizations.Read,
        Organizations.StatusHistoryRead,
        Branches.Read,
        Branches.StatusHistoryRead,
        BranchManagers.Read,
        BranchManagers.HistoryRead,
        BranchAssignments.Read,
        OrganizationSettings.Read,
        OrganizationSubscriptions.Read,
        OrganizationSubscriptions.ReadEntitlements,
        OrganizationSubscriptions.ReadLimits,
        OrganizationConfigurations.Read,
        BranchConfigurationOverrides.Read,
        OrganizationSequences.Read,
        OrganizationRepresentatives.Read,
        OrganizationLegalProfiles.Read,
        OrganizationClosures.Read,
        CrmLeads.Read,
        CrmActivities.Read,
        CrmTasks.Read,
        CrmAssessments.Read,
        CrmAssessments.ResultRead,
        CrmOffers.Read,
        CrmDashboard.Read,
        CrmDashboard.Nominal,
        CrmDashboard.BranchScope
    ];

    /// <summary>
    /// Platform-level organization lifecycle permissions. These should not be
    /// assigned automatically to ordinary tenant roles.
    /// </summary>
    public static readonly string[] PlatformOrganizationGovernance =
    [
        Organizations.Activate,
        Organizations.Restrict,
        Organizations.Suspend,
        Organizations.Reactivate
    ];
}
