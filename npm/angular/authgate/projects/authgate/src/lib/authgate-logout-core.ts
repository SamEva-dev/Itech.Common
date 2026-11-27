import { ValidatedAuthGateConfig } from './authgate-config';
import { AuthGateErrorCode } from './authgate-contracts';
import { AuthGateProtocolError } from './authgate-oidc';

const unavailable = () => new AuthGateProtocolError(AuthGateErrorCode.OidcUnavailable);
const markerPattern = /^[a-f0-9]{64}$/;
/** Non-secret epoch only. Never store or broadcast credentials, identities or permissions. */
export class AuthGateLogoutEpoch {
  readonly key: string;
  constructor(config: ValidatedAuthGateConfig, private readonly storage: Pick<Storage,'getItem'|'setItem'>,
    private readonly crypto: Crypto) { this.key = config.storageNamespace + ':logout-epoch'; }
  read(): string {
    try {
      const marker = this.storage.getItem(this.key);
      if (marker === null) return this.advance();
      if (!markerPattern.test(marker)) throw unavailable();
      return marker;
    } catch { throw unavailable(); }
  }
  advance(): string {
    try {
      const marker = Array.from(this.crypto.getRandomValues(new Uint8Array(32)), b => b.toString(16).padStart(2,'0')).join('');
      this.storage.setItem(this.key, marker);
      return marker;
    } catch { throw unavailable(); }
  }
}
export interface AuthGateLogoutResult {
  readonly local: 'cleared';
  readonly otherTabs: 'signalled'|'unavailable'|'not-requested';
  readonly pendingTransaction: 'cleared'|'unavailable';
  readonly remote: 'revoked'|'not-requested'|'no-token'|'unconfirmed';
}
interface LogoutSession { getAccessToken(): string|null; clearLocal(): void; }
export interface AuthGateLogoutOptions { readonly revokeCurrent?: boolean; readonly otherTabs?: boolean; }
/** Explicit operations only; current-token revocation is not central or all-device logout. */
export class AuthGateLogoutCore {
  constructor(private readonly config: ValidatedAuthGateConfig, private readonly session: LogoutSession,
    private readonly epoch: AuthGateLogoutEpoch, private readonly request: typeof fetch) {}
  logout(options: AuthGateLogoutOptions = {}): Promise<AuthGateLogoutResult> {
    const revoke = options.revokeCurrent === true, tabs = options.otherTabs === true;
    let token: string|null = null;
    try { if (revoke) token = this.session.getAccessToken(); } catch { /* local cleanup still required */ }
    let transaction: AuthGateLogoutResult['pendingTransaction'] = 'cleared';
    try { this.session.clearLocal(); } catch { transaction = 'unavailable'; }
    let otherTabs: AuthGateLogoutResult['otherTabs'] = 'not-requested';
    if (tabs) { try { this.epoch.advance(); otherTabs = 'signalled'; } catch { otherTabs = 'unavailable'; } }
    const result = Promise.resolve().then(async (): Promise<AuthGateLogoutResult> => {
      const remote = !revoke ? 'not-requested' : !token ? 'no-token' : await this.#revoke(token);
      return Object.freeze({local:'cleared',otherTabs,pendingTransaction:transaction,remote});
    });
    return result;
  }
  async #revoke(token: string): Promise<AuthGateLogoutResult['remote']> {
    if (!/^[A-Za-z0-9._~+\/-]+={0,2}$/.test(token) || token.length > 16384) return 'unconfirmed';
    const abort = new AbortController();
    let timer: ReturnType<typeof setTimeout>|undefined;
    try {
      const timeout = new Promise<never>((_, reject) => {timer = setTimeout(() => {abort.abort(); reject(unavailable());},5000);});
      const response = await Promise.race([this.request(new URL('api/oidc/session/revoke-current',this.config.authority).href, {
        method:'POST', headers:{Authorization:`Bearer ${token}`}, signal:abort.signal, redirect:'error', credentials:'omit', cache:'no-store', referrerPolicy:'no-referrer',
      }),timeout]);
      if (response.body) void response.body.cancel().catch(() => undefined);
      return response.status === 204 && !response.redirected ? 'revoked' : 'unconfirmed';
    } catch { return 'unconfirmed'; } finally { if(timer !== undefined) clearTimeout(timer); }
  }
}
/** Top-level navigation only. AuthGate displays a CSRF-protected confirmation before closing its cookie. */
export function authGateEndSessionUrl(config: ValidatedAuthGateConfig): string {
  const url = new URL('connect/logout',config.authority);
  url.searchParams.set('client_id',config.clientId);
  if(config.postLogoutRedirectUri) url.searchParams.set('post_logout_redirect_uri',config.postLogoutRedirectUri);
  return url.href;
}
