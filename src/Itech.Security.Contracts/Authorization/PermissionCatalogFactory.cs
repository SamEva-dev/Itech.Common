using System.Text;
using System.Text.RegularExpressions;

namespace Itech.Security.Contracts.Authorization;

/// <summary>
/// Builds stable permission metadata from the exhaustive code list owned by an application.
/// </summary>
public static partial class PermissionCatalogFactory
{
    public static IReadOnlyList<PermissionDefinition> Create(
        string applicationCode,
        IEnumerable<string> permissionCodes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationCode);
        ArgumentNullException.ThrowIfNull(permissionCodes);

        return permissionCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .Select(code => CreateDefinition(applicationCode, code))
            .ToArray();
    }

    private static PermissionDefinition CreateDefinition(string applicationCode, string code)
    {
        var segments = code.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var action = Humanize(segments.Length == 0 ? code : segments[^1]);
        var resourceSegments = segments.Length > 1 ? segments[..^1] : segments;
        var category = string.Join(" ", resourceSegments.Select(Humanize));

        if (string.IsNullOrWhiteSpace(category))
        {
            category = "General";
        }

        return new PermissionDefinition(
            applicationCode,
            code,
            $"{action} {category}".Trim(),
            $"Allows {action.ToLowerInvariant()} access to {category}.",
            category);
    }

    private static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Replace('-', ' ').Replace('_', ' ');
        normalized = PascalCaseBoundary().Replace(normalized, "$1 $2");
        normalized = MultiSpace().Replace(normalized, " ").Trim();

        var result = new StringBuilder(normalized.Length);
        for (var index = 0; index < normalized.Length; index++)
        {
            result.Append(index == 0
                ? char.ToUpperInvariant(normalized[index])
                : normalized[index]);
        }

        return result.ToString();
    }

    [GeneratedRegex("([a-z0-9])([A-Z])", RegexOptions.CultureInvariant)]
    private static partial Regex PascalCaseBoundary();

    [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
    private static partial Regex MultiSpace();
}
