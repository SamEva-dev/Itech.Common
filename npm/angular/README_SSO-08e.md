# SSO-08e — Guards, permissions et workspaces

Pré-requis : intégrer SSO-08a à 08d, puis ce patch. Version du package : `0.1.0-preview.8e`.
Ce patch ajoute le noyau d'accès métier, son transport dédié et les guards Angular. Il ne modifie aucun consommateur.

## Contrats vérifiés dans les archives source

- PedagoraPilot / LocaGuest : `GET /api/v1/session/bootstrap` retourne `subject`, `state`, `workspaces` (id, organizationId, label, active), `selectedWorkspaceId: null`, `permissionsResolved`, `businessSessionEstablished`, `version`. La découverte ne confère aucun droit. `POST /api/v1/session/context` avec `{workspaceId}` retourne le contexte sélectionné, notamment `organizationId`, `selectedWorkspaceId`, `roles` et `permissions` ; il ne renvoie pas nécessairement `workspaces`. Les deux identifiants doivent correspondre à la sélection. `context_verified` ne suffit pas pour autoriser l'UI.
- MobiOne : `GET /api/v1/session/bootstrap`, `workspaces: []`, `selectedWorkspaceId: null`, version 1. Les états `profile_verified`, `profile_pending`, `onboarding_required` sont non autorisés et leur raison est conservée. Aucun identifiant d'organisation artificiel. Les détails du profil restent du ressort de l'adaptateur métier à venir.
- Manager : contrat d'administration plateforme différent. Son ancien guard autorise en l'absence de bootstrap ; ce comportement doit être corrigé lors de SSO-08j. Ne pas traiter une preuve plateforme comme un rôle métier générique.
- VPS : autorisations locales et accès SSH distincts. Pas d'assimilation automatique au bootstrap multi-tenant.

## Configuration future (exemple PedagoraPilot)

```ts
provideAuthGate(config),
provideAuthGateAccess({
  workspaceMode: 'required',
  authorizationVersion: 1,
  bootstrapUrl: 'https://api.example.invalid/api/v1/session/bootstrap',
  contextUrl: 'https://api.example.invalid/api/v1/session/context'
})
```

Les URL sont des exemples : utiliser les routes du produit réellement configurées, dans la base API autorisée par `provideAuthGate`. Si API et SPA partagent une origine, un préfixe API explicite est requis. Pour LocaGuest V2, configurer `authorizationVersion: 2` **uniquement** lorsque le backend utilise V2. Une divergence refuse les droits ; aucune conversion automatique V1/V2 ni expansion implicite du catalogue. Les codes sont comparés exactement (casse comprise). Les mappings d'alias et la hiérarchie V2 propres à LocaGuest doivent rester explicites dans son intégration. Pour MobiOne, utiliser `workspaceMode: 'none'`, version 1 et omettre `contextUrl`.

```ts
const access = inject(AuthGateAccess);
await access.discover();
// En mode required, afficher access.state().workspaces et faire choisir un ID exact.
await access.selectWorkspace(id);
const canView = access.allows({ allPermissions: ['CODE_CANONIQUE'] });
```

L'appel de découverte ou de sélection doit être déclenché explicitement dans le parcours callback/bootstrap ou dans un écran de sélection. Aucun réseau ni redirection automatique au démarrage ou depuis un guard. `state()` est un Signal en lecture seule. `allows` vérifie également l'expiration effective de la session (utile si le timer navigateur est suspendu). Une sélection obsolète renvoie un état `idle` neutre, jamais les anciens grants ; l'état courant est exposé par le Signal.

```ts
// Exemples de guards ; remplacer les codes par les codes du catalogue métier.
canActivate: [authGateAuthenticatedGuard] // identité seulement, inscription/onboarding
canActivate: [requireAuthGateAccess({allPermissions:['CODE_CANONIQUE']})]
canMatch: [matchAuthGateAccess({anyRoles:['ROLE_CANONIQUE']})]
```

`requireAuthGateAccess()` sans critère exige une session métier prête. Si les deux critères sont fournis, ils doivent être satisfaits simultanément : toutes les permissions, au moins un rôle. Les listes explicitement vides sont refusées. Aucun wildcard, privilège implicite ou droit dérivé d'un rôle. Les guards retournent un booléen ; l'application choisit l'écran de refus. Attention : `CanMatch` false peut faire essayer une route alternative. Ne pas utiliser les guards d'identité seule pour les routes sensibles.

## Sécurité et limites

- Les grants sont uniquement en mémoire et liés à l'objet d'état de la session, y compris si le même utilisateur se reconnecte. Un switch, une erreur, une expiration ou un logout invalide le contexte. Les requêtes précédentes sont annulées et leur réponse ne peut pas écraser la nouvelle sélection.
- Transport Fetch dédié (ne passe pas par les interceptors Angular) : destinations validées par le filtre 08d, GET/POST fixés, Bearer courant, cookies omis, redirection refusée, délai 10 s, réponse JSON limitée à 1 Mio. Aucun retry. 401 courant invalide la session locale, 403/5xx ne la déconnectent pas ; les droits deviennent indisponibles. L'erreur publique ne contient ni corps serveur, ni token.
- La sélection UI ne transmet pas encore le contexte aux autres appels HTTP ou SignalR. Les intégrations 08g–08j devront conserver les headers, scopes, routes et restrictions propres aux applications. Ne pas activer ce package seul pour remplacer un contexte métier existant.
- Les permissions UI peuvent devenir obsolètes entre deux appels explicites ; aucun polling ou cache persistant. L'API doit toujours appliquer les droits courants et la séparation des ressources. Les guards ne constituent pas une frontière de sécurité serveur.
- Pas de contrôle automatique des abonnements, quotas, scopes pédagogiques fins, statut chauffeur au-delà du bootstrap, ou autorisations SSH. Aucun changement backend, aucun secret, aucune publication NPM.
- Les composants Angular, Router et l'intégration Fetch dans un navigateur réel doivent être compilés/testés. Les tests ici couvrent le noyau TypeScript et un transport Fetch avec réponses simulées.

## Traductions à intégrer dans les applications

| Code | FR | EN |
|---|---|---|
| AUTHGATE_ACCESS_UNAVAILABLE | Les droits d'accès sont momentanément indisponibles. | Access rights are temporarily unavailable. |
| AUTHGATE_CONFIGURATION_INVALID | Configuration d'authentification invalide. | Invalid authentication configuration. |

États à présenter selon le métier : `select_workspace` (sélectionner un espace de travail), `no_access` (aucun accès disponible), `not_ready` (contexte non prêt), `unavailable` (droits indisponibles). Aucun texte UI imposé par la bibliothèque.

## Recette et compilation

Depuis `Itech.Common/angular/authgate` :

```powershell
npm install
npm test
npm run build
npm run pack:local
```

237 tests du noyau TypeScript réussis ici (182 précédents + 55 nouveaux), avec Node 22.16.0 et le compilateur global TypeScript 5.8.3. Les dépendances Angular/NPM ne sont pas installées ici, donc la compilation ng-packagr et les tests Angular Router ne sont pas exécutés. Générer et versionner le lockfile localement.

Recette d'intégration : vérifier découverte à zéro/un/plusieurs workspaces, refus d'un workspace suspendu, sélection A puis B avec réponse A retardée, expiration et reconnexion même subject, 401 tardif, API 403/503, changement de catalogue V1/V2, profil chauffeur en attente, alias métier, restrictions d'abonnement, headers de contexte et isolation multi-tenant. Vérifier les routes alternatives CanMatch et les routes d'onboarding. Ne pas supprimer les anciens guards avant parité validée.
