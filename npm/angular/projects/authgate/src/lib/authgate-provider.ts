import { DestroyRef, EnvironmentProviders, InjectionToken, makeEnvironmentProviders, inject } from '@angular/core';
import { AuthGateConfig, ValidatedAuthGateConfig, validateAuthGateConfig } from './authgate-config';

import { AuthGateOidcClient, AuthGateProtocolError } from './authgate-oidc';
import { AuthGateLogoutEpoch } from './authgate-logout-core';
import { AuthGateSession } from './authgate-session';
import { AuthGateErrorCode } from './authgate-contracts';

/** The root application owns the configuration; no environment is selected by the library. */
export const AUTHGATE_CONFIG = new InjectionToken<ValidatedAuthGateConfig>('@itech/authgate/config');

export function provideAuthGate(config: AuthGateConfig): EnvironmentProviders {
  // Validate immediately, before registration. No browser globals are accessed.
  return makeEnvironmentProviders([{ provide: AUTHGATE_CONFIG, useValue: validateAuthGateConfig(config) }, {
    provide: AuthGateLogoutEpoch, useFactory: () => {
      try {
        const validated = inject(AUTHGATE_CONFIG);
        if (typeof window === 'undefined' || !window.isSecureContext || window.location.origin !== validated.appOrigin) throw new Error();
        return new AuthGateLogoutEpoch(validated, window.localStorage, window.crypto);
      } catch { throw new AuthGateProtocolError(AuthGateErrorCode.OidcUnavailable); }
    },
  }, {
    provide: AuthGateOidcClient,
    useFactory: () => {
      const validated = inject(AUTHGATE_CONFIG);
      const epoch = inject(AuthGateLogoutEpoch);
      if (typeof window === 'undefined' || !window.isSecureContext || !window.crypto?.subtle)
        throw new AuthGateProtocolError(AuthGateErrorCode.OidcUnavailable);
      try {
        return new AuthGateOidcClient(validated, {
          storage: window.sessionStorage, crypto: window.crypto, fetch: window.fetch.bind(window),
          currentUrl: () => window.location.href,
          replaceUrl: path => window.history.replaceState(window.history.state, '', path),
          navigate: url => window.location.assign(url), now: () => Date.now(), logoutEpoch: () => epoch.read(),
        });
      } catch { throw new AuthGateProtocolError(AuthGateErrorCode.OidcUnavailable); }
    },
  }, { provide: AuthGateSession, useFactory: () => new AuthGateSession(inject(AuthGateOidcClient), inject(DestroyRef), inject(AuthGateLogoutEpoch), inject(AUTHGATE_CONFIG)) }]);
}
