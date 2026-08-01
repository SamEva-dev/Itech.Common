using Itech.Security.Contracts.Applications;

namespace Itech.Security.Contracts.Authorization;

public readonly record struct AuthorizationContext(
    ApplicationCode ApplicationCode,
    Guid OrganizationId);
