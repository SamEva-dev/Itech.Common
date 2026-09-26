namespace PedagoraPilot.Security.Contracts;

public static class PedagoraPilotRolePermissionDefaults
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Matrix =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [PedagoraPilotRoleCodes.PlatformAdministrator] = PedagoraPilotPermissionCodes.All,
            [PedagoraPilotRoleCodes.OrganizationAdministrator] = PedagoraPilotPermissionCodes.All,
            [PedagoraPilotRoleCodes.OrganizationDirection] =
                PedagoraPilotPermissionCodes.All
                    .Where(code => !string.Equals(code, PedagoraPilotPermissionCodes.Access.PrivilegedManage, StringComparison.OrdinalIgnoreCase)
                                   && !string.Equals(code, PedagoraPilotPermissionCodes.Organization.OwnershipTransfer, StringComparison.OrdinalIgnoreCase)
                                   && !string.Equals(code, PedagoraPilotPermissionCodes.Organization.CommercialManage, StringComparison.OrdinalIgnoreCase))
                    .ToArray(),

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
                .. PedagoraPilotPermissionCodes.Cohorts.All,
                .. PedagoraPilotPermissionCodes.Sessions.All,
                .. PedagoraPilotPermissionCodes.Driving.All,
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
                .. PedagoraPilotPermissionCodes.Audit.All
            ],

            [PedagoraPilotRoleCodes.PedagogicalManager] =
            [
                PedagoraPilotPermissionCodes.Home.View,
                .. PedagoraPilotPermissionCodes.Planning.All,
                .. PedagoraPilotPermissionCodes.DistanceLearning.All,
                PedagoraPilotPermissionCodes.RemoteWork.View,
                .. PedagoraPilotPermissionCodes.Learners.All,
                .. PedagoraPilotPermissionCodes.Cohorts.All,
                .. PedagoraPilotPermissionCodes.Sessions.All,
                .. PedagoraPilotPermissionCodes.Driving.All,
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
                PedagoraPilotPermissionCodes.Audit.View
            ],

            [PedagoraPilotRoleCodes.Secretariat] =
            [
                PedagoraPilotPermissionCodes.Home.View,
                .. PedagoraPilotPermissionCodes.Planning.All,
                PedagoraPilotPermissionCodes.RemoteWork.View,
                PedagoraPilotPermissionCodes.DistanceLearning.View,
                .. PedagoraPilotPermissionCodes.Learners.All,
                .. PedagoraPilotPermissionCodes.Cohorts.All,
                .. PedagoraPilotPermissionCodes.Sessions.All,
                .. PedagoraPilotPermissionCodes.Attendance.All,
                .. PedagoraPilotPermissionCodes.Internships.All,
                .. PedagoraPilotPermissionCodes.Documents.All,
                .. PedagoraPilotPermissionCodes.Certification.All,
                .. PedagoraPilotPermissionCodes.Results.All,
                .. PedagoraPilotPermissionCodes.Success.All,
                .. PedagoraPilotPermissionCodes.Reports.All,
                PedagoraPilotPermissionCodes.Audit.View
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
                .. PedagoraPilotPermissionCodes.Driving.All,
                .. PedagoraPilotPermissionCodes.Sheets.All,
                .. PedagoraPilotPermissionCodes.Skills.All,
                .. PedagoraPilotPermissionCodes.Attendance.All,
                PedagoraPilotPermissionCodes.Internships.View,
                PedagoraPilotPermissionCodes.Documents.View,
                PedagoraPilotPermissionCodes.Certification.View,
                PedagoraPilotPermissionCodes.Certification.CandidateView
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
                PedagoraPilotPermissionCodes.Attendance.View,
                PedagoraPilotPermissionCodes.Internships.View,
                PedagoraPilotPermissionCodes.Documents.View,
                PedagoraPilotPermissionCodes.Certification.View,
                PedagoraPilotPermissionCodes.Certification.CandidateView
            ],

            [PedagoraPilotRoleCodes.Jury] =
            [
                PedagoraPilotPermissionCodes.Certification.View,
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
