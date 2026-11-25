namespace LocaGuest.Security.Contracts;

public static class LocaGuestAuthorizationDenialReasonV2
{
    public const string PermissionDenied = "PERMISSION_DENIED";
    public const string FeatureNotIncluded = "FEATURE_NOT_INCLUDED";
    public const string QuotaExceeded = "QUOTA_EXCEEDED";
    public const string AuthorizationUnavailable = "AUTHORIZATION_UNAVAILABLE";
}
