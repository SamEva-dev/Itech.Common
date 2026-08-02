using Itech.Security.Contracts.Applications;

namespace LocaGuest.Security.Contracts;

public static class LocaGuestApplication
{
    public const string Code = "locaguest";

    public static ApplicationCode ApplicationCode { get; } = new(Code);
}

