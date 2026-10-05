namespace MobiOne.Security.Contracts;

/// <summary>
/// Stable authorization role codes shared by AuthGate, MobiOne.Api and clients.
/// Business feature permissions will be introduced incrementally with their
/// corresponding backend vertical slices instead of being guessed up front.
/// </summary>
public static class MobiOneRoleCodes
{
    public const string PlatformAdministrator = "MobiOne.PlatformAdministrator";
    public const string Dispatcher = "MobiOne.Dispatcher";
    public const string BusinessAdministrator = "MobiOne.BusinessAdministrator";
    public const string EstablishmentAdministrator = "MobiOne.EstablishmentAdministrator";
    public const string FleetAdministrator = "MobiOne.FleetAdministrator";
    public const string SupportAgent = "MobiOne.SupportAgent";
    public const string Driver = "MobiOne.Driver";
    public const string Customer = "MobiOne.Customer";

    public static readonly string[] All =
    [
        PlatformAdministrator,
        Dispatcher,
        BusinessAdministrator,
        EstablishmentAdministrator,
        FleetAdministrator,
        SupportAgent,
        Driver,
        Customer
    ];
}
