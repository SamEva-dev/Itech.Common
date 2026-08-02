namespace DriveOS.Security.Contracts;

/// <summary>
/// Built-in role codes proposed for DriveOS.
///
/// These authorization roles are distinct from operational branch assignment
/// roles such as Instructor or Accountant. A user may have an authorization
/// role and one or more dated branch assignments at the same time.
/// </summary>
public static class DriveOsRoleCodes
{
    public const string PlatformAdministrator =
        "DriveOS.PlatformAdministrator";

    public const string OrganizationOwner =
        "DriveOS.OrganizationOwner";

    public const string OrganizationAdministrator =
        "DriveOS.OrganizationAdministrator";

    public const string Director =
        "DriveOS.Director";

    public const string BranchManager =
        "DriveOS.BranchManager";

    public const string PedagogicalManager =
        "DriveOS.PedagogicalManager";

    public const string AdministrativeManager =
        "DriveOS.AdministrativeManager";

    public const string Secretary =
        "DriveOS.Secretary";

    public const string Accountant =
        "DriveOS.Accountant";

    public const string FleetManager =
        "DriveOS.FleetManager";

    public const string ExamCoordinator =
        "DriveOS.ExamCoordinator";

    public const string Instructor =
        "DriveOS.Instructor";

    public const string SalesAdvisor =
        "DriveOS.SalesAdvisor";

    public const string ComplianceOfficer =
        "DriveOS.ComplianceOfficer";

    public const string TrainingCoordinator =
        "DriveOS.TrainingCoordinator";

    public const string Receptionist =
        "DriveOS.Receptionist";

    public const string SupportAgent =
        "DriveOS.SupportAgent";

    public const string ReadOnly =
        "DriveOS.ReadOnly";

    public static readonly string[] All =
    [
        PlatformAdministrator,
        OrganizationOwner,
        OrganizationAdministrator,
        Director,
        BranchManager,
        PedagogicalManager,
        AdministrativeManager,
        Secretary,
        Accountant,
        FleetManager,
        ExamCoordinator,
        Instructor,
        SalesAdvisor,
        ComplianceOfficer,
        TrainingCoordinator,
        Receptionist,
        SupportAgent,
        ReadOnly
    ];

    public static readonly string[] PlatformRoles =
    [
        PlatformAdministrator
    ];

    public static readonly string[] TenantAdministrationRoles =
    [
        OrganizationOwner,
        OrganizationAdministrator,
        Director
    ];

    public static readonly string[] BranchAdministrationRoles =
    [
        BranchManager,
        PedagogicalManager,
        AdministrativeManager
    ];
}
