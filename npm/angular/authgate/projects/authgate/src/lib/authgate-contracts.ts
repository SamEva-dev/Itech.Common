/** Stable, non-sensitive codes for application-owned translations. */
export const AuthGateErrorCode = {
  ConfigurationInvalid: 'AUTHGATE_CONFIGURATION_INVALID',
  AuthorityInvalid: 'AUTHGATE_AUTHORITY_INVALID',
  ApiOriginInvalid: 'AUTHGATE_API_ORIGIN_INVALID',
  CallbackInvalid: 'AUTHGATE_CALLBACK_INVALID',
  ClientInvalid: 'AUTHGATE_CLIENT_INVALID',
  ScopeInvalid: 'AUTHGATE_SCOPE_INVALID',
  OidcTransactionInvalid: 'AUTHGATE_OIDC_TRANSACTION_INVALID',
  OidcResponseInvalid: 'AUTHGATE_OIDC_RESPONSE_INVALID',
  OidcAccessDenied: 'AUTHGATE_OIDC_ACCESS_DENIED',
  OidcUnavailable: 'AUTHGATE_OIDC_UNAVAILABLE',
  SessionCancelled: 'AUTHGATE_SESSION_CANCELLED',
  SessionBusy: 'AUTHGATE_SESSION_BUSY',
  SessionExpired: 'AUTHGATE_SESSION_EXPIRED',
  AccessUnavailable: 'AUTHGATE_ACCESS_UNAVAILABLE',
  HttpNetwork: 'AUTHGATE_HTTP_NETWORK',
  HttpUnauthorized: 'AUTHGATE_HTTP_UNAUTHORIZED',
  HttpForbidden: 'AUTHGATE_HTTP_FORBIDDEN',
  HttpRateLimited: 'AUTHGATE_HTTP_RATE_LIMITED',
  HttpUnavailable: 'AUTHGATE_HTTP_UNAVAILABLE',
  HttpFailure: 'AUTHGATE_HTTP_FAILURE',
  HttpCredentialInvalid: 'AUTHGATE_HTTP_CREDENTIAL_INVALID',
  ReturnUrlInvalid: 'AUTHGATE_RETURN_URL_INVALID',
} as const;
export type AuthGateErrorCode = typeof AuthGateErrorCode[keyof typeof AuthGateErrorCode];

export class AuthGateConfigurationError extends Error {
  constructor(readonly code: AuthGateErrorCode) {
    super(code); // Never attach URLs, tokens or configuration values.
    this.name = 'AuthGateConfigurationError';
  }
}

/** Identity is not a membership, permission, or proof of authorization. */
export interface AuthGateIdentity {
  readonly subject: string;
}

/** Public UI state deliberately contains no tokens, roles or organization IDs. */
export type AuthGateSessionState =
  | Readonly<{ status: 'anonymous' }>
  | Readonly<{ status: 'authenticating' }>
  | Readonly<{ status: 'authenticated'; identity: AuthGateIdentity }>
  | Readonly<{ status: 'expired' }>
  | Readonly<{ status: 'unavailable' }>;
