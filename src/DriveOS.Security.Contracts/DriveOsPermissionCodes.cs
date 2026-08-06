using System.Diagnostics;

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
            Read, Create, Submit, Approve, Reject, Schedule, 
            Cancel, Complete, Reopen
        ];
    }


    /// <summary>
    /// Every DriveOS permission delivered from ORG-001 through ORG-013.
    /// </summary>
    public static readonly string[] All =
    [
        .. Organizations.All,
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
        .. OrganizationClosures.All
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
        OrganizationLegalProfiles.Read
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
