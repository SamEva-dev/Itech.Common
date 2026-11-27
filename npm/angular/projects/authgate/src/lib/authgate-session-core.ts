import { AuthGateErrorCode, AuthGateSessionState } from './authgate-contracts';
import { AuthGateOidcClient, AuthGateProtocolError, AuthGateStartOptions } from './authgate-oidc';

type OidcFlow = Pick<AuthGateOidcClient, 'start' | 'complete' | 'cancelPending'>;
export interface AuthGateSessionClock {
  now(): number;
  monotonic(): number;
  schedule(callback: () => void, delayMs: number): () => void;
}
interface Credential { token: string; wallDeadline: number; monotonicDeadline: number; }
interface Operation<T> { readonly abort: AbortController; readonly promise: Promise<T>; }
const anonymous = Object.freeze({ status: 'anonymous' } as const);
const busy = () => new AuthGateProtocolError(AuthGateErrorCode.SessionBusy);
const cancelled = () => new AuthGateProtocolError(AuthGateErrorCode.SessionCancelled);
const safeError = (error: unknown) => error instanceof AuthGateProtocolError && Object.values(AuthGateErrorCode).includes(error.code)
  ? new AuthGateProtocolError(error.code) : new AuthGateProtocolError(AuthGateErrorCode.OidcUnavailable);

/** One instance per configured Angular root. No browser token persistence or automatic redirects. */
export class AuthGateSessionCore {
  #state: AuthGateSessionState = anonymous;
  #credential?: Credential;
  #cancelTimer?: () => void;
  #start?: Operation<void>;
  #complete?: Operation<string>;
  #redirectPending = false;
  #destroyed = false;
  #epoch?: string;
  constructor(private readonly oidc: OidcFlow, private readonly clock: AuthGateSessionClock,
    private readonly publish: (state: AuthGateSessionState) => void = () => undefined,
    private readonly readEpoch?: () => string) { this.#epoch = readEpoch?.(); }

  /** Reconcile actual storage, not the possibly stale value carried by a storage event. */
  syncLogout(): boolean {
    try {
      const current = this.readEpoch?.();
      if (current === this.#epoch) return true;
      this.#epoch = current;
    } catch { /* fail closed if the epoch cannot be read */ }
    try { this.clearLocal(); } catch { /* credential is already cleared */ }
    return false;
  }

  get state(): AuthGateSessionState { this.syncLogout(); this.#expireIfNeeded(); return this.#state; }
  getAccessToken(): string | null { this.syncLogout(); this.#expireIfNeeded(); return this.#credential?.token ?? null; }

  /** A fresh top-level code flow; the server's cookie may supply SSO. No silent refresh or retry. */
  start(returnUrl?: string, options?: AuthGateStartOptions): Promise<void> {
    if (this.#destroyed) return Promise.reject(cancelled());
    if (!this.syncLogout()) {
      try { if (this.readEpoch?.() !== this.#epoch) return Promise.reject(cancelled()); } catch { return Promise.reject(cancelled()); }
    }
    if (this.#start) return this.#start.promise;
    if (this.#complete) return Promise.reject(busy());
    if (this.#redirectPending) return Promise.resolve();
    this.#forgetCredential();
    const abort = new AbortController();
    // Defer the work so concurrent callers observe the same operation before navigation.
    const promise = Promise.resolve().then(async () => {
      try {
        if (abort.signal.aborted || !this.syncLogout()) throw cancelled();
        await this.oidc.start(returnUrl, abort.signal, options);
        if (abort.signal.aborted || !this.syncLogout()) throw cancelled();
        this.#redirectPending = true;
      } catch (error) {
        if (abort.signal.aborted) throw cancelled();
        this.#setState({ status: 'unavailable' });
        throw safeError(error);
      } finally { if (this.#start?.abort === abort) this.#start = undefined; }
    });
    this.#start = { abort, promise };
    this.#setState({ status: 'authenticating' });
    return promise;
  }
  renew(returnUrl?: string, options?: AuthGateStartOptions): Promise<void> { return this.start(returnUrl, options); }

  /** Complete exactly one callback operation; only the safe return route is returned to the UI. */
  complete(): Promise<string> {
    if (this.#destroyed) return Promise.reject(cancelled());
    // Let OIDC scrub the callback URL even after a logout invalidates its transaction.
    this.syncLogout();
    if (this.#complete) return this.#complete.promise;
    if (this.#start || this.getAccessToken()) return Promise.reject(busy());
    this.#redirectPending = false;
    const abort = new AbortController();
    const promise = Promise.resolve().then(async () => {
      try {
        if (abort.signal.aborted) throw cancelled();
        const result = await this.oidc.complete(abort.signal);
        if (abort.signal.aborted || !this.syncLogout()) throw cancelled();
        const duration = result.expiresIn * 1000 - 5000;
        if (!Number.isSafeInteger(result.expiresIn) || result.expiresIn > 86400 || duration <= 0 || !result.accessToken || !result.identity.subject)
          throw new AuthGateProtocolError(AuthGateErrorCode.SessionExpired);
        const wall = this.clock.now(), monotonic = this.clock.monotonic();
        if (!Number.isFinite(wall) || !Number.isFinite(monotonic)) throw new AuthGateProtocolError(AuthGateErrorCode.OidcUnavailable);
        const credential = { token: result.accessToken, wallDeadline: wall + duration, monotonicDeadline: monotonic + duration };
        this.#credential = credential;
        this.#armExpiry(credential);
        this.#setState({ status: 'authenticated', identity: Object.freeze({ subject: result.identity.subject }) });
        return result.returnUrl;
      } catch (error) {
        if (abort.signal.aborted) throw cancelled();
        this.#forgetCredential();
        const safe = safeError(error);
        this.#setState({ status: safe.code === AuthGateErrorCode.SessionExpired ? 'expired' : 'unavailable' });
        throw safe;
      } finally { if (this.#complete?.abort === abort) this.#complete = undefined; }
    });
    this.#complete = { abort, promise };
    this.#setState({ status: 'authenticating' });
    return promise;
  }

  /** A delayed 401 for a previous token must not invalidate a newer session. */
  invalidateIfCurrent(token: string): boolean {
    if (!token || this.getAccessToken() !== token) return false;
    this.clearLocal();
    return true;
  }
  /** Local only: no promise of remote logout/revocation. */
  clearLocal(): void {
    this.#reset();
    this.oidc.cancelPending();
  }
  /** Do not delete the pending transaction on Angular destruction/navigation. */
  destroy(): void { this.#destroyed = true; this.#reset(); }
  #reset(): void {
    this.#start?.abort.abort(); this.#complete?.abort.abort();
    this.#start = undefined; this.#complete = undefined; this.#redirectPending = false;
    this.#forgetCredential(); this.#setState(anonymous);
  }
  #setState(state: AuthGateSessionState): void {
    this.#state = Object.freeze(state); this.publish(this.#state);
  }
  #forgetCredential(): void {
    this.#credential = undefined; this.#cancelTimer?.(); this.#cancelTimer = undefined;
  }
  #remaining(c: Credential): number {
    const remaining = Math.min(c.wallDeadline - this.clock.now(), c.monotonicDeadline - this.clock.monotonic());
    return Number.isFinite(remaining) ? remaining : 0;
  }
  #expireIfNeeded(): void {
    if (this.#credential && this.#remaining(this.#credential) <= 0) {
      this.#forgetCredential(); this.#setState({ status: 'expired' });
    }
  }
  #armExpiry(credential: Credential): void {
    this.#cancelTimer = this.clock.schedule(() => {
      if (this.#credential !== credential) return;
      this.#expireIfNeeded();
      if (this.#credential === credential) this.#armExpiry(credential);
    }, Math.max(1, Math.ceil(this.#remaining(credential))));
  }
}
