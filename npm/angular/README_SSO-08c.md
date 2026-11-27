# SSO-08c — Session OIDC en mémoire et réautorisation

Appliquer après SSO-08a et 08b. Package proposé `@itech/authgate` 0.1.0-preview.8c.

## Décision de compatibilité

Le dernier `OidcTokenExchangeController` livré (SSO-05e3) n'accepte que `IsAuthorizationCodeGrantType()` ; `AuthGateOidcExtensions` (SSO-05e2) n'enregistre que `AllowAuthorizationCodeFlow()` et fixe la durée Access Token à 10 minutes. Aucun refresh token OIDC n'est disponible dans ce contrat. Ne pas ajouter `offline_access`, ne pas appeler le refresh Legacy pour une session OIDC. Aucun changement backend dans cet incrément.

La méthode `renew()` déclenche une nouvelle autorisation top-level PKCE, exclusivement à la demande du consommateur. Le cookie AuthGate peut permettre le SSO sans ressaisie, selon sa validité et les politiques du serveur. Aucun iframe silencieux, aucun retry automatique, aucun redirect sur expiration. Le renouvellement remplace la session locale et peut interrompre des opérations métier en cours ; ne pas le déclencher en plein formulaire non sauvegardé. La rotation de refresh tokens OIDC n'est pas implémentée.

## Intégration future

`provideAuthGate(config)` enregistre `AuthGateSession` dans l'injecteur racine, en plus du client OIDC. Les consommateurs devront utiliser cette façade pour les opérations de session, sans appeler directement `AuthGateOidcClient` en parallèle. Une instance par configuration/application. Exemple :

```ts
const session = inject(AuthGateSession);
// Action explicite de connexion :
await session.start('/dashboard');
// Uniquement sur la route callback, dans un autre chargement de document :
const returnUrl = await session.complete();
await router.navigateByUrl(returnUrl);
// Action explicite de renouvellement :
await session.renew('/dashboard');
```

Les erreurs de ces promesses doivent être interceptées par le consommateur. Ne pas relancer automatiquement `start()` depuis le callback en erreur. `state()` est un Signal en lecture seule : anonymous, authenticating, authenticated (subject seulement), expired, unavailable. Ce signal n'est pas une preuve d'autorisation métier. `getAccessToken()` est destiné au futur interceptor, pas aux composants UI. L'interceptor devra borner strictement les destinations et vérifier l'expiration à chaque lecture (SSO-08d). `invalidateIfCurrent(token)` permet d'ignorer un 401 tardif pour un ancien token ; ne pas l'appeler pour un 403 métier. `clearLocal()` efface uniquement la session et la transaction locale, sans déconnecter le cookie SSO ni révoquer le token serveur. En cas d'échec de suppression de sessionStorage, le token en mémoire est quand même effacé et une erreur stable est émise. La déconnexion distante et la synchronisation multi-onglets sont prévues en 08f.

Seule la transaction PKCE reste dans sessionStorage. Le token n'est ni persisté, ni inclus dans les états UI ; un rechargement complet ou un nouvel onglet nécessite une nouvelle autorisation explicite. Une XSS peut toujours lire un token accessible au code de l'application : CSP, maîtrise des dépendances et prévention XSS restent indispensables. Aucune garantie de révocation serveur par effacement mémoire.

L'expiration locale est anticipée de 5 secondes, à partir de `expiresIn` déjà minoré par la durée de l'échange en 08b. Deux horloges (Date.now et performance.now) sont contrôlées avant chaque accès ; le timer met aussi à jour le signal. Une veille du navigateur peut retarder le timer : la prochaine lecture du token vérifie les échéances. Les API restent seules responsables de l'expiration et de la révocation réelles.

Les opérations simultanées du même type partagent la promesse, un callback et un démarrage incompatibles se refusent. Un effacement local annule les opérations et empêche un résultat tardif de restaurer un token. Si une navigation a déjà été lancée, elle ne peut pas être annulée rétroactivement. Après annulation pendant le calcul PKCE, attendre la fin de l'opération annulée avant de redémarrer si le client OIDC est encore occupé. Une destruction Angular ne supprime pas la transaction nécessaire à un callback après navigation.

## Contrôles

- 119 tests noyau TypeScript réussis (89 précédents + 26 session + 4 annulation OIDC) avec Node 22.16.0, TypeScript global 5.8.3. Les services HTTP et horloges des nouveaux tests sont simulés ; les tests RSA précédents utilisent WebCrypto.
- Build Angular/ng-packagr non exécuté (dépendances Angular non installées). Aucun package publié, aucun frontend métier modifié. Tester la DI et les Signals dans Angular 20.3 et 22 avant activation.
- Depuis `Itech.Common/angular/authgate` : `npm install`, `npm test`, `npm run build`, `npm run pack:local`.
- Recette intégrée : connexion, callback, expiration réelle, renouvellement cookie valide/invalide, MFA, annulation pendant échange, double clic, rechargement de page, 401 tardif, absence de token dans localStorage/sessionStorage, isolement des onglets. Contrôler CORS et les URLs AuthGate existantes.
- Codes nouveaux à traduire FR/EN par les consommateurs lors de leur intégration : `AUTHGATE_SESSION_CANCELLED`, `AUTHGATE_SESSION_BUSY`, `AUTHGATE_SESSION_EXPIRED`.

Aucune migration, aucune suppression, aucun secret réel. Pour rollback, restaurer les fichiers modifiés et retirer les nouveaux fichiers de session après vérification des références ; ne pas republier une même version NPM avec du contenu différent.
