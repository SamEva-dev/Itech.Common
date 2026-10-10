namespace PedagoraPilot.Security.Contracts;

/// <summary>
/// Default permission matrix for AuthGate role provisioning.
/// These permissions are capabilities, not unconditional access to documents:
/// handlers must still validate tenant, site, cohort, membership and signer identity.
/// PedagogicalManager includes trainer capabilities; no distinct referent role exists
/// in the provided PedagoraPilotRoleCodes catalog.
/// </summary>
public static class PedagoraPilotRolePermissionDefaults
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Matrix =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [PedagoraPilotRoleCodes.PlatformAdministrator] = PedagoraPilotPermissionCodes.All,
            [PedagoraPilotRoleCodes.OrganizationAdministrator] = PedagoraPilotPermissionCodes.All,
            [PedagoraPilotRoleCodes.OrganizationDirection] = PedagoraPilotPermissionCodes.All,

            [PedagoraPilotRoleCodes.SiteDirection] =
            [
                PedagoraPilotPermissionCodes.Home.View,
                PedagoraPilotPermissionCodes.Planning.View,
                PedagoraPilotPermissionCodes.Planning.Manage,
                PedagoraPilotPermissionCodes.RemoteWork.View,
                PedagoraPilotPermissionCodes.RemoteWork.Manage,
                PedagoraPilotPermissionCodes.DistanceLearning.View,
                PedagoraPilotPermissionCodes.DistanceLearning.Manage,
                .. PedagoraPilotPermissionCodes.Learners.All,
                PedagoraPilotPermissionCodes.Cohorts.View,
                PedagoraPilotPermissionCodes.Cohorts.Manage,
                PedagoraPilotPermissionCodes.Sessions.View,
                PedagoraPilotPermissionCodes.Sessions.Manage,
                PedagoraPilotPermissionCodes.Driving.View,
                PedagoraPilotPermissionCodes.Driving.Manage,
                .. PedagoraPilotPermissionCodes.Sheets.All,
                .. PedagoraPilotPermissionCodes.Skills.All,
                .. PedagoraPilotPermissionCodes.Attendance.All,
                .. PedagoraPilotPermissionCodes.Internships.All,
                .. PedagoraPilotPermissionCodes.Documents.All,
                .. PedagoraPilotPermissionCodes.Certification.All,
                .. PedagoraPilotPermissionCodes.Results.All,
                .. PedagoraPilotPermissionCodes.Success.All,
                .. PedagoraPilotPermissionCodes.Reports.All,
                .. PedagoraPilotPermissionCodes.Statistics.All,
                .. PedagoraPilotPermissionCodes.Audit.All,
                .. PedagoraPilotPermissionCodes.Access.All,
                PedagoraPilotPermissionCodes.Sessions.ViewOthers,
                PedagoraPilotPermissionCodes.Sessions.AssignTrainer,
                PedagoraPilotPermissionCodes.Driving.ViewOthers,
                PedagoraPilotPermissionCodes.Driving.AssignTrainer,
                PedagoraPilotPermissionCodes.Signatures.View,
                PedagoraPilotPermissionCodes.Signatures.Request,
                PedagoraPilotPermissionCodes.Signatures.Supervise,
            ],

            [PedagoraPilotRoleCodes.PedagogicalManager] =
            [
                PedagoraPilotPermissionCodes.Home.View,
                .. PedagoraPilotPermissionCodes.Planning.All,
                .. PedagoraPilotPermissionCodes.DistanceLearning.All,
                PedagoraPilotPermissionCodes.RemoteWork.View,
                .. PedagoraPilotPermissionCodes.Learners.All,
                PedagoraPilotPermissionCodes.Cohorts.View,
                PedagoraPilotPermissionCodes.Cohorts.Manage,
                PedagoraPilotPermissionCodes.Sessions.View,
                PedagoraPilotPermissionCodes.Sessions.Manage,
                PedagoraPilotPermissionCodes.Driving.View,
                PedagoraPilotPermissionCodes.Driving.Manage,
                .. PedagoraPilotPermissionCodes.Sheets.All,
                .. PedagoraPilotPermissionCodes.Skills.All,
                .. PedagoraPilotPermissionCodes.Attendance.All,
                .. PedagoraPilotPermissionCodes.Internships.All,
                .. PedagoraPilotPermissionCodes.Documents.All,
                .. PedagoraPilotPermissionCodes.Certification.All,
                .. PedagoraPilotPermissionCodes.Results.All,
                .. PedagoraPilotPermissionCodes.Success.All,
                .. PedagoraPilotPermissionCodes.Reports.All,
                .. PedagoraPilotPermissionCodes.Statistics.All,
                PedagoraPilotPermissionCodes.Audit.View,
                PedagoraPilotPermissionCodes.Sessions.ViewOthers,
                PedagoraPilotPermissionCodes.Sessions.AssignTrainer,
                PedagoraPilotPermissionCodes.Driving.ViewOthers,
                PedagoraPilotPermissionCodes.Driving.AssignTrainer,
                PedagoraPilotPermissionCodes.Signatures.View,
                PedagoraPilotPermissionCodes.Signatures.Request,
                PedagoraPilotPermissionCodes.Signatures.Supervise,
            ],

            [PedagoraPilotRoleCodes.Secretariat] =
            [
                PedagoraPilotPermissionCodes.Home.View,
                .. PedagoraPilotPermissionCodes.Planning.All,
                PedagoraPilotPermissionCodes.RemoteWork.View,
                PedagoraPilotPermissionCodes.DistanceLearning.View,
                .. PedagoraPilotPermissionCodes.Learners.All,
                PedagoraPilotPermissionCodes.Cohorts.View,
                PedagoraPilotPermissionCodes.Cohorts.Manage,
                PedagoraPilotPermissionCodes.Sessions.View,
                PedagoraPilotPermissionCodes.Sessions.Manage,
                .. PedagoraPilotPermissionCodes.Attendance.All,
                .. PedagoraPilotPermissionCodes.Internships.All,
                .. PedagoraPilotPermissionCodes.Documents.All,
                .. PedagoraPilotPermissionCodes.Certification.All,
                .. PedagoraPilotPermissionCodes.Results.All,
                .. PedagoraPilotPermissionCodes.Success.All,
                .. PedagoraPilotPermissionCodes.Reports.All,
                PedagoraPilotPermissionCodes.Audit.View,
                PedagoraPilotPermissionCodes.Sessions.ViewOthers,
                PedagoraPilotPermissionCodes.Signatures.View,
            ],

            [PedagoraPilotRoleCodes.Trainer] =
            [
                PedagoraPilotPermissionCodes.Home.View,
                PedagoraPilotPermissionCodes.Planning.View,
                PedagoraPilotPermissionCodes.RemoteWork.View,
                PedagoraPilotPermissionCodes.DistanceLearning.View,
                PedagoraPilotPermissionCodes.Learners.View,
                PedagoraPilotPermissionCodes.Learners.DetailView,
                PedagoraPilotPermissionCodes.Sessions.View,
                PedagoraPilotPermissionCodes.Sessions.Manage,
                PedagoraPilotPermissionCodes.Driving.View,
                PedagoraPilotPermissionCodes.Driving.Manage,
                .. PedagoraPilotPermissionCodes.Sheets.All,
                .. PedagoraPilotPermissionCodes.Skills.All,
                .. PedagoraPilotPermissionCodes.Attendance.All,
                PedagoraPilotPermissionCodes.Internships.View,
                PedagoraPilotPermissionCodes.Documents.View,
                PedagoraPilotPermissionCodes.Certification.View,
                PedagoraPilotPermissionCodes.Certification.CandidateView,
                PedagoraPilotPermissionCodes.Signatures.View,
                PedagoraPilotPermissionCodes.Signatures.Request,
            ],

            [PedagoraPilotRoleCodes.Student] =
            [
                PedagoraPilotPermissionCodes.Home.View,
                PedagoraPilotPermissionCodes.Planning.View,
                PedagoraPilotPermissionCodes.DistanceLearning.View,
                PedagoraPilotPermissionCodes.Learners.DetailView,
                PedagoraPilotPermissionCodes.Sessions.View,
                PedagoraPilotPermissionCodes.Driving.View,
                PedagoraPilotPermissionCodes.Sheets.View,
                PedagoraPilotPermissionCodes.Skills.View,
                PedagoraPilotPermissionCodes.Internships.View,
                PedagoraPilotPermissionCodes.Documents.View,
                PedagoraPilotPermissionCodes.Certification.View,
                PedagoraPilotPermissionCodes.Certification.CandidateView,
                PedagoraPilotPermissionCodes.Signatures.View,
                PedagoraPilotPermissionCodes.Signatures.Sign,
            ],

            [PedagoraPilotRoleCodes.Jury] =
            [
                PedagoraPilotPermissionCodes.Certification.CandidateView,
                PedagoraPilotPermissionCodes.Jury.View,
                PedagoraPilotPermissionCodes.Jury.Evaluate
            ],

            [PedagoraPilotRoleCodes.ReadOnly] =
            [
                PedagoraPilotPermissionCodes.Home.View,
                PedagoraPilotPermissionCodes.Planning.View,
                PedagoraPilotPermissionCodes.DistanceLearning.View,
                PedagoraPilotPermissionCodes.Learners.View,
                PedagoraPilotPermissionCodes.Learners.DetailView,
                PedagoraPilotPermissionCodes.Cohorts.View,
                PedagoraPilotPermissionCodes.Sessions.View,
                PedagoraPilotPermissionCodes.Driving.View,
                PedagoraPilotPermissionCodes.Sheets.View,
                PedagoraPilotPermissionCodes.Skills.View,
                PedagoraPilotPermissionCodes.Attendance.View,
                PedagoraPilotPermissionCodes.Internships.View,
                PedagoraPilotPermissionCodes.Documents.View,
                PedagoraPilotPermissionCodes.Certification.View,
                PedagoraPilotPermissionCodes.Certification.CandidateView,
                PedagoraPilotPermissionCodes.Results.View,
                PedagoraPilotPermissionCodes.Success.View,
                PedagoraPilotPermissionCodes.Reports.View,
                PedagoraPilotPermissionCodes.Statistics.View,
                PedagoraPilotPermissionCodes.Audit.View
            ]
        };

    public static IReadOnlyList<string> GetPermissions(string roleCode) =>
        Matrix.TryGetValue(roleCode, out var permissions)
            ? permissions
            : Array.Empty<string>();

    public static bool TryGetPermissions(string roleCode, out IReadOnlyList<string> permissions) =>
        Matrix.TryGetValue(roleCode, out permissions!);
}
