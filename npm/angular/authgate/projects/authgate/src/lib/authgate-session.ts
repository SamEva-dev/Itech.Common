import { DestroyRef, Signal, signal } from '@angular/core';
import { AuthGateSessionState } from './authgate-contracts';
import { AuthGateOidcClient, AuthGateStartOptions } from './authgate-oidc';
import { AuthGateLogoutEpoch } from './authgate-logout-core';
import { AuthGateSessionCore } from './authgate-session-core';
import { AuthGateSessionMonitor, verifyAuthGateSession } from './authgate-session-monitor';
import { ValidatedAuthGateConfig } from './authgate-config';

/** Angular facade; the public signal contains neither tokens nor permissions. */
export class AuthGateSession {
  readonly state: Signal<AuthGateSessionState>;
  readonly #core: AuthGateSessionCore;
  readonly #monitor?: AuthGateSessionMonitor;
  constructor(oidc: AuthGateOidcClient, destroyRef: DestroyRef, epoch?: AuthGateLogoutEpoch, config?: ValidatedAuthGateConfig) {
    const state = signal<AuthGateSessionState>(Object.freeze({ status: 'anonymous' }));
    this.state = state.asReadonly();
    const clock = {
      now: () => Date.now(), monotonic: () => performance.now(),
      schedule: (callback: () => void, delay: number) => { const timer = setTimeout(callback, delay); return () => clearTimeout(timer); },
    };
    this.#core = new AuthGateSessionCore(oidc, clock, next => {
      state.set(next);
      if (next.status === 'authenticated') this.#monitor?.bind(this.#core.getAccessToken(), next.identity.subject);
      else this.#monitor?.bind(null);
    }, epoch ? () => epoch.read() : undefined);
    if (config && typeof window !== 'undefined') {
      this.#monitor = new AuthGateSessionMonitor(clock,
        (token, subject, signal) => verifyAuthGateSession(config, token, subject, window.fetch.bind(window), signal),
        token => { this.#core.invalidateIfCurrent(token); });
    }
    const reconcile = () => { this.#core.syncLogout(); this.#core.getAccessToken(); this.#monitor?.wake(); };
    const storage = (event: StorageEvent) => { if (event.key === null || event.key === epoch?.key) reconcile(); };
    if (epoch && typeof window !== 'undefined') {
      window.addEventListener('storage', storage);
      window.addEventListener('pageshow', reconcile);
      window.addEventListener('focus', reconcile);
      window.document.addEventListener('visibilitychange', reconcile);
    }
    destroyRef.onDestroy(() => {
      if (epoch && typeof window !== 'undefined') {
        window.removeEventListener('storage', storage);
        window.removeEventListener('pageshow', reconcile);
        window.removeEventListener('focus', reconcile);
        window.document.removeEventListener('visibilitychange', reconcile);
      }
      this.#monitor?.destroy();
      this.#core.destroy();
    });
  }
  start(returnUrl?: string, options?: AuthGateStartOptions): Promise<void> { return this.#core.start(returnUrl, options); }
  renew(returnUrl?: string, options?: AuthGateStartOptions): Promise<void> { return this.#core.renew(returnUrl, options); }
  complete(): Promise<string> { return this.#core.complete(); }
  getAccessToken(): string | null { return this.#core.getAccessToken(); }
  invalidateIfCurrent(token: string): boolean { return this.#core.invalidateIfCurrent(token); }
  clearLocal(): void { this.#core.clearLocal(); }
}
