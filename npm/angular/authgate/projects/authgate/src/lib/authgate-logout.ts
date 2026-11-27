import { Injectable, inject } from '@angular/core';
import { AUTHGATE_CONFIG } from './authgate-provider';
import { AuthGateSession } from './authgate-session';
import { AuthGateLogoutCore, AuthGateLogoutEpoch, AuthGateLogoutOptions, AuthGateLogoutResult, authGateEndSessionUrl } from './authgate-logout-core';
import { AuthGateProtocolError } from './authgate-oidc';
import { AuthGateErrorCode } from './authgate-contracts';

@Injectable({providedIn:'root'})
export class AuthGateLogout {
  readonly #config = inject(AUTHGATE_CONFIG);
  readonly #session = inject(AuthGateSession);
  readonly #epoch = inject(AuthGateLogoutEpoch);
  readonly #core = new AuthGateLogoutCore(this.#config,this.#session,this.#epoch,window.fetch.bind(window));
  /** Local logout immediately; optional remote current-token revocation has an explicit outcome. */
  logout(options: AuthGateLogoutOptions = {}): Promise<AuthGateLogoutResult> { return this.#core.logout(options); }
  /** Separate explicit action: the server asks for confirmation. No tokens in the URL. */
  closeCentralSession(): void {
    try {
      if (window.location.origin !== this.#config.appOrigin) throw new Error();
      // Do not navigate if the local transaction or marker cannot be cleared/written.
      this.#session.clearLocal();
      this.#epoch.advance();
      window.location.assign(authGateEndSessionUrl(this.#config));
    } catch { throw new AuthGateProtocolError(AuthGateErrorCode.OidcUnavailable); }
  }
}
