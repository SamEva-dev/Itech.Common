# SSO-08b — Authorization Code Flow + PKCE S256

Prérequis : appliquer SSO-08a. Copier les fichiers du ZIP dans Itech.Common. Aucun fichier supprimé. Version proposée : `@itech/authgate` 0.1.0-preview.8b.

## Contrat et intégration future

Le code AuthGate disponible (`AuthGateOidcExtensions.cs`) expose `/connect/authorize`, `/connect/token`, un issuer HTTPS, des clients publics et PKCE obligatoire. Les clients Manager et VPS existants exigent `iss` dans le callback. La bibliothèque reprend ce contrat, ajoute `nonce` et exige un ID Token signé RS256. Les endpoints discovery et JWKS sont lus depuis l'autorité configurée, sans accepter d'autre origine ni de redirection HTTP. Vérifier leurs valeurs exactes et leur disponibilité CORS dans chaque environnement avant bascule.

```ts
import { inject } from '@angular/core';
import { AuthGateOidcClient } from '@itech/authgate';

// Dans un composant Angular exécuté dans le navigateur, après provideAuthGate(...).
const oidc = inject(AuthGateOidcClient);
await oidc.start('/dashboard');
// Dans le composant callback /auth/callback :
const result = await oidc.complete();
// À transmettre au futur gestionnaire de session, sans logs ni stockage local.
// Ne pas naviguer ni accorder les accès métier avant le bootstrap de l'API.
```

La transaction (state, nonce, verifier de 256 bits, namespace, scope, timestamp et destination locale) est placée dans sessionStorage, durée maximale 10 minutes. Elle est consommée avant tout appel réseau. Le callback est nettoyé via history.replaceState avant le premier await. Un callback échoué exige un nouveau login. Le client ne stocke pas de tokens, ne conserve pas l'ID Token, ne demande pas de refresh token et ne rejoue pas l'échange de code. Le budget HTTP total est de 15 secondes ; chaque réponse est limitée à 64 Kio. Les tokens sont transmis à l'appelant uniquement après validation cryptographique de l'ID Token ; ce n'est pas une validation cryptographique de l'Access Token. L'API doit toujours vérifier signature, audience, session, permissions et membership. La durée restante renvoyée est diminuée du temps de l'échange et de la vérification.

## Configuration et sécurité

- Vérifier les redirect URIs exactes, le client public, le scope `openid`, CORS sur le token/discovery/JWKS et `iss` dans le callback. AuthGate doit produire un ID Token signé RS256 avec `kid`, `nonce`, `sub`, `aud`, `iss`, `iat`, `exp`. Si `at_hash` existe, il est vérifié. Aucun fallback vers un ID Token non signé, un issuer différent ou un algorithme non pris en charge.
- Les clés JWKS sont lues à chaque callback, pas de cache ni de relance. Rotation de clé pendant l'échange : échec restrictif, nouveau login requis.
- Les erreurs publiques sont `AUTHGATE_OIDC_TRANSACTION_INVALID`, `AUTHGATE_OIDC_RESPONSE_INVALID`, `AUTHGATE_OIDC_ACCESS_DENIED`, `AUTHGATE_OIDC_UNAVAILABLE`. Traductions FR/EN à ajouter au moment du raccordement des frontends.
- Navigateur sécurisé avec WebCrypto et sessionStorage accessible. Pas de SSR pour la résolution du client OIDC. Le stockage de la transaction n'est pas une protection contre XSS, et peut être dupliqué par ouverture/clonage d'onglet ; le serveur doit garantir la consommation unique du code. Prévoir CSP et `Referrer-Policy: no-referrer` sur le document de callback, aucun script tiers ni analytics avant nettoyage de l'URL. Ne pas journaliser les paramètres de callback dans le reverse proxy.
- Le résultat de connexion est un contrat transitoire : aucune gestion persistante des tokens, du renouvellement, de la déconnexion ou du multi-onglet dans cet incrément. Aucun changement de session existante n'est effectué par le client ; l'intégration devra vider l'ancienne session au début d'un login explicite et ne publier la nouvelle qu'après bootstrap.
- Aucun changement des API ou frontends existants. Pas de package publié. Angular 20.3–22 : compatibilité à vérifier par builds réels. Pas de dépendance nouvelle.

## Vérification

Depuis `Itech.Common/angular/authgate` :

```sh
npm install
npm test
npm run build
npm run pack:local
```

Le noyau TypeScript a été compilé avec tsc 5.8.3 et les tests Node 22.16.0 exécutés : 89/89 (47 précédents + 42 OIDC). Les tests utilisent une vraie paire RSA 2048 et WebCrypto, mais simulent les réponses HTTP et le navigateur. La compilation Angular/ng-packagr avec les dépendances prévues (TS 5.9) et la recette sur serveur réel restent à faire.

Recette réelle avant SSO-08g : login neuf, session SSO existante, MFA, annulation login, callback expiré, callback dupliqué, mauvais issuer/client, changement de clé RSA, CORS token/discovery/JWKS, indisponibilité, erreur de bootstrap, navigation locale, aucun token dans storage/logs. Vérifier que les workflows d'inscription et de prelogin gardent leurs propres étapes métier ; pas de remplacement automatique.
