# SSO-08a — Fondation Angular @itech/authgate

Nouveau sous-projet autonome de Itech.Common : `angular/authgate`. Aucun fichier existant des cinq frontends, des API .NET, des workflows de publication NuGet ou des interfaces utilisateur n'est modifié. Pas de dépendance vers un métier. Aucune publication npm effectuée.

## Sources et compatibilité

Versions observées dans les package.json des archives fournies :

| Application | Angular | TypeScript | SDK OIDC déclaré |
|---|---|---|---|
| TP-ECSR-Pilot | 22.1.7 | ~6.0.2 | oidc-client-ts 3.5.0 |
| LocaGuest WEB | ^20.3.0 | ~5.9.2 | oidc-client-ts 3.5.0 |
| MobiOne WEB | ^22.0.0 | ~6.0.0 | oidc-client-ts 3.5.0 |
| Manager | ^20.3.0 | ~5.9.2 | aucun dans package.json |
| VPS Control Center UI | 22.2.3 | ~6.0.0 | aucun dans package.json |

Le choix de compilation partielle Angular 20.3 / TypeScript 5.9 permet de cibler la génération la plus ancienne. Les peerDependencies déclarent Angular >=20.3 <23 et RxJS 7.8 ; ce n'est pas une certification de compatibilité. Tester les cinq builds réels, leurs versions Node et la compilation/linker Angular avant publication. Ne pas mettre à niveau les applications pour ce seul incrément. Le SDK OIDC sera sélectionné et intégré en 08b, après vérification de son contrat ; la présence d'un package dans les manifestes ne prouve pas qu'il est utilisé sur chaque chemin.

## Contenu

- `AuthGateConfig`, `ValidatedAuthGateConfig`, `validateAuthGateConfig` : paramètres explicites, URL HTTPS, autorité racine, exception HTTP loopback explicitement activée pour frontend et API uniquement, callback et logout sur l'origine de l'application, scope openid requis, configuration immuable et namespace de stockage par contexte.
- `safeAuthGateReturnUrl` : redirection relative locale uniquement, refus des callbacks et des chemins ambigus, y compris les paramètres de matrice Angular ; fallback `/`. Les routes métier doivent encore contrôler leurs propres paramètres de redirection et permissions.
- `AuthGateConfigurationError` et codes stables non sensibles : les traductions FR/EN seront intégrées côté consommateur lors du raccordement ; aucun libellé utilisateur imposé.
- `AuthGateIdentity`, `AuthGateSessionState` : contrats seulement, pas de session fonctionnelle, pas de token dans les états UI.
- `AUTHGATE_CONFIG`, `provideAuthGate` : provider Angular standalone, sans accès implicite à window ou au stockage. Fournir l'origine réelle côté application à partir de sa configuration de confiance. La fonction ne vérifie pas elle-même l'origine d'exécution du navigateur ; cette vérification est prévue à l'entrée des parcours OIDC en 08b. Un seul provider par injecteur racine.

Aucun stockage, réseau, cryptographie OIDC, callback, renouvellement, guard, interceptor ou permission effective n'est implémenté dans cet incrément. Ne pas remplacer un service d'authentification existant par ce socle seul. `apiAudience` est un paramètre de configuration, pas une validation de JWT. Les API restent autorité de décision. Le namespace n'est ni un secret ni une frontière de sécurité contre XSS.

## Installation locale (PowerShell)

Depuis Itech.Common, après extraction du patch :

```powershell
cd angular/authgate
npm install
npm test
npm run build
npm run pack:local
```

Le tarball attendu est `itech-authgate-0.1.0-preview.8a.tgz`. Le lockfile n'est pas livré (résolution des dépendances npm indisponible dans l'environnement de travail) : le générer, le contrôler et le versionner avant d'utiliser `npm ci` en CI. Ne pas publier automatiquement ; réserver le scope @itech dans le registre npm privé/approprié et définir sa politique de publication. Les workflows .NET ne sont pas modifiés.

Exemple de futur enregistrement (aucune intégration d'application à ce stade) :

```ts
import { provideAuthGate } from '@itech/authgate';

export const authProviders = [provideAuthGate({
  authority: 'https://auth.example.invalid/',
  clientId: 'example-web',
  apiAudience: 'api://example',
  scope: 'openid profile email',
  appOrigin: 'https://app.example.invalid',
  redirectUri: 'https://app.example.invalid/auth/callback',
  apiBaseUrl: 'https://api.example.invalid/'
})];
```

Le mode dev `allowInsecureLoopback: true` permet les URL HTTP de localhost/127.0.0.1/[::1] pour le frontend et l'API, mais pas pour l'autorité. Ne jamais utiliser un secret client dans le frontend. Adapter les paramètres aux environnements existants. L'autorité à base de chemin non racine n'est pas prise en charge par ce socle.

## Vérification réalisée

- Node 22.16.0, TypeScript global 5.8.3 : compilation du noyau indépendant d'Angular et 47 tests Node réussis.
- Tests : normalisation, namespace, erreurs non sensibles, HTTP loopback, URL invalides, callbacks et redirections ambiguës, retours locaux valides.
- Compilation Angular/ng-packagr, DI Angular réelle, installation du tarball dans les cinq frontends et compatibilité Angular 22 : non exécutées. Aucun package npm publié.
- Aucun mock en production ; tests sans réseau.

## Prochaine étape

SSO-08b : choix et intégration du SDK OIDC, découverte de l'autorité, Authorization Code + PKCE S256, state et nonce, validation du callback et du contexte courant. Pas de bascule des applications avant les incréments d'intégration et leur recette.
