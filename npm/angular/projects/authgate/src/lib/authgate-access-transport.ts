import { AuthGateConfigurationError, AuthGateErrorCode } from './authgate-contracts';
import { ValidatedAuthGateConfig } from './authgate-config';
import { AuthGateAccessRequest } from './authgate-access-core';
import { isAuthGateBearerToken, resolveAuthGateApiUrl } from './authgate-http-core';

export interface AuthGateAccessTransportOptions {
  readonly bootstrapUrl: string;
  readonly contextUrl?: string;
}
interface SessionCredential {
  getAccessToken(): string | null;
  invalidateIfCurrent(token: string): boolean;
}
/** Dedicated no-redirect transport, independent of application HttpClient interceptors. */
export class AuthGateAccessTransport {
  readonly #bootstrap: string;
  readonly #context?: string;
  constructor(config: ValidatedAuthGateConfig, options: AuthGateAccessTransportOptions,
    private readonly session: SessionCredential, private readonly fetcher: typeof fetch) {
    const validate = (raw: string): string => {
      const target = resolveAuthGateApiUrl(raw, config.appOrigin + '/', config);
      if (!target || new URL(target).search || raw.includes('?')) throw new AuthGateConfigurationError(AuthGateErrorCode.ConfigurationInvalid);
      return target;
    };
    this.#bootstrap=validate(options.bootstrapUrl);
    this.#context=options.contextUrl === undefined ? undefined : validate(options.contextUrl);
  }
  async load(request: AuthGateAccessRequest): Promise<unknown> {
    const token=this.session.getAccessToken();
    const target=request.workspaceId===null ? this.#bootstrap : this.#context;
    if (!token || !isAuthGateBearerToken(token) || !target || request.signal.aborted) throw new Error(AuthGateErrorCode.AccessUnavailable);
    const abort=new AbortController();
    const cancel=()=>abort.abort();
    request.signal.addEventListener('abort',cancel,{once:true});
    const timer=setTimeout(cancel,10000);
    try {
      const response=await this.fetcher(target,{
        method:request.workspaceId===null?'GET':'POST',
        headers:{Authorization:`Bearer ${token}`,Accept:'application/json',...(request.workspaceId===null?{}:{'Content-Type':'application/json','Idempotency-Key':crypto.randomUUID()})},
        ...(request.workspaceId===null?{}:{body:JSON.stringify({workspaceId:request.workspaceId})}),
        signal:abort.signal,redirect:'error',credentials:'omit',cache:'no-store',referrerPolicy:'no-referrer',
      });
      if (abort.signal.aborted || this.session.getAccessToken()!==token) {void response.body?.cancel().catch(()=>undefined); throw new Error(AuthGateErrorCode.AccessUnavailable);}
      if (response.status===401) { try {this.session.invalidateIfCurrent(token);} catch {/* memory already cleared */} }
      if (!response.ok || response.redirected || !response.body || !/^application\/json(?:\s*;|$)/i.test(response.headers.get('content-type')??'')) {
        void response.body?.cancel().catch(()=>undefined); throw new Error(AuthGateErrorCode.AccessUnavailable);
      }
      const reader=response.body.getReader(); const decoder=new TextDecoder('utf-8',{fatal:true});
      let text='',size=0;
      try {
        while(true) {
          const {done,value}=await reader.read();
          if(done) break;
          size+=value.byteLength;
          if(size>1024*1024) {void reader.cancel().catch(()=>undefined); throw new Error(AuthGateErrorCode.AccessUnavailable);}
          text+=decoder.decode(value,{stream:true});
        }
        if(abort.signal.aborted || this.session.getAccessToken()!==token) throw new Error(AuthGateErrorCode.AccessUnavailable);
        return JSON.parse(text+decoder.decode()) as unknown;
      } finally {void reader.cancel().catch(()=>undefined);reader.releaseLock();}
    } catch {throw new Error(AuthGateErrorCode.AccessUnavailable);}
    finally {clearTimeout(timer);request.signal.removeEventListener('abort',cancel);}
  }
}
