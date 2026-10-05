using Itech.Security.Contracts.Applications;

namespace MobiOne.Security.Contracts;

public static class MobiOneApplication
{
    public const string Code = "mobione";

    public static ApplicationCode ApplicationCode { get; } = new(Code);
}
