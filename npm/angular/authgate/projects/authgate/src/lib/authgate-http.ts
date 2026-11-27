import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { HttpContextToken, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Injectable, Injector, PLATFORM_ID, inject, signal } from '@angular/core';
import { catchError, defer, throwError } from 'rxjs';
import { AUTHGATE_CONFIG } from './authgate-provider';
import { AuthGateSession } from './authgate-session';
import { AuthGateProtocolError } from './authgate-oidc';
import { AuthGateErrorCode } from './authgate-contracts';
import { AuthGateHttpFailure, handleAuthGateHttpFailure, isAuthGateBearerToken, resolveAuthGateApiUrl } from './authgate-http-core';

/** Opt out for public endpoints or requests with independently managed credentials. */
export const AUTHGATE_SKIP_AUTH = new HttpContextToken<boolean>(() => false);

@Injectable({ providedIn: 'root' })
export class AuthGateHttpErrors {
  readonly #last = signal<AuthGateHttpFailure | null>(null);
  readonly last = this.#last.asReadonly();
  report(failure: AuthGateHttpFailure): void { this.#last.set(Object.freeze({ ...failure })); }
  clear(): void { this.#last.set(null); }
}

/** Use once, after URL-rewriting interceptors; requires FetchBackend to enforce redirect:'error'. */
export const authGateHttpInterceptor: HttpInterceptorFn = (request, next) => {
  if (request.context.get(AUTHGATE_SKIP_AUTH) || request.headers.has('Authorization')) return next(request);
  const config = inject(AUTHGATE_CONFIG);
  const document = inject(DOCUMENT);
  const injector = inject(Injector);
  // No bearer attachment during SSR or when the actual browser origin is inconsistent.
  if (!isPlatformBrowser(inject(PLATFORM_ID)) || !document.defaultView || document.defaultView.location.origin !== config.appOrigin) return next(request);
  return defer(() => {
    const target = resolveAuthGateApiUrl(request.url, document.baseURI, config);
    if (target === null) return next(request);
    const session = injector.get(AuthGateSession);
    const errors = injector.get(AuthGateHttpErrors);
    const token = session.getAccessToken();
    if (token !== null && !isAuthGateBearerToken(token)) {
      let invalidated = false;
      try { invalidated = session.invalidateIfCurrent(token); }
      catch { invalidated = session.getAccessToken() === null; }
      errors.report({ code: AuthGateErrorCode.HttpCredentialInvalid, status: 0, sessionInvalidated: invalidated });
      return throwError(() => new AuthGateProtocolError(AuthGateErrorCode.HttpCredentialInvalid));
    }
    const outgoing = token === null ? request.clone({ url: target }) : request.clone({
      url: target, setHeaders: { Authorization: `Bearer ${token}` },
      redirect: 'error', credentials: 'omit', withCredentials: false, cache: 'no-store', transferCache: false,
    });
    return next(outgoing).pipe(catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        const failure = handleAuthGateHttpFailure(error.status, token, session);
        if (failure) errors.report(failure);
      }
      // Preserve application-specific response bodies, headers and status for existing consumers.
      // No logging, retry, navigation, token refresh or automatic reauthentication here.
      return throwError(() => error);
    }));
  });
};
