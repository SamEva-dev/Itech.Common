import { ValidatedAuthGateConfig, safeAuthGateReturnUrl } from './authgate-config';
import { AuthGateErrorCode } from './authgate-contracts';

export class AuthGateProtocolError extends Error {
  constructor(readonly code: AuthGateErrorCode) { super(code); this.name = 'AuthGateProtocolError'; }
}
const fail = (code: AuthGateErrorCode): never => { throw new AuthGateProtocolError(code); };
const invalid = (): never => fail(AuthGateErrorCode.OidcResponseInvalid);
const textEncoder = new TextEncoder();
const b64 = (bytes: Uint8Array): string => btoa(String.fromCharCode(...bytes)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
function decode(value: string): Uint8Array<ArrayBuffer> {
  if (!/^[A-Za-z0-9_-]+$/.test(value) || value.length % 4 === 1) return invalid();
  const bytes = Uint8Array.from(atob(value.replace(/-/g, '+').replace(/_/g, '/')), c => c.charCodeAt(0));
  if (b64(bytes) !== value) return invalid();
  return bytes;
}
function object(value: unknown): Record<string, unknown> {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return invalid();
  return value as Record<string, unknown>;
}
function jwtPart(value: string): Record<string, unknown> {
  return object(JSON.parse(new TextDecoder('utf-8', { fatal: true }).decode(decode(value))));
}
const opaque = (v: unknown, max = 16384): v is string => typeof v === 'string' && v.length > 0 && v.length <= max && !/[\s\u0000-\u001f\u007f]/.test(v);
const randomPattern = /^[A-Za-z0-9_-]{43}$/;
const randomValue = (v: unknown): v is string => typeof v === 'string' && randomPattern.test(v);
const transactionLifetimeMs = 600_000;

/** Browser adapter: only the pending transaction is stored, never tokens. */
export interface AuthGateOidcBrowser {
  readonly storage: Pick<Storage, 'getItem' | 'setItem' | 'removeItem'>;
  readonly crypto: Crypto;
  readonly fetch: typeof fetch;
  currentUrl(): string;
  replaceUrl(path: string): void;
  navigate(url: string): void;
  now(): number;
  /** Optional non-secret cross-tab logout epoch, captured before navigation. */
  logoutEpoch?(): string;
}
export interface AuthGateLoginResult {
  readonly accessToken: string;
  readonly expiresIn: number;
  readonly identity: Readonly<{ subject: string }>;
  readonly returnUrl: string;
}
export interface AuthGateStartOptions {
  readonly loginHint?: string;
  readonly maxAgeSeconds?: 1;
}
interface Transaction {
  readonly state: string; readonly nonce: string; readonly verifier: string;
  readonly namespace: string; readonly created: number; readonly returnUrl: string;
  readonly scope: string;
  readonly logoutEpoch?: string;
}

/** AuthGate-specific public-client code flow. No client secret, token persistence or automatic retry. */
export class AuthGateOidcClient {
  private readonly key: string;
  private starting = false;
  constructor(private readonly config: ValidatedAuthGateConfig, private readonly browser: AuthGateOidcBrowser) {
    this.key = config.storageNamespace + ':oidc-transaction';
  }
  async start(returnUrl?: string, signal?: AbortSignal, options: AuthGateStartOptions = {}): Promise<void> {
    if (signal?.aborted || this.starting) return fail(AuthGateErrorCode.OidcTransactionInvalid);
    this.starting = true;
    try {
      if (new URL(this.browser.currentUrl()).origin !== this.config.appOrigin) return fail(AuthGateErrorCode.OidcTransactionInvalid);
      const random = () => b64(this.browser.crypto.getRandomValues(new Uint8Array(32)));
      const tx: Transaction = { state: random(), nonce: random(), verifier: random(), namespace: this.config.storageNamespace,
        created: this.browser.now(), returnUrl: safeAuthGateReturnUrl(returnUrl, this.config), scope: this.config.scope, logoutEpoch: this.browser.logoutEpoch?.() };
      const challenge = b64(new Uint8Array(await this.browser.crypto.subtle.digest('SHA-256', textEncoder.encode(tx.verifier))));
      if (signal?.aborted) return fail(AuthGateErrorCode.SessionCancelled);
      const url = new URL('connect/authorize', this.config.authority);
      url.search = new URLSearchParams({ client_id: this.config.clientId, redirect_uri: this.config.redirectUri,
        response_type: 'code', response_mode: 'query', scope: this.config.scope, state: tx.state, nonce: tx.nonce,
        code_challenge: challenge, code_challenge_method: 'S256' }).toString();
      if (options.loginHint !== undefined) {
        if (typeof options.loginHint !== 'string' || options.loginHint.length > 254 || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(options.loginHint) || /[\u0000-\u001f\u007f]/.test(options.loginHint)) return fail(AuthGateErrorCode.OidcTransactionInvalid);
        url.searchParams.set('login_hint', options.loginHint);
      }
      if (options.maxAgeSeconds !== undefined) {
        if (options.maxAgeSeconds !== 1) return fail(AuthGateErrorCode.OidcTransactionInvalid);
        url.searchParams.set('max_age', '1');
      }
      if (tx.logoutEpoch !== this.browser.logoutEpoch?.()) return fail(AuthGateErrorCode.SessionCancelled);
      this.browser.storage.setItem(this.key, JSON.stringify(tx));
      try { this.browser.navigate(url.href); } catch { this.browser.storage.removeItem(this.key); return fail(AuthGateErrorCode.OidcUnavailable); }
    } catch (error) {
      if (error instanceof AuthGateProtocolError) throw error;
      return fail(AuthGateErrorCode.OidcUnavailable);
    } finally { this.starting = false; }
  }
  /** Cancel only this application's pending transaction. Does not revoke server sessions. */
  cancelPending(): void {
    try { this.browser.storage.removeItem(this.key); }
    catch { fail(AuthGateErrorCode.OidcUnavailable); }
  }
  /** Call only on the configured callback. A failed callback requires a fresh login. */
  async complete(signal?: AbortSignal): Promise<AuthGateLoginResult> {
    try {
      if (signal?.aborted) return fail(AuthGateErrorCode.SessionCancelled);
      const callback = new URL(this.browser.currentUrl());
      if (callback.origin !== this.config.appOrigin || callback.pathname !== this.config.callbackPath)
        return fail(AuthGateErrorCode.OidcTransactionInvalid);
      // Scrub before the first await or network call, even if the transaction is absent.
      this.browser.replaceUrl(this.config.callbackPath);
      const raw = this.browser.storage.getItem(this.key);
      this.browser.storage.removeItem(this.key);
      if (!raw || raw.length > 16384) return fail(AuthGateErrorCode.OidcTransactionInvalid);
      const tx = object(JSON.parse(raw));
      if (tx.logoutEpoch !== this.browser.logoutEpoch?.()) return fail(AuthGateErrorCode.SessionCancelled);
      const age = this.browser.now() - Number(tx.created);
      if (tx.namespace !== this.config.storageNamespace || tx.scope !== this.config.scope || !randomValue(tx.state) || !randomValue(tx.nonce) || !randomValue(tx.verifier) ||
          typeof tx.created !== 'number' || !Number.isFinite(age) || age < 0 || age > transactionLifetimeMs)
        return fail(AuthGateErrorCode.OidcTransactionInvalid);
      const query = callback.searchParams;
      let duplicate = false;
      query.forEach((_v, k) => { if (query.getAll(k).length !== 1) duplicate = true; });
      if (callback.hash || duplicate ||
          query.get('state') !== tx.state || query.get('iss') !== this.config.authority || query.has('access_token') || query.has('id_token')) return invalid();
      if (query.has('error')) {
        if (query.has('code')) return invalid();
        return fail(query.get('error') === 'access_denied' ? AuthGateErrorCode.OidcAccessDenied : AuthGateErrorCode.OidcResponseInvalid);
      }
      const code = query.get('code');
      if (!opaque(code, 8192)) return invalid();
      const started = this.browser.now();
      const abort = new AbortController();
      const cancel = () => abort.abort();
      signal?.addEventListener('abort', cancel, { once: true });
      if (signal?.aborted) cancel();
      const timer = setTimeout(cancel, 15000);
      try {
        const token = await this.json(new URL('connect/token', this.config.authority), abort.signal, 65536, {
          method: 'POST', body: new URLSearchParams({ grant_type: 'authorization_code', client_id: this.config.clientId,
            redirect_uri: this.config.redirectUri, code, code_verifier: tx.verifier }) });
        if (!opaque(token.access_token) || !opaque(token.id_token) || typeof token.token_type !== 'string' || token.token_type.toLowerCase() !== 'bearer' ||
            typeof token.expires_in !== 'number' || !Number.isSafeInteger(token.expires_in) || token.expires_in <= 0 || token.expires_in > 86400) return invalid();
        const subject = await this.verifyIdentity(token.id_token, tx.nonce, token.access_token, abort.signal);
        const expiresIn = token.expires_in - Math.ceil(Math.max(0, this.browser.now() - started) / 1000);
        if (tx.logoutEpoch !== this.browser.logoutEpoch?.()) return fail(AuthGateErrorCode.SessionCancelled);
        if (expiresIn <= 0 || abort.signal.aborted) return invalid();
        return Object.freeze({ accessToken: token.access_token, expiresIn, identity: Object.freeze({ subject }), returnUrl: safeAuthGateReturnUrl(tx.returnUrl, this.config) });
      } finally { clearTimeout(timer); signal?.removeEventListener('abort', cancel); }
    } catch (error) {
      if (error instanceof AuthGateProtocolError) throw error;
      // Never expose raw URLs, server error descriptions, tokens or parser errors.
      return fail(AuthGateErrorCode.OidcUnavailable);
    }
  }
  private async json(url: URL, signal: AbortSignal, limit: number, init: RequestInit = {}): Promise<Record<string, unknown>> {
    if (url.origin !== new URL(this.config.authority).origin || url.username || url.password || url.search || url.hash) return invalid();
    const response = await this.browser.fetch(url.href, { ...init, signal, redirect: 'error', credentials: 'omit', cache: 'no-store', referrerPolicy: 'no-referrer', headers: { Accept: 'application/json' } });
    if (!response.ok || response.redirected || !/^application\/json(?:\s*;|$)/i.test(response.headers.get('content-type') ?? '') || !response.body) return invalid();
    const reader = response.body.getReader();
    const decoder = new TextDecoder('utf-8', { fatal: true });
    let size = 0, body = '';
    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        size += value.byteLength;
        if (size > limit) { void reader.cancel().catch(() => undefined); return invalid(); }
        body += decoder.decode(value, { stream: true });
      }
      body += decoder.decode();
      return object(JSON.parse(body));
    } finally { reader.releaseLock(); }
  }
  private async verifyIdentity(jwt: string, nonce: string, accessToken: string, signal: AbortSignal): Promise<string> {
    const parts = jwt.split('.');
    if (parts.length !== 3 || !parts[0] || !parts[1] || !parts[2]) return invalid();
    const header = jwtPart(parts[0]), payload = jwtPart(parts[1]);
    if (header.alg !== 'RS256' || !opaque(header.kid, 256) || (header.typ !== undefined && header.typ !== 'JWT') || ['crit','jku','jwk','x5u','x5c','b64'].some(k => Object.hasOwn(header, k))) return invalid();
    const discovery = await this.json(new URL('.well-known/openid-configuration', this.config.authority), signal, 65536);
    if (discovery.issuer !== this.config.authority || discovery.authorization_endpoint !== new URL('connect/authorize', this.config.authority).href ||
        discovery.token_endpoint !== new URL('connect/token', this.config.authority).href || typeof discovery.jwks_uri !== 'string') return invalid();
    const keys = await this.json(new URL(discovery.jwks_uri), signal, 65536);
    if (!Array.isArray(keys.keys) || keys.keys.length > 64) return invalid();
    const matches = keys.keys.filter(k => k && typeof k === 'object' && k.kid === header.kid);
    if (matches.length !== 1) return invalid();
    const key = object(matches[0]);
    if (key.kty !== 'RSA' || (key.use !== undefined && key.use !== 'sig') || (key.alg !== undefined && key.alg !== 'RS256') ||
        (key.key_ops !== undefined && (!Array.isArray(key.key_ops) || !key.key_ops.includes('verify'))) || !opaque(key.n, 2048) || !opaque(key.e, 16)) return invalid();
    const modulus = decode(key.n);
    if (modulus.length < 256 || modulus.length > 1024 || modulus[0] === 0) return invalid();
    const imported = await this.browser.crypto.subtle.importKey('jwk', { kty: 'RSA', n: key.n, e: key.e, alg: 'RS256', ext: true }, { name: 'RSASSA-PKCS1-v1_5', hash: 'SHA-256' }, false, ['verify']);
    if ((imported.algorithm as RsaHashedKeyAlgorithm).modulusLength < 2048 || !await this.browser.crypto.subtle.verify('RSASSA-PKCS1-v1_5', imported, decode(parts[2]), textEncoder.encode(parts[0] + '.' + parts[1]))) return invalid();
    const now = Math.floor(this.browser.now() / 1000);
    // A single audience is expected for AuthGate's public SPA ID tokens.
    const audience = payload.aud === this.config.clientId || (Array.isArray(payload.aud) && payload.aud.length === 1 && payload.aud[0] === this.config.clientId);
    if (payload.iss !== this.config.authority || !audience || (payload.azp !== undefined && payload.azp !== this.config.clientId) || payload.nonce !== nonce ||
        !opaque(payload.sub, 255) || typeof payload.exp !== 'number' || !Number.isSafeInteger(payload.exp) || payload.exp <= now ||
        typeof payload.iat !== 'number' || !Number.isSafeInteger(payload.iat) || payload.iat > now + 30 || payload.iat < now - 600 || payload.exp <= payload.iat ||
        (payload.nbf !== undefined && (typeof payload.nbf !== 'number' || !Number.isSafeInteger(payload.nbf) || payload.nbf > now + 30))) return invalid();
    if (payload.at_hash !== undefined) {
      const hash = new Uint8Array(await this.browser.crypto.subtle.digest('SHA-256', textEncoder.encode(accessToken)));
      if (payload.at_hash !== b64(hash.slice(0, 16))) return invalid();
    }
    return payload.sub;
  }
}
