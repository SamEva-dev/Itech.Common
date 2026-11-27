import { AuthGateConfigurationError, AuthGateErrorCode } from './authgate-contracts';

export interface AuthGateConfig {
  readonly authority: string;
  readonly clientId: string;
  readonly apiAudience: string;
  readonly scope: string;
  readonly appOrigin: string;
  readonly redirectUri: string;
  readonly apiBaseUrl: string;
  readonly postLogoutRedirectUri?: string;
  /** Platform administration requires the distinct online platform validator. */
  readonly sessionValidation?: 'application' | 'platform';
  /** Explicit dev-only exception for localhost, 127.0.0.1 and [::1]. */
  readonly allowInsecureLoopback?: boolean;
}

export interface ValidatedAuthGateConfig extends AuthGateConfig {
  readonly apiOrigin: string;
  readonly storageNamespace: string;
  readonly callbackPath: string;
}

const loopback = new Set(['localhost', '127.0.0.1', '[::1]']);
const controls = /[\u0000-\u0020\u007f\\]/;
const clientPattern = /^[A-Za-z0-9][A-Za-z0-9._:-]{0,127}$/;
const scopePattern = /^[\x21\x23-\x5b\x5d-\x7e]+$/;

function fail(code: AuthGateErrorCode): never { throw new AuthGateConfigurationError(code); }

function url(value: string, allowLoopback: boolean, code: AuthGateErrorCode): URL {
  if (typeof value !== 'string' || value.length > 2048 || controls.test(value) || !/^https?:\/\//i.test(value)) fail(code);
  let parsed: URL;
  try { parsed = new URL(value); } catch { return fail(code); }
  if (parsed.username || parsed.password || parsed.search || parsed.hash || value.includes('?') || value.includes('#')) fail(code);
  if (parsed.protocol !== 'https:' && !(allowLoopback && parsed.protocol === 'http:' && loopback.has(parsed.hostname))) fail(code);
  return parsed;
}

function hasAmbiguousPath(path: string): boolean {
  // Reject encoded separators, dot segments, nested encodings, controls and malformed escapes.
  return /%(?:2f|5c|2e|25|0[0-9a-f]|1[0-9a-f]|7f)/i.test(path) || /%(?![0-9a-f]{2})/i.test(path);
}

export function validateAuthGateConfig(input: AuthGateConfig): ValidatedAuthGateConfig {
  if (!input || typeof input !== 'object' || (input.allowInsecureLoopback !== undefined && typeof input.allowInsecureLoopback !== 'boolean'))
    fail(AuthGateErrorCode.ConfigurationInvalid);
  if (input.sessionValidation !== undefined && input.sessionValidation !== 'application' && input.sessionValidation !== 'platform') fail(AuthGateErrorCode.ConfigurationInvalid);
  const allow = input.allowInsecureLoopback === true;
  const authority = url(input.authority, false, AuthGateErrorCode.AuthorityInvalid);
  // AuthGate's current discovery and connect endpoints are rooted at the origin.
  if (authority.pathname !== '/') fail(AuthGateErrorCode.AuthorityInvalid);
  if (typeof input.clientId !== 'string' || !clientPattern.test(input.clientId) || typeof input.apiAudience !== 'string' || !input.apiAudience || input.apiAudience.length > 256 || controls.test(input.apiAudience)) fail(AuthGateErrorCode.ClientInvalid);
  if (typeof input.scope !== 'string' || input.scope.length > 1024) fail(AuthGateErrorCode.ScopeInvalid);
  const scopes = input.scope.split(' ');
  if (!scopes.includes('openid') || scopes.some(s => !scopePattern.test(s)) || new Set(scopes).size !== scopes.length) fail(AuthGateErrorCode.ScopeInvalid);
  const app = url(input.appOrigin, allow, AuthGateErrorCode.CallbackInvalid);
  if (app.pathname !== '/') fail(AuthGateErrorCode.CallbackInvalid);
  const callback = url(input.redirectUri, allow, AuthGateErrorCode.CallbackInvalid);
  if (callback.origin !== app.origin || callback.pathname === '/' || hasAmbiguousPath(callback.pathname)) fail(AuthGateErrorCode.CallbackInvalid);
  const api = url(input.apiBaseUrl, allow, AuthGateErrorCode.ApiOriginInvalid);
  if (hasAmbiguousPath(api.pathname)) fail(AuthGateErrorCode.ApiOriginInvalid);
  const logout = input.postLogoutRedirectUri === undefined ? undefined : url(input.postLogoutRedirectUri, allow, AuthGateErrorCode.CallbackInvalid);
  if (logout && (logout.origin !== app.origin || hasAmbiguousPath(logout.pathname) || logout.pathname === callback.pathname)) fail(AuthGateErrorCode.CallbackInvalid);
  // Include all security context dimensions. This is a namespace, not secret storage.
  const storageNamespace = 'itech.authgate.v1:' + [authority.href, input.clientId, input.apiAudience, app.origin, api.href, callback.href].map(encodeURIComponent).join(':');
  return Object.freeze({ authority: authority.href, clientId: input.clientId, apiAudience: input.apiAudience,
    scope: scopes.join(' '), appOrigin: app.origin, redirectUri: callback.href, apiBaseUrl: api.href,
    postLogoutRedirectUri: logout?.href, allowInsecureLoopback: allow, apiOrigin: api.origin,
    sessionValidation: input.sessionValidation ?? 'application',
    storageNamespace, callbackPath: callback.pathname });
}

/** Safe local route only; never returns an external URL or a callback route. */
export function safeAuthGateReturnUrl(value: unknown, config: ValidatedAuthGateConfig): string {
  if (typeof value !== 'string' || value.length > 4096 || !value.startsWith('/') || value.startsWith('//') || controls.test(value)) return '/';
  const rawPath = value.split(/[?#]/, 1)[0] ?? '';
  if (hasAmbiguousPath(rawPath) || rawPath.split('/').some(s => s === '.' || s === '..')) return '/';
  try {
    const parsed = new URL(value, config.appOrigin);
    if (parsed.origin !== config.appOrigin || parsed.pathname.startsWith('//') || parsed.pathname.toLowerCase() === config.callbackPath.toLowerCase() || parsed.pathname.toLowerCase().startsWith(config.callbackPath.toLowerCase() + '/')) return '/';
    // Decode once to reject alternate spellings of the callback (e.g. /%61uth/callback).
    const decoded = decodeURIComponent(parsed.pathname).toLowerCase();
    const callback = decodeURIComponent(config.callbackPath).toLowerCase();
    const logout = config.postLogoutRedirectUri ? decodeURIComponent(new URL(config.postLogoutRedirectUri).pathname).toLowerCase() : undefined;
    const blocked = [callback, '/auth/logout', '/auth/logout-callback', '/auth/oidc-test', ...(logout && logout !== '/' ? [logout] : [])];
    if (/[;()?#%]/.test(decoded) || controls.test(decoded) || decoded.includes('//') || blocked.some(p => decoded === p || decoded.startsWith(p + '/'))) return '/';
    return parsed.pathname + parsed.search + parsed.hash;
  } catch { return '/'; }
}

/** Post-bootstrap navigation only. Enrollment/invitation entry routes are handled explicitly
 * by the application before this function is called. No Angular matrix or auxiliary outlets. */
export function safeAuthGateLandingUrl(value: unknown, fallback: string,
  additionalBlocked: readonly string[] = []): string {
  if (typeof value !== 'string' || value.length > 2048 || !value.startsWith('/') || value.startsWith('//') || controls.test(value)) return fallback;
  const rawPath = value.split(/[?#]/, 1)[0] ?? '';
  if (hasAmbiguousPath(rawPath)) return fallback;
  try {
    const path = decodeURIComponent(rawPath);
    if (controls.test(path) || /[;()?#%]/.test(path) || path.includes('//') || path.split('/').some(s => s === '.' || s === '..')) return fallback;
    const normalized = path.toLowerCase().replace(/\/+$/, '') || '/';
    const blocked = ['/auth', '/login', '/connexion', '/register', '/inscription',
      '/reset-password', '/forgot-password', '/confirm-email', '/confirmation-email',
      '/check-email', '/change-password', '/modifier-mot-de-passe', '/accept-invitation',
      ...additionalBlocked];
    if (normalized === '/' || blocked.some(prefix => normalized === prefix.toLowerCase() || normalized.startsWith(prefix.toLowerCase() + '/'))) return fallback;
    return value;
  } catch { return fallback; }
}
