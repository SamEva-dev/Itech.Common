namespace Itech.Security.Contracts.Applications;

public readonly record struct ApplicationCode
{
    public const int MaximumLength = 100;

    public ApplicationCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > MaximumLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                $"An application code cannot exceed {MaximumLength} characters.");
        }

        Value = normalized;
    }

    public string Value { get; }

    public override string ToString() => Value;

    public static implicit operator string(ApplicationCode code) => code.Value;

    public static explicit operator ApplicationCode(string value) => new(value);
}
