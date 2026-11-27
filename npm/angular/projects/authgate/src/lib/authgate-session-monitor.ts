import { ValidatedAuthGateConfig } from './authgate-config';
import { isAuthGateBearerToken } from './authgate-http-core';

export type AuthGateSessionValidation = 'active' | 'denied' | 'unavailable';
export interface SessionMonitorClock {
  schedule(callback: () => void, delayMs: number): () => void;
  now(): number;
}
interface Binding { readonly token: string; readonly subject: string; }
const interval = 30000;
const timeout = 5000;

/** Fixed trusted authority and route. No cookies, redirects, retries or credential persistence. */
export async function verifyAuthGateSession(config: ValidatedAuthGateConfig, token: string, subject: string,
  request: typeof fetch, signal: AbortSignal): Promise<AuthGateSessionValidation> {
  if (!isAuthGateBearerToken(token) || !subject || signal.aborted) return 'unavailable';
  const platform = config.sessionValidation === 'platform';
  const url = new URL(platform ? 'api/oidc/platform/session/validate' : 'api/oidc/session/active', config.authority).href;
  let response: Response;
  try {
    response = await request(url, {method: 'GET', headers: {Authorization: `Bearer ${token}`, Accept: 'application/json'},
      credentials: 'omit', redirect: 'error', cache: 'no-store', referrerPolicy: 'no-referrer', signal});
  } catch { return 'unavailable'; }
  if (signal.aborted || response.redirected || (response.url && response.url !== url)) {
    void response.body?.cancel().catch(() => undefined); return 'unavailable';
  }
  if (response.status === 401 || response.status === 403) {
    void response.body?.cancel().catch(() => undefined); return 'denied';
  }
  if (response.status !== 200 || !/^application\/json(?:\s*;|$)/i.test(response.headers.get('content-type') ?? '') || !response.body) {
    void response.body?.cancel().catch(() => undefined); return 'unavailable';
  }
  const reader = response.body.getReader();
  try {
    const decoder = new TextDecoder('utf-8', {fatal: true});
    let body = '', bytes = 0;
    for (;;) {
      const part = await reader.read();
      if (signal.aborted) return 'unavailable';
      if (part.done) break;
      bytes += part.value.byteLength;
      if (bytes > 1024) return 'unavailable';
      body += decoder.decode(part.value, {stream: true});
    }
    body += decoder.decode();
    const value: unknown = JSON.parse(body);
    if (!value || typeof value !== 'object' || Array.isArray(value)) return 'unavailable';
    const data = value as Record<string, unknown>;
    if (data['active'] !== true || (platform && data['subject'] !== subject)) return 'denied';
    return 'active';
  } catch { return 'unavailable'; }
  finally { void reader.cancel().catch(() => undefined); }
}

/** One probe in flight per binding. Both confirmed denial and inability to verify clear local access.
 * Old responses cannot affect a replacement binding, even if a transport ignores cancellation.
 */
export class AuthGateSessionMonitor {
  private binding?: Binding;
  private cancelTimer?: () => void;
  private flight?: AbortController;
  private cancelDeadline?: () => void;
  private nextDue = 0;
  private destroyed = false;
  constructor(private readonly clock: SessionMonitorClock,
    private readonly verify: (token: string, subject: string, signal: AbortSignal) => Promise<AuthGateSessionValidation>,
    private readonly invalidate: (token: string, result: Exclude<AuthGateSessionValidation, 'active'>) => void) {}
  bind(token: string | null, subject?: string): void {
    if (this.destroyed) return;
    if (token && subject && this.binding?.token === token && this.binding.subject === subject) return;
    this.stop();
    if (!token || !subject) return;
    const binding = {token, subject};
    this.binding = binding;
    this.schedule(binding, 0);
  }
  /** Resume a due probe on visibility/focus; never start a second concurrent probe. */
  wake(): void {
    if (this.binding && !this.flight && this.clock.now() >= this.nextDue) {
      this.cancelTimer?.(); this.cancelTimer = undefined; void this.probe(this.binding);
    }
  }
  destroy(): void { this.destroyed = true; this.stop(); }
  private stop(): void {
    this.binding = undefined;
    this.cancelTimer?.(); this.cancelTimer = undefined;
    this.cancelDeadline?.(); this.cancelDeadline = undefined;
    this.flight?.abort(); this.flight = undefined;
  }
  private schedule(binding: Binding, delay: number): void {
    this.nextDue = this.clock.now() + delay;
    this.cancelTimer = this.clock.schedule(() => {this.cancelTimer = undefined; void this.probe(binding);}, delay);
  }
  private async probe(binding: Binding): Promise<void> {
    if (this.destroyed || this.binding !== binding || this.flight) return;
    const abort = new AbortController(); this.flight = abort;
    let cancelDeadline: (() => void) | undefined;
    let result: AuthGateSessionValidation = 'unavailable';
    try {
      const deadline = new Promise<AuthGateSessionValidation>(resolve => {
        cancelDeadline = this.clock.schedule(() => {abort.abort(); resolve('unavailable');}, timeout);
        this.cancelDeadline = cancelDeadline;
      });
      // Resolve on cancellation too, even if the request implementation never settles.
      const cancelled = new Promise<AuthGateSessionValidation>(resolve => abort.signal.addEventListener('abort', () => resolve('unavailable'), {once: true}));
      result = await Promise.race([this.verify(binding.token, binding.subject, abort.signal), deadline, cancelled]);
    } catch { /* fail closed */ }
    finally {
      cancelDeadline?.();
      if (this.flight === abort) { this.flight = undefined; this.cancelDeadline = undefined; }
    }
    if (this.destroyed || this.binding !== binding) return;
    if (result === 'active' && !abort.signal.aborted) this.schedule(binding, interval);
    else {
      this.stop();
      try { this.invalidate(binding.token, result === 'denied' ? 'denied' : 'unavailable'); } catch { /* no unhandled timer rejection */ }
    }
  }
}
