import { ValidatedAuthGateConfig } from './authgate-config';
import { AuthGateErrorCode } from './authgate-contracts';

// Fail closed for paths that proxies/servers could decode or normalize differently.
const unsafeUrl = /[\u0000-\u0020\u007f\\]/;
const unsafePath = /%(?:2f|5c|2e|25|3b|3f|23|0[0-9a-f]|1[0-9a-f]|7f)/i;
function safePath(path: string): boolean {
  if (unsafePath.test(path) || /%(?![0-9a-f]{2})/i.test(path) || path.includes(';') ||
      path.includes('//') || path.split('/').some(s => s === '.' || s === '..')) return false;
  try { decodeURIComponent(path); return true; } catch { return false; }
}

/** Resolve against the actual document base URI, not the API base URL. Returns an absolute URL or null. */
export function resolveAuthGateApiUrl(value: string, documentBase: string, config: ValidatedAuthGateConfig): string | null {
  if (typeof value !== 'string' || !value || value.length > 16384 || unsafeUrl.test(value) || value.includes('#') || value.startsWith('//')) return null;
  try {
    const base = new URL(documentBase);
    if (base.origin !== config.appOrigin || base.username || base.password) return null;
    // Reject malformed absolute URLs before URL's forgiving parser normalizes them.
    const hasScheme = /^[a-z][a-z0-9+.-]*:/i.test(value);
    if (hasScheme && !/^https?:\/\/[^/]/i.test(value)) return null;
    const rawPath = (hasScheme ? value.replace(/^https?:\/\/[^/?#]+/i, '') : value).split('?')[0] ?? '';
    if (!safePath(rawPath)) return null;
    const target = new URL(value, base);
    const api = new URL(config.apiBaseUrl);
    if (target.origin !== api.origin || target.username || target.password || target.hash || !safePath(target.pathname)) return null;
    // AuthGate's protocol uses the dedicated OIDC client, never the API interceptor.
    if (target.origin === new URL(config.authority).origin) return null;
    const prefix = api.pathname.replace(/\/+$/, '');
    // A shared SPA origin must have an explicit API path; do not attach tokens to assets or arbitrary routes.
    if (!prefix && target.origin === config.appOrigin) return null;
    if (prefix && target.pathname !== prefix && !target.pathname.startsWith(prefix + '/')) return null;
    return target.href;
  } catch { return null; }
}

/** RFC 6750 bearer credential syntax; avoid malformed header values. */
export function isAuthGateBearerToken(value: string): boolean {
  return typeof value === 'string' && value.length <= 65536 && /^[A-Za-z0-9._~+\/-]+=*$/.test(value);
}

export interface AuthGateHttpFailure {
  readonly code: AuthGateErrorCode;
  readonly status: number;
  readonly sessionInvalidated: boolean;
}

export function classifyAuthGateHttpStatus(status: number): AuthGateErrorCode {
  switch (status) {
    case 0: return AuthGateErrorCode.HttpNetwork;
    case 401: return AuthGateErrorCode.HttpUnauthorized;
    case 403: return AuthGateErrorCode.HttpForbidden;
    case 429: return AuthGateErrorCode.HttpRateLimited;
    case 502: case 503: case 504: return AuthGateErrorCode.HttpUnavailable;
    default: return AuthGateErrorCode.HttpFailure;
  }
}

/** A delayed failure must never invalidate a replacement session or publish a stale session-expired event. */
export function handleAuthGateHttpFailure(status: number, sentToken: string | null,
  session: { getAccessToken(): string | null; invalidateIfCurrent(token: string): boolean }): AuthGateHttpFailure | null {
  let invalidated = false;
  if (sentToken !== null) {
    if (session.getAccessToken() !== sentToken) return null;
    if (status === 401) {
      try { invalidated = session.invalidateIfCurrent(sentToken); }
      catch {
        // clearLocal clears the in-memory token before attempting transaction-storage cleanup.
        invalidated = session.getAccessToken() === null;
      }
      if (!invalidated) return null;
    }
  } else if (session.getAccessToken() !== null) {
    // Anonymous request started before a successful login.
    return null;
  }
  return Object.freeze({ code: classifyAuthGateHttpStatus(status), status, sessionInvalidated: invalidated });
}
