# SSO-08f — Logout, current token revocation, cross-tab coordination

Apply on top of SSO-08a–08e in Itech.Common. Version `@itech/authgate` 0.1.0-preview.8f. No business application changes, backend changes or deletions. The Angular integration is browser-only; package not published.

## API and semantics

```ts
const logout = inject(AuthGateLogout);
// Local only (one tab):
await logout.logout();
// Clear current tab, signal other tabs of the SAME configured application/origin,
// and request revocation of the current access token:
const result = await logout.logout({otherTabs: true, revokeCurrent: true});
// An independent, explicit operation (after awaiting the previous call if desired):
logout.closeCentralSession();
```

`logout()` clears local credentials and cancels in-progress local OIDC operations synchronously before any network call. Angular AuthGateAccess hides permissions when the session is no longer authenticated; application-specific stores, pending requests, SignalR, and HTTP error presentation are integration responsibilities. No automatic navigation, reauthentication or retries. Repeated logout without a current token makes no revocation call. Late revocation responses do not alter a new local session. An explicit subsequent logout still clears a new session.

`result.remote` is `revoked` only for HTTP 204 without redirection from POST `/api/oidc/session/revoke-current` (no payload). `not-requested`, `no-token`, and `unconfirmed` are distinct. 401/403/503, other statuses, network errors, timeout and malformed credentials are NOT treated as confirmed revocation. This endpoint requires `Oidc:Enabled=true` and `Oidc:EnforceTokenEntryValidation=true` on AuthGate and the API's online token entry/session validation for immediate rejection; verify CORS and registered client compatibility, especially Manager. Request uses HTTPS configured authority, 5-second budget, no cookies, no redirect, no retry. Does not revoke other token entries, sessions, refresh tokens or authorizations. A failed or unconfirmed revocation does not restore the token; the browser can no longer retry it from memory.

`otherTabs` is `signalled`, `unavailable`, or `not-requested`. `signalled` means marker written, not proof all tabs processed it. No token, identity, permission or organization in the marker or storage event. Marker is a random 256-bit hex string under `<storageNamespace>:logout-epoch`; generated on first read and replaced on logout or if missing. StorageNamespace contains issuer, client, audience, app origin, API URL and callback. Apps with different origin/configuration, separate browser profiles, devices, or storage partitions do not share the marker. Same namespace means all local tabs, not only a selected identity. No cross-origin browser federation, no token sharing, no all-device logout. No `BroadcastChannel` dependency. Storage events plus focus/pageshow/visibilitychange and credential access re-read the actual marker. Stale event payloads are ignored. Pending OIDC transactions bind to the marker before redirect, check it before token exchange and again before accepting the result. Transactions started on version 8e before upgrade will be rejected once 8f is active. If storage is inaccessible/corrupt, token retrieval fails closed; explicit logout still clears memory, with `pendingTransaction` and `otherTabs` reporting storage failures. Browser storage can be manipulated by same-origin JS; this does not defend against XSS or restore atomic server-side session semantics. Concurrent browser processes can briefly race; online API authorization remains mandatory.

`closeCentralSession()` clears current local state and signals same-context tabs before top-level navigation to `/connect/logout?client_id=...&post_logout_redirect_uri=...` (optional redirect from validated configuration). No token in URL. AuthGate displays a CSRF-protected confirmation form; actual logout requires user confirmation. Configure the exact URI in `Oidc:Clients:*:PostLogoutRedirectUris` and test OpenIddict validation of the client_id without id_token_hint. This is intentionally separate from token revocation and does NOT prove logout succeeded. Other applications can retain valid access tokens until their own invalidation/expiry. A failed navigation or refusal does not restore local tokens. No automatic global or per-client multi-device revocation.

## Validation

From `Itech.Common/angular/authgate`:

```powershell
npm install
npm test
npm run build
npm run pack:local
```

Noyau : 267 tests (237 précédents + 30 nouveaux) réussis ici avec Node 22.16.0 et tsc global 5.8.3. Les tests emploient des doubles HTTP/stockage/horloge; pas de test Angular navigateur ni backend réel. Les dépendances npm Angular sont absentes de cet environnement. Vérifier builds Angular 20 et 22, injection, navigation, CORS et tests E2E suivants avant activation :

- A/B même application, deux tokens distincts : logout local A conserve B, logout avec otherTabs efface B (pas de révocation serveur du token B), autre application/origine non affectée.
- Logout pendant callback, pendant retour de l'IdP, onglet suspendu/repris, réponse tardive, suppression du marqueur, stockage bloqué, nouveau login explicite.
- Révocation 204 puis API refuse l'ancien token ; 401/403/503 ou timeout affichent un résultat non confirmé ; aucune relance.
- End-session : GET confirmation ne ferme pas le cookie, POST confirmé le ferme, URI enregistrée, annulation de confirmation, absence de redirection automatique.
- Vérifier invalidation des stores métier, navigation et connexions SignalR lors de l'intégration. Traduire les messages de résultat dans chaque app (FR/EN).

Rollback : revenir au package 8e avant l'intégration, en abandonnant les transactions PKCE en cours. Ne pas supprimer arbitrairement le marqueur pendant que des onglets 8f sont actifs. Pas de SQL ou de secrets.
