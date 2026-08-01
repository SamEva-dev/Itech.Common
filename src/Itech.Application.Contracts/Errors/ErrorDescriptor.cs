namespace Itech.Application.Contracts.Errors;

public sealed record ErrorDescriptor
{
    public ErrorDescriptor(
        string key,
        IReadOnlyDictionary<string, object?>? parameters = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        Key = key.Trim();
        Parameters = parameters ?? EmptyParameters;
    }

    private static readonly IReadOnlyDictionary<string, object?> EmptyParameters =
        new Dictionary<string, object?>();

    public string Key { get; }
    public IReadOnlyDictionary<string, object?> Parameters { get; }
}
