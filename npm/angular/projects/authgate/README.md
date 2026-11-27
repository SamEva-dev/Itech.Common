# @itech-core/authgate

Shared Angular authentication and access-control library for AuthGate. It is independent of application-specific business logic.

## Installation

```sh
npm install @itech-core/authgate@preview --save-exact
```

For reproducible deployments, pin an approved, published version and commit the consumer lockfile. The `preview` tag identifies prereleases; stable releases use `latest`.

## Configuration

Register the provider in the Angular application's root providers. Replace the illustrative URLs, client ID, audience and scope with values registered in your AuthGate instance.

```ts
import { provideAuthGate } from '@itech-core/authgate';

export const authGateProvider = provideAuthGate({
  authority: 'https://identity.example.com/',
  clientId: 'example-spa',
  apiAudience: 'example-api',
  scope: 'openid profile',
  appOrigin: 'https://app.example.com',
  redirectUri: 'https://app.example.com/auth/callback',
  apiBaseUrl: 'https://api.example.com/',
  postLogoutRedirectUri: 'https://app.example.com/'
});
```

The application owns the callback route, bootstrap, business permissions and error presentation. Use the exported `AuthGateOidcClient`, `AuthGateSession`, `AuthGateAccess`, `authGateAuthenticatedGuard` and `requireAuthGateAccess` according to the host application's integration.

## Features

- OIDC public-client authorization code flow with PKCE S256.
- Configuration validation and local return URL validation.
- Session and access management, HTTP integration, route guards and logout coordination.
- Application and platform session validation modes.

## Compatibility and security

Declared Angular peer dependency range: >=20.3.0 and <23.0.0; RxJS ^7.8.0. The library build uses Angular 20.3. Full integration compatibility must be verified in each consuming application.

Use HTTPS and registered redirect URIs. Never put client secrets or private keys in browser configuration. UI guards do not replace server-side authorization, token validation or tenant isolation.

## Development and releases

From `npm/angular`, run `npm ci`, `npm test` and `npm run build`. GitHub Actions uses `PACKAGE_VERSION` in `.github/workflows/publish-npm.yml` for releases. Published versions are immutable. The workflow adds the README to the compiled package before packing.

## Repository and license

Repository: https://github.com/SamEva-dev/Itech.Common

Package metadata currently declares `UNLICENSED`; public availability does not grant a license to reuse the code.
