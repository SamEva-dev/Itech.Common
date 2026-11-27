import { AuthGateConfigurationError, AuthGateErrorCode, AuthGateSessionState } from './authgate-contracts';

export type AuthGateWorkspaceMode = 'required' | 'none';
export interface AuthGateAccessPolicy {
  readonly workspaceMode: AuthGateWorkspaceMode;
  readonly authorizationVersion: 1 | 2;
}
export interface AuthGateWorkspace { readonly id: string; readonly label: string; }
export interface AuthGateAccessState {
  readonly status: 'anonymous' | 'idle' | 'loading' | 'select_workspace' | 'no_access' | 'not_ready' | 'ready' | 'unavailable';
  readonly workspaces: readonly AuthGateWorkspace[];
  readonly workspaceId: string | null;
  readonly permissions: readonly string[];
  readonly roles: readonly string[];
  readonly reason?: 'context_verified' | 'profile_verified' | 'profile_pending' | 'onboarding_required';
}
export interface AuthGateAccessRequirements {
  readonly allPermissions?: readonly string[];
  readonly anyRoles?: readonly string[];
}
export interface AuthGateAccessRequest {
  readonly subject: string;
  readonly workspaceId: string | null;
  readonly signal: AbortSignal;
}
export type AuthGateAccessLoader = (request: AuthGateAccessRequest) => Promise<unknown>;
const empty = Object.freeze([]) as readonly never[];
function state(status: AuthGateAccessState['status'], workspaces: readonly AuthGateWorkspace[] = empty): AuthGateAccessState {
  return Object.freeze({status, workspaces, workspaceId: null, permissions: empty, roles: empty});
}
function record(value: unknown): Record<string, unknown> {
  if (!value || typeof value !== 'object' || Array.isArray(value)) throw new Error('invalid access response');
  return value as Record<string, unknown>;
}
function code(value: unknown): value is string {
  return typeof value === 'string' && value.length > 0 && value.length <= 256 && !/[\s\u0000-\u001f\u007f]/u.test(value);
}
function codes(value: unknown): readonly string[] {
  if (!Array.isArray(value) || value.length > 4096 || !value.every(code)) throw new Error('invalid access response');
  return Object.freeze([...new Set<string>(value)]);
}
function workspaces(value: unknown): readonly AuthGateWorkspace[] {
  if (!Array.isArray(value) || value.length > 2048) throw new Error('invalid workspaces');
  const seen = new Set<string>(); const active: AuthGateWorkspace[] = [];
  for (const raw of value) {
    const w = record(raw);
    if (!code(w['id']) || seen.has(w['id']) || typeof w['active'] !== 'boolean' ||
      typeof w['label'] !== 'string' || w['label'].length > 512 || /[\u0000-\u001f\u007f]/u.test(w['label'])) throw new Error('invalid workspace');
    if (w['organizationId'] !== undefined && w['organizationId'] !== w['id']) throw new Error('contradictory workspace');
    seen.add(w['id']);
    if (w['active']) active.push(Object.freeze({id: w['id'], label: w['label']}));
  }
  return Object.freeze(active);
}
export function validateAuthGateAccessPolicy(policy: AuthGateAccessPolicy): AuthGateAccessPolicy {
  if (!policy || !['required','none'].includes(policy.workspaceMode) || ![1,2].includes(policy.authorizationVersion)) throw new AuthGateConfigurationError(AuthGateErrorCode.ConfigurationInvalid);
  return Object.freeze({workspaceMode: policy.workspaceMode, authorizationVersion: policy.authorizationVersion});
}

/** Only effective grants from the business API. Discovery role defaults are never consumed. */
export function normalizeAuthGateAccessResponse(value: unknown, subject: string, requested: string | null,
  policy: AuthGateAccessPolicy, discovered: readonly AuthGateWorkspace[]): AuthGateAccessState {
  const r = record(value);
  if (r['subject'] !== subject || r['version'] !== policy.authorizationVersion ||
      typeof r['permissionsResolved'] !== 'boolean' || typeof r['businessSessionEstablished'] !== 'boolean') throw new Error('invalid access response');
  const selected = r['selectedWorkspaceId'];
  if (selected !== requested || (requested !== null ? r['organizationId'] !== requested : (r['organizationId'] !== undefined && r['organizationId'] !== null))) throw new Error('context mismatch');
  const list = requested === null ? workspaces(r['workspaces']) :
    r['workspaces'] === undefined ? discovered : workspaces(r['workspaces']);
  if (policy.workspaceMode === 'none' && (requested !== null || list.length)) throw new Error('unexpected workspace');
  if (requested !== null && !list.some(w => w.id === requested)) throw new Error('workspace unavailable');
  const ready = r['state'] === 'ready';
  if (ready !== (r['businessSessionEstablished'] === true) || (ready && r['permissionsResolved'] !== true)) throw new Error('invalid readiness');
  if (policy.workspaceMode === 'required' && requested === null) {
    if (ready || r['permissionsResolved'] || (r['state'] !== 'select_workspace' && r['state'] !== 'no_access') ||
        (r['state'] === 'select_workspace') !== (list.length > 0)) throw new Error('invalid discovery');
    return state(list.length ? 'select_workspace' : 'no_access', list);
  }
  if (!ready) {
    if (!['context_verified','profile_verified','profile_pending','onboarding_required','no_access'].includes(String(r['state']))) throw new Error('unknown state');
    const reason = r['state'];
    if (reason === 'no_access') return state('no_access', list);
    if (reason !== 'context_verified' && reason !== 'profile_verified' && reason !== 'profile_pending' && reason !== 'onboarding_required') throw new Error('unknown state');
    return Object.freeze({...state('not_ready', list), reason});
  }
  return Object.freeze({status:'ready',workspaces:list,workspaceId:requested,
    permissions:codes(r['permissions']),roles:codes(r['roles'])});
}

export function allowsAuthGateAccess(s: AuthGateAccessState, requirements: AuthGateAccessRequirements = {}): boolean {
  if (s.status !== 'ready' || !requirements || typeof requirements !== 'object' || Array.isArray(requirements) || Object.keys(requirements).some(k=>k !== 'allPermissions' && k !== 'anyRoles')) return false;
  const {allPermissions, anyRoles} = requirements;
  if (allPermissions !== undefined && (!Array.isArray(allPermissions) || !allPermissions.length || !allPermissions.every(code) || !allPermissions.every(p=>s.permissions.includes(p)))) return false;
  if (anyRoles !== undefined && (!Array.isArray(anyRoles) || !anyRoles.length || !anyRoles.every(code) || !anyRoles.some(r=>s.roles.includes(r)))) return false;
  return true;
}

/** A session object is an opaque generation marker, not a subject-only cache key. */
export class AuthGateAccessCore {
  #session?: AuthGateSessionState;
  #state = state('anonymous');
  #abort?: AbortController;
  #destroyed = false;
  readonly #policy: AuthGateAccessPolicy;
  constructor(policy: AuthGateAccessPolicy, private readonly currentSession: () => AuthGateSessionState,
    private readonly load: AuthGateAccessLoader, private readonly publish: (s: AuthGateAccessState)=>void = ()=>{}) {
    this.#policy = validateAuthGateAccessPolicy(policy);
  }
  get state(): AuthGateAccessState { this.sync(); return this.#state; }
  sync(): void {
    if (this.#destroyed) return;
    const current = this.currentSession();
    if (current === this.#session) return;
    this.#session = current;
    this.#abort?.abort(); this.#abort = undefined;
    this.#set(state(current.status === 'authenticated' ? 'idle' : 'anonymous'));
  }
  discover(): Promise<AuthGateAccessState> { return this.#request(null); }
  selectWorkspace(id: string): Promise<AuthGateAccessState> {
    this.sync();
    if (this.#policy.workspaceMode !== 'required' || !code(id) || !this.#state.workspaces.some(w=>w.id===id)) {
      this.clear(); return Promise.resolve(this.#state);
    }
    return this.#request(id);
  }
  clear(): void { this.#abort?.abort(); this.#abort=undefined; this.#session=undefined; this.sync(); }
  destroy(): void { this.#destroyed=true; this.#abort?.abort(); this.#abort=undefined; this.#session=undefined; this.#set(state('anonymous')); }
  #set(s: AuthGateAccessState): void { this.#state=s; this.publish(s); }
  async #request(id: string | null): Promise<AuthGateAccessState> {
    this.sync(); const session = this.#session;
    if (this.#destroyed || session?.status !== 'authenticated') return this.#state;
    const list = id === null ? empty : this.#state.workspaces;
    this.#abort?.abort(); const abort = new AbortController(); this.#abort=abort;
    this.#set(state('loading',list));
    try {
      const response = await this.load({subject:session.identity.subject,workspaceId:id,signal:abort.signal});
      this.sync();
      if (this.#abort!==abort || abort.signal.aborted || this.#session!==session) return state('idle');
      this.#set(normalizeAuthGateAccessResponse(response,session.identity.subject,id,this.#policy,list));
    } catch {
      this.sync();
      if (this.#abort!==abort || abort.signal.aborted || this.#session!==session) return state('idle');
      this.#set(state('unavailable'));
    } finally { if (this.#abort===abort) this.#abort=undefined; }
    return this.#state;
  }
}
