using Itech.Security.Contracts.Applications;

namespace PedagoraPilot.Security.Contracts;

public static class PedagoraPilotApplication
{
    public const string Code = "pedagora-pilot";

    public static ApplicationCode ApplicationCode { get; } = new(Code);
}
