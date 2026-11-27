# @itech-core/authgate — intégration SSO-10.0

Ce répertoire attend les sources originales de la bibliothèque Angular SSO-09g. Aucun substitut ni package vide n'est fourni. Le workflow ignore la publication tant que package.json est absent.

## Contrat de build à intégrer avec les sources

- `package.json` de travail non publié, version identique au package final ; scripts `test` (non interactif) et `build` (Angular partial compilation via ng-packagr).
- `package-lock.json` versionné, configuration Angular et TypeScript, sources et tests originaux.
- Sortie compilée `dist/authgate/` avec package.json, exports, types, peerDependencies et fichiers de distribution Angular.
- Nom final `@itech-core/authgate`, version initiale proposée `0.1.0-preview.9g` si disponible, `publishConfig` : `{"access":"public","registry":"https://registry.npmjs.org/"}`, repository.url correspondant exactement au dépôt GitHub réel. Licence à valider avant publication.
- Les consommateurs utilisent Angular 20 et 22 ; compiler avec une chaîne compatible et tester les deux versions avant d'annoncer une plage de peerDependencies. Ne pas imposer Angular 22 aux consommateurs Angular 20.
- Inspecter `npm pack --dry-run` et le tarball : aucun secret, fichier de configuration interne, fixture sensible ou source map non souhaitée. Le contrôle automatique des métadonnées ne remplace pas cette revue.

## Première publication

Après intégration et revue du vrai package, depuis npm/authgate : `npm ci`, `npm test`, `npm run build`, `npm pack ./dist/authgate --dry-run`, puis `npm login` et `npm publish ./dist/authgate --access public --tag preview` (2FA). Ne pas publier de placeholder. Pour cette première publication, effectuer la configuration sur une branche de travail avant fusion ; si le premier push sur main contient la même version déjà publiée, incrémenter la version réelle avant fusion.

Dans npmjs.com, ouvrir le package > Settings > Trusted publishing > GitHub Actions. Renseigner le propriétaire GitHub réel (pas nécessairement itech-core), le dépôt réel, le workflow `publish-npm.yml`, autoriser `npm publish`, laisser Environment vide. Configurer peu avant la première publication CI (expiration de la configuration initiale : 2 jours). Le workflow utilise OIDC, sans NPM_TOKEN. Les runners sont hébergés par GitHub. Les préversions utilisent le tag preview, les versions stables latest. Une version déjà publiée est immuable : incrémenter la version avant un push de release ; le workflow échoue si elle existe déjà.

## Migration des consommateurs (après publication)

Pour préserver les imports existants, utiliser temporairement un alias :

```sh
npm install @itech/authgate@npm:@itech-core/authgate@0.1.0-preview.9g --save-exact
npm ci
npm run build
```

Ou migrer tous les imports vers `@itech-core/authgate` puis installer directement ce nom. Ne pas conserver les deux noms en même temps. Versionner le lockfile réellement régénéré. Ne pas modifier les cinq applications avant la disponibilité du vrai package.

## NuGet

Le workflow .NET existant reste inchangé. Il utilise Trusted Publishing mais fixe encore PACKAGE_VERSION à 5.0.2 dans l'archive examinée ; vérifier la version publiée et appliquer le lot 1 avant de pousser des changements .NET. Les changements sous npm/ seuls ne déclenchent pas le workflow de publication NuGet existant.
