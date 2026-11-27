import { inject } from '@angular/core';
import type { CanActivateFn, CanMatchFn } from '@angular/router';
import { AuthGateAccess } from './authgate-access';
import { AuthGateAccessRequirements } from './authgate-access-core';
import { AuthGateSession } from './authgate-session';

/** No navigation or implicit bootstrap: onboarding/callback routes can use identity-only checks. */
export const authGateAuthenticatedGuard: CanActivateFn = () => inject(AuthGateSession).getAccessToken() !== null;
/** Fail closed while loading or switching workspaces. These are UI gates, not server authorization. */
export function requireAuthGateAccess(requirements: AuthGateAccessRequirements = {}): CanActivateFn {
  return () => inject(AuthGateAccess).allows(requirements);
}
export function matchAuthGateAccess(requirements: AuthGateAccessRequirements = {}): CanMatchFn {
  return () => inject(AuthGateAccess).allows(requirements);
}
