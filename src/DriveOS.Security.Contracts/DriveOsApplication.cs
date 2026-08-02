using Itech.Security.Contracts.Applications;

namespace DriveOS.Security.Contracts;

public static class DriveOsApplication
{
    public const string Code = "driveos";

    public static ApplicationCode ApplicationCode { get; } = new(Code);
}

