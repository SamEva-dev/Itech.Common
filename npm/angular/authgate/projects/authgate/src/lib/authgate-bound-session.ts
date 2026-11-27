import { computed, createEnvironmentInjector, EnvironmentInjector, signal } from '@angular/core';
import { AuthGateConfig, validateAuthGateConfig, ValidatedAuthGateConfig } from './authgate-config';
import { provideAuthGate } from './authgate-provider';
import { AuthGateSession } from './authgate-session';
import { AuthGateLogout } from './authgate-logout';

interface Scope { injector: EnvironmentInjector; config: ValidatedAuthGateConfig; session: AuthGateSession; logout: AuthGateLogout; }
/** An explicitly configured child session for SPAs with runtime-selected authorities.
 * Profile must be established by the consuming API before exposing any credential.
 */
export class AuthGateBoundSession<T extends {subject: string}> {
  private readonly scope = signal<Scope | null>(null);
  private readonly verified = signal<T | null>(null);
  private revision = 0;
  private pending?: Promise<{profile: T; returnUrl: string}>;
  readonly profile = computed(() => {
    const scope = this.scope(); const profile = this.verified(); const state = scope?.session.state();
    return profile && state?.status === 'authenticated' && state.identity.subject === profile.subject ? profile : null;
  });
  constructor(private readonly parent: EnvironmentInjector) {}
  configure(input: AuthGateConfig): void {
    let config: ValidatedAuthGateConfig;
    try { config = validateAuthGateConfig(input); } catch (error) { this.dispose(); throw error; }
    const previous = this.scope()?.config;
    if (previous?.storageNamespace === config.storageNamespace && previous.scope === config.scope &&
        previous.postLogoutRedirectUri === config.postLogoutRedirectUri && previous.allowInsecureLoopback === config.allowInsecureLoopback && previous.sessionValidation === config.sessionValidation) return;
    this.dispose();
    const injector = createEnvironmentInjector([provideAuthGate(config), AuthGateLogout], this.parent);
    try { this.scope.set({injector, config, session: injector.get(AuthGateSession), logout: injector.get(AuthGateLogout)}); }
    catch (error) { injector.destroy(); throw error; }
  }
  get config(): ValidatedAuthGateConfig | null { return this.scope()?.config ?? null; }
  getAccessToken(): string | null {
    const scope = this.scope(); const profile = this.profile();
    return scope && profile ? scope.session.getAccessToken() : null;
  }
  start(returnUrl: string): Promise<void> {
    const scope = this.requireScope();
    if (this.pending) return Promise.reject(new Error('AUTHGATE_SESSION_BUSY'));
    this.verified.set(null);
    return scope.session.start(returnUrl);
  }
  complete(load: (token: string, subject: string) => Promise<T>): Promise<{profile: T; returnUrl: string}> {
    if (this.pending) return this.pending;
    const scope = this.requireScope();
    if (this.getAccessToken()) return Promise.reject(new Error('AUTHGATE_SESSION_BUSY'));
    const revision = ++this.revision;
    this.verified.set(null);
    const operation = (async () => {
      try {
        const returnUrl = await scope.session.complete();
        const state = scope.session.state(); const token = scope.session.getAccessToken();
        if (!token || state.status !== 'authenticated' || revision !== this.revision) throw new Error('AUTHGATE_SESSION_CANCELLED');
        const profile = await load(token, state.identity.subject);
        if (revision !== this.revision || this.scope() !== scope || scope.session.getAccessToken() !== token || profile.subject !== state.identity.subject)
          throw new Error('AUTHGATE_SESSION_CANCELLED');
        this.verified.set(Object.freeze({...profile}));
        return {profile, returnUrl};
      } catch (error) {
        if (revision === this.revision && this.scope() === scope) this.clear();
        throw error;
      }
    })();
    this.pending = operation;
    void operation.finally(() => { if (this.pending === operation) this.pending = undefined; }).catch(() => undefined);
    return operation;
  }
  invalidateIfCurrent(token: string): boolean {
    if (this.getAccessToken() !== token) return false;
    this.clear(); return true;
  }
  clear(): void {
    ++this.revision; this.pending = undefined; this.verified.set(null);
    try { this.scope()?.session.clearLocal(); } catch { /* memory was cleared first */ }
  }
  async logout(revokeCurrent = true): Promise<void> {
    ++this.revision; this.pending = undefined; this.verified.set(null);
    const result = await this.scope()?.logout.logout({otherTabs: true, revokeCurrent});
    if (result && (result.remote === 'unconfirmed' || result.otherTabs === 'unavailable')) throw new Error('AUTHGATE_OIDC_UNAVAILABLE');
  }
  dispose(): void { const previous = this.scope(); this.clear(); this.scope.set(null); previous?.injector.destroy(); }
  private requireScope(): Scope { const scope = this.scope(); if (!scope) throw new Error('AUTHGATE_CONFIGURATION_INVALID'); return scope; }
}
