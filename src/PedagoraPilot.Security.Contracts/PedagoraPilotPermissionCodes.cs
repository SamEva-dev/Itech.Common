namespace PedagoraPilot.Security.Contracts;

/// <summary>
/// Stable public authorization contract for Pedagora Pilot.
/// Values are mirrored by the Angular application and must never be renamed once released.
/// </summary>
public static class PedagoraPilotPermissionCodes
{
    public static class Home
    {
        public const string View = "home.view";
        public static readonly string[] All = [View];
    }

    public static class Organization
    {
        public const string Dashboard = "organization.dashboard";
        public const string Read = "organization.read";
        public const string Manage = "organization.manage";
        public static readonly string[] All = [Dashboard, Read, Manage];
    }

    public static class Sites
    {
        public const string View = "sites.view";
        public const string Manage = "sites.manage";
        public static readonly string[] All = [View, Manage];
    }

    public static class Programs
    {
        public const string View = "programs.view";
        public const string Manage = "programs.manage";
        public static readonly string[] All = [View, Manage];
    }

    public static class Referentials
    {
        public const string View = "referentials.view";
        public const string Manage = "referentials.manage";
        public const string Publish = "referentials.publish";
        public static readonly string[] All = [View, Manage, Publish];
    }

    public static class Planning
    {
        public const string View = "planning.view";
        public const string Manage = "planning.manage";
        public static readonly string[] All = [View, Manage];
    }

    public static class RemoteWork
    {
        public const string View = "remoteWork.view";
        public const string Manage = "remoteWork.manage";
        public static readonly string[] All = [View, Manage];
    }

    public static class DistanceLearning
    {
        public const string View = "distanceLearning.view";
        public const string Manage = "distanceLearning.manage";
        public static readonly string[] All = [View, Manage];
    }

    public static class Learners
    {
        public const string View = "students.view";
        public const string DetailView = "studentDetail.view";
        public const string Create = "students.create";
        public const string Update = "students.update";
        public const string Delete = "students.delete";
        public static readonly string[] All = [View, DetailView, Create, Update, Delete];
    }

    public static class Cohorts
    {
        public const string View = "promotions.view";
        public const string Manage = "promotions.manage";
        public static readonly string[] All = [View, Manage];
    }

    public static class Sessions
    {
        public const string View = "sessions.view";
        public const string Manage = "sessions.manage";
        public static readonly string[] All = [View, Manage];
    }

    public static class Driving
    {
        public const string View = "driving.view";
        public const string Manage = "driving.manage";
        public static readonly string[] All = [View, Manage];
    }

    public static class Sheets
    {
        public const string View = "sheets.view";
        public const string Manage = "sheets.manage";
        public static readonly string[] All = [View, Manage];
    }

    public static class Skills
    {
        public const string View = "skills.view";
        public const string Evaluate = "skills.evaluate";
        public static readonly string[] All = [View, Evaluate];
    }

    public static class Attendance
    {
        public const string View = "attendance.view";
        public const string Manage = "attendance.manage";
        public static readonly string[] All = [View, Manage];
    }

    public static class Internships
    {
        public const string View = "internships.view";
        public const string Manage = "internships.manage";
        public static readonly string[] All = [View, Manage];
    }

    public static class Documents
    {
        public const string View = "documents.view";
        public const string Manage = "documents.manage";
        public const string Delete = "documents.delete";
        public static readonly string[] All = [View, Manage, Delete];
    }

    public static class Certification
    {
        public const string View = "certification.view";
        public const string Manage = "certification.manage";
        public const string Publish = "certification.publish";
        public const string CandidateView = "candidateCertification.view";
        public static readonly string[] All = [View, Manage, Publish, CandidateView];
    }

    public static class Jury
    {
        public const string View = "jury.view";
        public const string Evaluate = "jury.evaluate";
        public static readonly string[] All = [View, Evaluate];
    }

    public static class Results
    {
        public const string View = "results.view";
        public const string Manage = "results.manage";
        public const string Publish = "results.publish";
        public static readonly string[] All = [View, Manage, Publish];
    }

    public static class Success
    {
        public const string View = "success.view";
        public static readonly string[] All = [View];
    }

    public static class Reports
    {
        public const string View = "reports.view";
        public const string Export = "reports.export";
        public static readonly string[] All = [View, Export];
    }

    public static class Statistics
    {
        public const string View = "statistics.view";
        public static readonly string[] All = [View];
    }


    public static class Audit
    {
        public const string View = "audit.view";
        public const string Export = "audit.export";
        public static readonly string[] All = [View, Export];
    }

    public static class Access
    {
        public const string Manage = "access.manage";
        public static readonly string[] All = [Manage];
    }

    public static class Administration
    {
        public const string Manage = "administration.manage";
        public static readonly string[] All = [Manage];
    }

    public static readonly string[] All =
    [
        .. Home.All,
        .. Organization.All,
        .. Sites.All,
        .. Programs.All,
        .. Referentials.All,
        .. Planning.All,
        .. RemoteWork.All,
        .. DistanceLearning.All,
        .. Learners.All,
        .. Cohorts.All,
        .. Sessions.All,
        .. Driving.All,
        .. Sheets.All,
        .. Skills.All,
        .. Attendance.All,
        .. Internships.All,
        .. Documents.All,
        .. Certification.All,
        .. Jury.All,
        .. Results.All,
        .. Success.All,
        .. Reports.All,
        .. Statistics.All,
        .. Audit.All,
        .. Access.All,
        .. Administration.All
    ];
}
