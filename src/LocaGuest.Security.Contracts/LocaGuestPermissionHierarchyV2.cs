namespace LocaGuest.Security.Contracts;

public static class LocaGuestPermissionHierarchyV2
{
    private static readonly IReadOnlyDictionary<string, string> Implied =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [LocaGuestPermissionCodesV2.PortfolioManage] = LocaGuestPermissionCodesV2.PortfolioRead,
            [LocaGuestPermissionCodesV2.RentalManage] = LocaGuestPermissionCodesV2.RentalRead,
            [LocaGuestPermissionCodesV2.DocumentsManage] = LocaGuestPermissionCodesV2.DocumentsRead,
            [LocaGuestPermissionCodesV2.SignaturesManage] = LocaGuestPermissionCodesV2.SignaturesRead,
            [LocaGuestPermissionCodesV2.SeasonalManage] = LocaGuestPermissionCodesV2.SeasonalRead,
            [LocaGuestPermissionCodesV2.FinanceManage] = LocaGuestPermissionCodesV2.FinanceRead,
            [LocaGuestPermissionCodesV2.FieldManage] = LocaGuestPermissionCodesV2.FieldRead,
            [LocaGuestPermissionCodesV2.OrganizationManage] = LocaGuestPermissionCodesV2.OrganizationRead,
            [LocaGuestPermissionCodesV2.TeamManage] = LocaGuestPermissionCodesV2.TeamRead,
            [LocaGuestPermissionCodesV2.BillingManage] = LocaGuestPermissionCodesV2.BillingRead,
            [LocaGuestPermissionCodesV2.AssistantManage] = LocaGuestPermissionCodesV2.AssistantUse,
        };

    // Only call with verified V2 grants from the active membership.
    public static IReadOnlyList<string> Expand(IEnumerable<string> grants)
    {
        ArgumentNullException.ThrowIfNull(grants);
        var supplied = new HashSet<string>(grants, StringComparer.OrdinalIgnoreCase);
        var result = new HashSet<string>(LocaGuestPermissionCodesV2.All.Where(supplied.Contains), StringComparer.OrdinalIgnoreCase);
        foreach (var grant in result.ToArray())
            if (Implied.TryGetValue(grant, out var implied)) result.Add(implied);
        return Array.AsReadOnly(LocaGuestPermissionCodesV2.All.Where(result.Contains).ToArray());
    }

    public static bool Allows(IEnumerable<string> verifiedV2Grants, string required) =>
        Expand(verifiedV2Grants).Contains(required, StringComparer.OrdinalIgnoreCase);
}
