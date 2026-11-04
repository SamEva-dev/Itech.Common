using PedagoraPilot.Security.Contracts;
using Xunit;
namespace PedagoraPilot.Security.Contracts.Tests;
public sealed class InternshipTutorPermissionsTests
{
    [Fact]
    public void Tutor_has_only_assigned_period_permissions()
    {
        var p = PedagoraPilotRolePermissionDefaults.GetPermissions(PedagoraPilotRoleCodes.InternshipTutor);
        Assert.Contains(PedagoraPilotPermissionCodes.Internships.TutorViewAssigned, p);
        Assert.Contains(PedagoraPilotPermissionCodes.Internships.TutorEvaluateAssigned, p);
        Assert.DoesNotContain(PedagoraPilotPermissionCodes.Internships.Manage, p);
        Assert.DoesNotContain(PedagoraPilotPermissionCodes.Internships.View, p);
    }
}
