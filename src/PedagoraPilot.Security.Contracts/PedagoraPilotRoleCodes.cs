namespace PedagoraPilot.Security.Contracts;

public static class PedagoraPilotRoleCodes
{
    public const string PlatformAdministrator = "PedagoraPilot.PlatformAdministrator";
    public const string OrganizationAdministrator = "PedagoraPilot.OrganizationAdministrator";
    public const string OrganizationDirection = "PedagoraPilot.OrganizationDirection";
    public const string SiteDirection = "PedagoraPilot.SiteDirection";
    public const string PedagogicalManager = "PedagoraPilot.PedagogicalManager";
    public const string Secretariat = "PedagoraPilot.Secretariat";
    public const string Trainer = "PedagoraPilot.Trainer";
    public const string Student = "PedagoraPilot.Student";
    public const string Jury = "PedagoraPilot.Jury";
    public const string ReadOnly = "PedagoraPilot.ReadOnly";

    public static readonly string[] All =
    [
        PlatformAdministrator,
        OrganizationAdministrator,
        OrganizationDirection,
        SiteDirection,
        PedagogicalManager,
        Secretariat,
        Trainer,
        Student,
        Jury,
        ReadOnly
    ];
}
