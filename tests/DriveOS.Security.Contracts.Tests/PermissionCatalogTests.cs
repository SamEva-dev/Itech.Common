using FluentAssertions;
using Xunit;

namespace DriveOS.Security.Contracts.Tests;

public sealed class PermissionCatalogTests
{
    [Fact]
    public void Catalog_contains_every_declared_permission_once()
    {
        Assert.Equal(DriveOsPermissionCodes.All.Length, DriveOsPermissionCatalog.All.Count);
        Assert.All(
            DriveOsPermissionCodes.All,
            code => Assert.Single(DriveOsPermissionCatalog.All, item => item.Code == code));
    }

    [Fact]
    public void Catalog_is_owned_by_driveos()
    {
        Assert.All(
            DriveOsPermissionCatalog.All,
            item => Assert.Equal(DriveOsApplication.Code, item.ApplicationCode));
    }

    [Fact]
    public void Cross_tenant_permissions_are_reserved_to_platform_administrator_by_default()
    {
        var platformPermissions = DriveOsRolePermissionDefaults.GetPermissions(
            DriveOsRoleCodes.PlatformAdministrator
        );
        platformPermissions.Should().Contain(
            DriveOsPermissionCodes.Organizations.CrossTenantRead
        );
        platformPermissions.Should().Contain(
            DriveOsPermissionCodes.Organizations.CrossTenantManage
        );

        foreach (
            string role in DriveOsRoleCodes.All.Where(
                role => role != DriveOsRoleCodes.PlatformAdministrator
            )
        )
        {
            var permissions = DriveOsRolePermissionDefaults.GetPermissions(role);
            permissions.Should().NotContain(
                DriveOsPermissionCodes.Organizations.CrossTenantRead
            );
            permissions.Should().NotContain(
                DriveOsPermissionCodes.Organizations.CrossTenantManage
            );
        }
    }
}

public sealed class ExamsPermissionCatalogTests
{
    [Fact]
    public void Exams_permissions_should_be_unique_and_part_of_global_catalog()
    {
        DriveOsPermissionCodes.Exams.All.Should().OnlyHaveUniqueItems();
        DriveOsPermissionCodes.All.Should().Contain(DriveOsPermissionCodes.Exams.All);
    }

    [Fact]
    public void ExamCoordinator_should_receive_exam_permissions()
    {
        DriveOsRolePermissionDefaults.GetPermissions(DriveOsRoleCodes.ExamCoordinator)
            .Should().Contain(DriveOsPermissionCodes.Exams.All);
    }

    [Fact]
    public void ReadOnly_should_not_submit_exam_opinions()
    {
        DriveOsPermissionCodes.Exams.ReadOnly.Should().Contain(DriveOsPermissionCodes.Exams.ReadinessReadOpinions);
        DriveOsPermissionCodes.Exams.ReadOnly.Should().NotContain(DriveOsPermissionCodes.Exams.ReadinessSubmitOpinion);
    }

    [Fact]
    public void Instructor_should_not_manage_exam_places_or_results()
    {
        var permissions = DriveOsRolePermissionDefaults.GetPermissions(DriveOsRoleCodes.Instructor);
        permissions.Should().Contain(DriveOsPermissionCodes.Exams.ReadinessRead);
        permissions.Should().Contain(DriveOsPermissionCodes.Exams.ReadinessEvaluate);
        permissions.Should().Contain(DriveOsPermissionCodes.Exams.ReadinessSubmitOpinion);
        permissions.Should().Contain(DriveOsPermissionCodes.Exams.ReadinessReadOpinions);
        permissions.Should().NotContain(DriveOsPermissionCodes.Exams.PlacesManage);
        permissions.Should().NotContain(DriveOsPermissionCodes.Exams.ResultsRecord);
    }
}


public sealed class FleetPermissionCatalogTests
{
    [Fact]
    public void Fleet_permissions_should_be_unique_and_part_of_global_catalog()
    {
        DriveOsPermissionCodes.Fleet.All.Should().OnlyHaveUniqueItems();
        DriveOsPermissionCodes.All.Should().Contain(DriveOsPermissionCodes.Fleet.All);
    }

    [Fact]
    public void FleetManager_should_receive_all_fleet_permissions()
    {
        DriveOsRolePermissionDefaults.GetPermissions(DriveOsRoleCodes.FleetManager)
            .Should().Contain(DriveOsPermissionCodes.Fleet.All);
    }
}

public sealed class WorkforcePermissionCatalogTests
{
    [Fact]
    public void Workforce_read_only_contains_analytics_permission()
        => Assert.Contains(DriveOsPermissionCodes.Workforce.AnalyticsRead, DriveOsPermissionCodes.Workforce.ReadOnly);

    [Fact]
    public void Workforce_all_contains_rehire_permission()
        => Assert.Contains(DriveOsPermissionCodes.Workforce.EmployeesRehire, DriveOsPermissionCodes.Workforce.All);

    [Fact]
    public void Workforce_permissions_should_be_unique_and_part_of_global_catalog()
    {
        DriveOsPermissionCodes.Workforce.All.Should().OnlyHaveUniqueItems();
        DriveOsPermissionCodes.All.Should().Contain(DriveOsPermissionCodes.Workforce.All);
    }

    [Fact]
    public void WorkforceManager_should_receive_all_workforce_permissions()
    {
        DriveOsRolePermissionDefaults.GetPermissions(DriveOsRoleCodes.WorkforceManager)
            .Should().Contain(DriveOsPermissionCodes.Workforce.All);
    }

    [Fact]
    public void Professional_marketplace_catalog_is_complete_and_unique()
    {
        Assert.NotEmpty(DriveOsPermissionCodes.ProfessionalMarketplace.All);
        Assert.Equal(
            DriveOsPermissionCodes.ProfessionalMarketplace.All.Length,
            DriveOsPermissionCodes.ProfessionalMarketplace.All.Distinct(StringComparer.Ordinal).Count());

        Assert.All(
            DriveOsPermissionCodes.ProfessionalMarketplace.All,
            code => Assert.Contains(DriveOsPermissionCatalog.All, item => item.Code == code));
    }

    [Fact]
    public void Professional_marketplace_manager_receives_all_marketplace_permissions()
    {
        IReadOnlyCollection<string> permissions =
            DriveOsRolePermissionDefaults.GetPermissions(DriveOsRoleCodes.ProfessionalMarketplaceManager);

        Assert.All(
            DriveOsPermissionCodes.ProfessionalMarketplace.All,
            permission => Assert.Contains(permission, permissions));
    }

}
