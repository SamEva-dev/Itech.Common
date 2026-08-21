using Xunit;

namespace DriveOS.Security.Contracts.Tests;

public sealed class CatalogTests
{
    [Fact]
    public void Application_code_is_stable() =>
        Assert.Equal("driveos", DriveOsApplication.ApplicationCode.ToString());

    [Fact]
    public void Permission_codes_are_unique() =>
        Assert.Equal(
            DriveOsPermissionCodes.All.Length,
            DriveOsPermissionCodes.All.Distinct(StringComparer.Ordinal).Count());

    [Fact]
    public void Every_role_has_only_known_permissions()
    {
        var known = DriveOsPermissionCodes.All.ToHashSet(StringComparer.Ordinal);

        foreach (var permissions in DriveOsRolePermissionDefaults.All.Values)
        {
            Assert.All(permissions, permission => Assert.Contains(permission, known));
        }
    }

    [Fact]
    public void Organization_sequence_permission_codes_are_stable() =>
        Assert.Equal(
            [
                "OrganizationSequences.Read",
                "OrganizationSequences.Create",
                "OrganizationSequences.Reserve",
                "OrganizationSequences.Suspend",
                "OrganizationSequences.Reactivate",
                "OrganizationSequences.Archive"
            ],
            DriveOsPermissionCodes.OrganizationSequences.All);

    [Fact]
    public void Crm_dashboard_permission_codes_are_stable() =>
        Assert.Equal(
            [
                "Crm.Dashboard.Read",
                "Crm.Dashboard.Tabs.Nominal",
                "Crm.Dashboard.Tabs.Empty",
                "Crm.Dashboard.Tabs.PartialData",
                "Crm.Dashboard.Tabs.RestrictedFinancial",
                "Crm.Dashboard.Tabs.ActiveFilters",
                "Crm.Dashboard.Tabs.IntegrationIncident",
                "Crm.Dashboard.Tabs.Loading",
                "Crm.Dashboard.Tabs.WidgetError",
                "Crm.Dashboard.Scope.Branch",
                "Crm.Dashboard.Scope.Organization",
                "Crm.Dashboard.Scope.Network",
                "Crm.Dashboard.Financial.Read"
            ],
            DriveOsPermissionCodes.CrmDashboard.All);

    [Fact]
    public void Crm_assessment_execution_permission_codes_are_stable() =>
        Assert.Equal(
            [
                "Crm.Assessments.Read",
                "Crm.Assessments.Create",
                "Crm.Assessments.Schedule",
                "Crm.Assessments.ReadAssigned",
                "Crm.Assessments.Start",
                "Crm.Assessments.Complete",
                "Crm.Assessments.Submit",
                "Crm.AssessmentNotes.Create",
                "Crm.Assessments.Result.Read",
                "Crm.Assessments.Result.Create",
                "Crm.Assessments.Result.Validate",
                "Crm.Assessments.Result.Share",
                "Crm.Assessments.Cancel"
            ],
            DriveOsPermissionCodes.CrmAssessments.All);
    [Fact]
    public void Training_delivery_permission_codes_are_stable() =>
        Assert.Equal(
            [
                "TrainingDelivery.Sessions.Read",
                "TrainingDelivery.Sessions.Materialize",
                "TrainingDelivery.Sessions.Prepare",
                "TrainingDelivery.Sessions.Start",
                "TrainingDelivery.Sessions.Complete",
                "TrainingDelivery.Attendance.Record",
                "TrainingDelivery.Attendance.Correct",
                "TrainingDelivery.Attendance.Override",
                "TrainingDelivery.Execution.Interventions.Record",
                "TrainingDelivery.Execution.Observations.Record",
                "TrainingDelivery.Execution.Interrupt",
                "TrainingDelivery.Execution.Resume",
                "TrainingDelivery.Execution.Odometer.Record",
                "TrainingDelivery.Assessments.Record",
                "TrainingDelivery.Incidents.Read",
                "TrainingDelivery.Incidents.Report",
                "TrainingDelivery.Incidents.Update",
                "TrainingDelivery.Incidents.Escalate",
                "TrainingDelivery.Incidents.Resolve",
                "TrainingDelivery.Incidents.Close",
                "TrainingDelivery.Consequences.Read",
                "TrainingDelivery.Consequences.Retry",
                "TrainingDelivery.Cancellations.Read",
                "TrainingDelivery.Cancellations.Record",
                "TrainingDelivery.SessionComments.CreateShared",
                "TrainingDelivery.SessionNotes.CreateInternal",
                "TrainingDelivery.SessionNotes.ReadInternal",
                "TrainingDelivery.Reports.Read",
                "TrainingDelivery.Reports.Write",
                "TrainingDelivery.Reports.Submit",
                "TrainingDelivery.Reports.RequestReview",
                "TrainingDelivery.Reports.Monitor",
                "TrainingDelivery.Reports.RequestCorrection",
                "TrainingDelivery.Reports.Correct",
                "TrainingDelivery.Reports.ApproveCorrection",
                "TrainingDelivery.Reports.Dispute",
                "TrainingDelivery.GroupSessions.Read",
                "TrainingDelivery.GroupSessions.Materialize",
                "TrainingDelivery.GroupSessions.ManageParticipants",
                "TrainingDelivery.GroupSessions.Attendance.Record",
                "TrainingDelivery.GroupSessions.Assessments.Record",
                "TrainingDelivery.GroupSessions.Report.Write",
                "TrainingDelivery.GroupSessions.Certificates.Prepare"
            ],
            DriveOsPermissionCodes.TrainingDelivery.All);

    [Fact]
    public void Training_delivery_read_only_group_contains_no_mutating_permissions()
    {
        Assert.DoesNotContain(DriveOsPermissionCodes.TrainingDelivery.ReportsWrite, DriveOsPermissionCodes.TrainingDelivery.ReadOnly);
        Assert.DoesNotContain(DriveOsPermissionCodes.TrainingDelivery.ReportsSubmit, DriveOsPermissionCodes.TrainingDelivery.ReadOnly);
        Assert.DoesNotContain(DriveOsPermissionCodes.TrainingDelivery.ReportsRequestCorrection, DriveOsPermissionCodes.TrainingDelivery.ReadOnly);
        Assert.DoesNotContain(DriveOsPermissionCodes.TrainingDelivery.ReportsApproveCorrection, DriveOsPermissionCodes.TrainingDelivery.ReadOnly);
        Assert.DoesNotContain(DriveOsPermissionCodes.TrainingDelivery.ConsequencesRetry, DriveOsPermissionCodes.TrainingDelivery.ReadOnly);
        Assert.DoesNotContain(DriveOsPermissionCodes.TrainingDelivery.AttendanceOverride, DriveOsPermissionCodes.TrainingDelivery.ReadOnly);
    }

    [Fact]
    public void Instructor_receives_training_delivery_execution_permissions()
    {
        var permissions = DriveOsRolePermissionDefaults.GetPermissions(DriveOsRoleCodes.Instructor);

        Assert.Contains(DriveOsPermissionCodes.TrainingDelivery.SessionsRead, permissions);
        Assert.Contains(DriveOsPermissionCodes.TrainingDelivery.SessionsPrepare, permissions);
        Assert.Contains(DriveOsPermissionCodes.TrainingDelivery.SessionsStart, permissions);
        Assert.Contains(DriveOsPermissionCodes.TrainingDelivery.SessionsComplete, permissions);
        Assert.Contains(DriveOsPermissionCodes.TrainingDelivery.AttendanceRecord, permissions);
        Assert.Contains(DriveOsPermissionCodes.TrainingDelivery.IncidentsReport, permissions);
        Assert.Contains(DriveOsPermissionCodes.TrainingDelivery.ReportsRead, permissions);
        Assert.Contains(DriveOsPermissionCodes.TrainingDelivery.ReportsWrite, permissions);
        Assert.Contains(DriveOsPermissionCodes.TrainingDelivery.ReportsSubmit, permissions);
        Assert.DoesNotContain(DriveOsPermissionCodes.TrainingDelivery.ReportsMonitor, permissions);
        Assert.DoesNotContain(DriveOsPermissionCodes.TrainingDelivery.AttendanceOverride, permissions);
        Assert.DoesNotContain(DriveOsPermissionCodes.TrainingDelivery.ConsequencesRetry, permissions);
    }

}
