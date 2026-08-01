using DomainRelay.Mapping.Expressions.Dynamic;

namespace Itech.Querying;

public static class DynamicQueryOptionsExtensions
{
    public static DynamicQueryOptions AddEquals<T>(this DynamicQueryOptions options, string memberName, T? value)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (ShouldSkip(value)) return options;

        options.Filters.Add(new DynamicFilter(memberName, DynamicFilterOperator.Equals, value));
        return options;
    }

    public static DynamicQueryOptions AddNotEquals<T>(this DynamicQueryOptions options, string memberName, T? value)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (ShouldSkip(value)) return options;

        options.Filters.Add(new DynamicFilter(memberName, DynamicFilterOperator.NotEquals, value));
        return options;
    }

    public static DynamicQueryOptions AddContains(this DynamicQueryOptions options, string memberName, string? value)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(value)) return options;

        options.Filters.Add(new DynamicFilter(memberName, DynamicFilterOperator.StringContains, value));
        return options;
    }

    public static DynamicQueryOptions AddGreaterThanOrEqual<T>(this DynamicQueryOptions options, string memberName, T? value)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (value is null) return options;

        options.Filters.Add(new DynamicFilter(memberName, DynamicFilterOperator.GreaterThanOrEqual, value));
        return options;
    }

    public static DynamicQueryOptions AddLessThanOrEqual<T>(this DynamicQueryOptions options, string memberName, T? value)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (value is null) return options;

        options.Filters.Add(new DynamicFilter(memberName, DynamicFilterOperator.LessThanOrEqual, value));
        return options;
    }

    public static DynamicQueryOptions AddSortOrDefault(
        this DynamicQueryOptions options,
        string? sortBy,
        string? sortDirection,
        string defaultMemberName,
        DynamicSortDirection defaultDirection = DynamicSortDirection.Desc)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultMemberName);

        var memberName = string.IsNullOrWhiteSpace(sortBy) ? defaultMemberName : sortBy;
        var direction = string.IsNullOrWhiteSpace(sortBy)
            ? defaultDirection
            : ParseDirection(sortDirection);

        options.Sorts.Add(new DynamicSort(memberName, direction));
        return options;
    }

    private static bool ShouldSkip<T>(T? value) =>
        value is null || value is string text && string.IsNullOrWhiteSpace(text);

    private static DynamicSortDirection ParseDirection(string? sortDirection) =>
        string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            ? DynamicSortDirection.Desc
            : DynamicSortDirection.Asc;
}
