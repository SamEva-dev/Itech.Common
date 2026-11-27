# SSO-08d — Interceptor HTTP commun et erreurs

## Installation
Appliquer après SSO-08a, b et c dans le dépôt Itech.Common. Aucun frontend consommateur n'est modifié. Version proposée `@itech/authgate` 0.1.0-preview.8d. Les nouveaux fichiers sont `authgate-http-core.ts`, `authgate-http.ts` et `tests/http.test.cjs`. Les autres fichiers du ZIP remplacent leurs homologues. Aucun fichier supprimé.

```powershell
cd angular/authgate
npm install
npm test
npm run build
npm run pack:local
```

`@angular/common` est ajouté comme peer dependency et dev dependency. Générer et committer le lockfile après restauration. Les tests noyau exécutés ici via tsc global 5.8.3 et Node 22.16.0 : 182/182 (119 précédents + 63 nouveaux). Pas de compilation ng-packagr, pas de tests réels de HttpClient/DI, pas de publication npm. Les tests HTTP vérifient la logique pure, pas le transport navigateur.

## Intégration future (pas encore appliquée aux frontends)

```typescript
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideAuthGate, authGateHttpInterceptor } from '@itech/authgate';

// Dans les providers de l'application, fusionner avec les providers existants.
provideAuthGate(config),
provideHttpClient(withFetch(), withInterceptors([authGateHttpInterceptor]))
```

Placer l'interceptor partagé après les réécritures d'URL et avant le backend; ne pas chaîner d'interceptor modifiant la destination ou Authorization après lui. Désactiver les anciens interceptors d'authentification OIDC dans ce mode; ne pas supprimer les mécanismes métier (organisation, client, idempotence, traitement des erreurs, etc.). Les routes publiques qui ne doivent jamais porter un token ou provoquer un événement d'erreur commun doivent utiliser l'opt-out :

```typescript
import { HttpContext } from '@angular/common/http';
import { AUTHGATE_SKIP_AUTH } from '@itech/authgate';
http.get(url, {context: new HttpContext().set(AUTHGATE_SKIP_AUTH, true)});
```

L'opt-out et un Authorization explicitement fourni laissent la requête intégralement intacte; le code appelant reste responsable de cette credential et de sa destination. L'interceptor n'est pas un firewall pour les autres interceptors ou les appels fetch/SignalR. Il ne bloque pas une requête hors périmètre, il n'y ajoute aucun token partagé. Une requête dans le périmètre sans token reste anonyme; l'API doit protéger toutes ses routes privées.

## Isolation
- Origine exacte + préfixe de chemin sur une frontière de segment, sensible à la casse. Les URL relatives sont résolues contre `document.baseURI`, puis le clone fixe l'URL absolue. Pas d'URL protocol-relative, credentials URL, fragment, séparateurs/dot segments ambigus ni encodage malformé. Les paramètres de requête restent inchangés.
- Une API sur une origine distincte peut configurer un chemin racine (toutes ses routes sont alors éligibles). Sur l'origine SPA, configurer impérativement un chemin explicite (ex. `https://app.example.test/api`), sinon aucun token n'est joint. L'origine AuthGate est exclue même si configurée comme API; un gateway partagé AuthGate/API nécessitera un adaptateur dédié.
- Pas d'attachement SSR; vérifier l'origine du navigateur et celle du document de base. HTTPS selon la configuration validée 08a (exception loopback explicite).
- Bearer syntaxiquement contrôlé. Pour les appels authentifiés gérés : `redirect: 'error'`, `credentials: 'omit'`, `withCredentials: false`, `cache: 'no-store'`, `transferCache: false`. Pas de cookies pour Fetch sur ces appels, pas de cache de transfert SSR. Les routes nécessitant des cookies doivent être hors de ce transport et auditées séparément.
- **FetchBackend obligatoire pour la politique de redirection et de cookies.** Angular 20 : `withFetch()`; vérifier sur Angular 22 selon le backend réellement utilisé. XHR ne garantit pas ces propriétés. Les routes API ne doivent pas retourner des redirections, et doivent rester protégées côté serveur. Les uploads nécessitant le suivi de progression devront être évalués séparément (Fetch ne fournit pas le même suivi qu'XHR). Ne pas activer sans test navigateur.
- Référence Angular 20 : https://v20.angular.dev/guide/http/making-requests (options Fetch).

## Erreurs
`AuthGateHttpErrors.last()` est un Signal du dernier événement courant, sans URL, corps, header ou token. Le consommateur choisit l'affichage, appelle `clear()` lorsqu'il a traité l'événement (et lors des transitions UI), et conserve ses propres erreurs métier. L'interceptor propage l'instance HttpErrorResponse originale sans la journaliser. Une réponse ancienne ne publie rien si le token courant a changé ou disparu. Le 401 du token actuellement utilisé efface la session locale, pas la session distante. Une requête anonyme 401 n'efface rien. 403, 429, 502/503/504, réseau et autres erreurs ne changent pas le token. Aucun retry, aucune navigation automatique. Le renouvellement reste explicite (08c). `status:0` est aussi utilisé pour signaler localement un bearer malformé, avec son propre code et aucune émission réseau.

Clés de traduction proposées à intégrer dans chaque frontend (pas de changement de leurs fichiers dans ce sprint) :

| Code | FR | EN |
|---|---|---|
| AUTHGATE_HTTP_NETWORK | Connexion au service impossible. | Could not reach the service. |
| AUTHGATE_HTTP_UNAUTHORIZED | Authentification nécessaire. | Authentication required. |
| AUTHGATE_HTTP_FORBIDDEN | Accès non autorisé. | Access denied. |
| AUTHGATE_HTTP_RATE_LIMITED | Trop de requêtes. Réessayez plus tard. | Too many requests. Try again later. |
| AUTHGATE_HTTP_UNAVAILABLE | Service temporairement indisponible. | Service temporarily unavailable. |
| AUTHGATE_HTTP_FAILURE | La requête a échoué. | The request failed. |
| AUTHGATE_HTTP_CREDENTIAL_INVALID | Session locale invalide. | Invalid local session. |

## Recette obligatoire sur Angular 20.3 et 22
1. Connexion réelle + GET/POST API : Authorization correct, DTO, headers métier, paramètres, réponse et idempotence inchangés. Vérifier la configuration CORS.
2. Requête externe, asset SPA, AuthGate, autre port et route sœur : aucun bearer partagé. Vérifier les URL relatives avec base-href non racine.
3. Opt-out et Authorization explicitement fourni : requête inchangée, pas de mutation du Signal ni de la session.
4. 401 du token courant : une seule invalidation locale, aucune redirection, aucune relance. Plusieurs 401 concurrents et 401 d'un ancien token : pas de suppression de session nouvelle. Tester 401 sur route publique opt-out.
5. 403, 429, 503, panne réseau : code approprié et session inchangée, même HttpErrorResponse, aucune relance. Vérifier les messages métier existants.
6. Redirect 302/307 vers autre origine et vers chemin hors préfixe : avec Fetch le second appel doit être absent. Vérifier absence de cookies et de transfert cache dans les requêtes gérées.
7. Vérifier SSR sans token, destruction du root, et tests des cinq applications avant suppression des interceptors historiques. Tester les usages SignalR séparément (non interceptés ici).
8. Vérifier les routes d'upload, les appels nécessitant cookies, les éventuels reverse proxies partagés et les requêtes absolues vers différentes API. Une application avec plusieurs audiences doit disposer de contextes distincts, pas élargir le périmètre de ce token.

Rollback : retirer le nouvel interceptor et revenir au package preview.8c, puis restaurer les providers précédents (sans double authentification). Ne pas activer le mode partagé en production avant recette.
