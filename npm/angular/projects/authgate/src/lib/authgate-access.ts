import { DestroyRef, EnvironmentProviders, Signal, computed, effect, inject, makeEnvironmentProviders, signal, untracked } from '@angular/core';
import { AUTHGATE_CONFIG } from './authgate-provider';
import { AuthGateSession } from './authgate-session';
import { AuthGateConfigurationError, AuthGateErrorCode, AuthGateSessionState } from './authgate-contracts';
import { AuthGateAccessCore, AuthGateAccessLoader, AuthGateAccessPolicy, AuthGateAccessRequirements, AuthGateAccessState, allowsAuthGateAccess, validateAuthGateAccessPolicy } from './authgate-access-core';
import { AuthGateAccessTransport, AuthGateAccessTransportOptions } from './authgate-access-transport';

export interface AuthGateAccessOptions extends AuthGateAccessPolicy, AuthGateAccessTransportOptions {}
const empty = Object.freeze([]) as readonly never[];
const hidden = (status: 'anonymous'|'idle'): AuthGateAccessState => Object.freeze({status,workspaces:empty,workspaceId:null,permissions:empty,roles:empty});

export class AuthGateAccess {
  readonly state: Signal<AuthGateAccessState>;
  readonly #core: AuthGateAccessCore;
  constructor(session: AuthGateSession, policy: AuthGateAccessPolicy, loader: AuthGateAccessLoader, destroyRef: DestroyRef) {
    const snapshot=signal<{owner:AuthGateSessionState; value:AuthGateAccessState}>({owner:session.state(),value:hidden('anonymous')});
    this.#core=new AuthGateAccessCore(policy,()=>{session.getAccessToken(); return session.state();},loader,
      value=>snapshot.set({owner:session.state(),value}));
    this.state=computed(()=>{
      const owner=session.state(); const current=snapshot();
      if(owner.status!=='authenticated') return hidden('anonymous');
      return current.owner===owner ? current.value : hidden('idle');
    });
    effect(()=>{session.state(); untracked(()=>this.#core.sync());});
    destroyRef.onDestroy(()=>this.#core.destroy());
  }
  discover(): Promise<AuthGateAccessState> {return this.#core.discover();}
  selectWorkspace(id:string): Promise<AuthGateAccessState> {return this.#core.selectWorkspace(id);}
  clear(): void {this.#core.clear();}
  allows(requirements: AuthGateAccessRequirements = {}): boolean {return allowsAuthGateAccess(this.#core.state,requirements);}
}

/** Optional product adapter. The product owns its API URLs and authorization version. */
export function provideAuthGateAccess(options: AuthGateAccessOptions): EnvironmentProviders {
  const policy=validateAuthGateAccessPolicy(options);
  const transportOptions=Object.freeze({bootstrapUrl:options.bootstrapUrl,contextUrl:options.contextUrl});
  if ((policy.workspaceMode==='required') !== (transportOptions.contextUrl!==undefined)) throw new AuthGateConfigurationError(AuthGateErrorCode.ConfigurationInvalid);
  return makeEnvironmentProviders([{provide:AuthGateAccess,useFactory:()=>{
    const session=inject(AuthGateSession); const config=inject(AUTHGATE_CONFIG);
    if(typeof window==='undefined' || window.location.origin!==config.appOrigin) throw new AuthGateConfigurationError(AuthGateErrorCode.ConfigurationInvalid);
    const transport=new AuthGateAccessTransport(config,transportOptions,session,window.fetch.bind(window));
    return new AuthGateAccess(session,policy,request=>transport.load(request),inject(DestroyRef));
  }}]);
}
