namespace Itech.Security.Contracts.Authorization;

/// <summary>
/// Portable metadata for an application permission.
/// </summary>
public sealed record PermissionDefinition(
    string ApplicationCode,
    string Code,
    string DisplayName,
    string Description,
    string Category);
