namespace LocaGuest.Security.Contracts;

public static class LocaGuestPlanCodesV2
{
    public const string Free = "free";
    public const string Starter = "starter";
    public const string Pro = "pro";
    public const string Business = "business";
    public const string Enterprise = "enterprise";
    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(new string[] { Free, Starter, Pro, Business, Enterprise });
}
