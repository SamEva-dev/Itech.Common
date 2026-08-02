# Itech.Common

`Itech.Common` regroupe les composants .NET génériques et réutilisables de l’écosystème Itech.

La solution fournit des contrats partagés pour la sécurité, les erreurs, la pagination et le tri, ainsi que des utilitaires de construction de requêtes dynamiques. Elle ne contient aucune règle métier propre à DriveOS, LocaGuest ou à une autre application.

## Packages NuGet

| Package | Rôle |
| --- | --- |
| `Itech.Security.Contracts` | Contrats génériques d’autorisation multi-application : application, permission, rôle, portée et contexte d’autorisation. |
| `Itech.Application.Contracts` | Contrats applicatifs communs : pagination, tri et erreurs structurées traduisibles par les fronts. |
| `Itech.Querying` | Extensions pour construire des `DynamicQueryOptions` de DomainRelay de manière cohérente. |

Les packages sont publiés séparément afin qu’une application ne référence que les composants dont elle a réellement besoin.

## Prérequis

- .NET SDK 9.0 ou supérieur compatible ;
- une source NuGet contenant les packages Itech ;
- `DomainRelay.Mapping.Expressions` pour utiliser `Itech.Querying`.

## Installation

Installer uniquement les packages nécessaires au projet :

```bash
dotnet add package Itech.Security.Contracts
dotnet add package Itech.Application.Contracts
dotnet add package Itech.Querying
```

Il est recommandé de fixer les versions avec la gestion centralisée des packages NuGet :

```xml
<!-- Directory.Packages.props -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <PackageVersion Include="Itech.Security.Contracts" Version="1.0.0" />
    <PackageVersion Include="Itech.Application.Contracts" Version="1.0.0" />
    <PackageVersion Include="Itech.Querying" Version="1.0.0" />
  </ItemGroup>
</Project>
```

Les projets référencent alors les packages sans répéter leur version :

```xml
<ItemGroup>
  <PackageReference Include="Itech.Security.Contracts" />
  <PackageReference Include="Itech.Application.Contracts" />
  <PackageReference Include="Itech.Querying" />
</ItemGroup>
```

## Itech.Security.Contracts

### Déclarer une application

Chaque produit définit son propre code applicatif dans son package métier :

```csharp
using Itech.Security.Contracts.Applications;

public static class DriveOsApplication
{
    public static readonly ApplicationCode Code = new("driveos");
}
```

`ApplicationCode` supprime les espaces de début et de fin, normalise la valeur en minuscules et limite sa longueur à 100 caractères.

### Déclarer une permission

```csharp
using Itech.Security.Contracts.Authorization;

public static class DriveOsPermissions
{
    public static readonly PermissionDefinition OrganizationsRead = new(
        DriveOsApplication.Code,
        "Organizations.Read",
        "Consulter les organisations DriveOS.");
}
```

L’identité fonctionnelle d’une permission est le couple :

```text
(ApplicationCode, PermissionCode)
```

Un même code de permission peut ainsi exister dans plusieurs applications sans collision.

### Déclarer un rôle

```csharp
using Itech.Security.Contracts.Authorization;

public static class DriveOsRoles
{
    public static readonly RoleDefinition OrganizationOwner = new(
        DriveOsApplication.Code,
        "DriveOS.OrganizationOwner",
        RoleScope.Organization,
        "Propriétaire d’une organisation DriveOS.");
}
```

Portées disponibles :

| Portée | Utilisation |
| --- | --- |
| `RoleScope.Organization` | Rôle limité à une organisation. |
| `RoleScope.Application` | Rôle valable dans toute une application. |
| `RoleScope.Platform` | Rôle global à la plateforme Itech. |

Le rôle plateforme de super-administrateur est fourni par le socle :

```csharp
using Itech.Security.Contracts.Platform;

var roleCode = PlatformRoleCodes.SuperAdministrator;
// platform.super-administrator
```

### Transporter le contexte d’autorisation

```csharp
using Itech.Security.Contracts.Authorization;

var context = new AuthorizationContext(
    DriveOsApplication.Code,
    organizationId);
```

Ce contexte permet de résoudre les rôles et permissions dans la bonne application et la bonne organisation.

> Les catalogues métier restent dans des packages dédiés tels que `DriveOS.Security.Contracts` et `LocaGuest.Security.Contracts`. Ils ne doivent pas être ajoutés à `Itech.Security.Contracts`.

## Itech.Application.Contracts

### Pagination

```csharp
using Itech.Application.Contracts.Pagination;

var request = new PageRequest(page: 2, pageSize: 20);

var result = new PagedResult<string>(
    items: ["A", "B"],
    page: request.Page,
    pageSize: request.PageSize,
    totalCount: 42);

Console.WriteLine(request.Skip);       // 20
Console.WriteLine(result.TotalPages);  // 3
Console.WriteLine(result.HasNextPage); // true
```

Valeurs communes :

| Paramètre | Valeur |
| --- | ---: |
| Page par défaut | `1` |
| Taille par défaut | `20` |
| Taille maximale | `100` |

`PageRequest` refuse une page inférieure à 1 et une taille située hors de l’intervalle 1 à 100.

### Erreurs structurées

Les erreurs applicatives exposent une clé stable et des paramètres, plutôt qu’un message backend utilisé comme contrat avec le front :

```csharp
using Itech.Application.Contracts.Errors;

var error = new ErrorDescriptor(
    "organizations.name_already_exists",
    new Dictionary<string, object?>
    {
        ["name"] = "Auto-école Nice Centre"
    });
```

Le front peut traduire `Key` et injecter les valeurs de `Parameters` dans le message localisé.

### Tri

```csharp
using Itech.Application.Contracts.Sorting;

var direction = SortDirection.Descending;
```

## Itech.Querying

Ce package complète `DomainRelay.Mapping.Expressions.Dynamic` avec des méthodes de construction lisibles.

```csharp
using DomainRelay.Mapping.Expressions.Dynamic;
using Itech.Querying;

var options = new DynamicQueryOptions()
    .AddEquals("OrganizationId", organizationId)
    .AddContains("Name", search)
    .AddGreaterThanOrEqual("CreatedAt", createdFrom)
    .AddLessThanOrEqual("CreatedAt", createdTo)
    .AddSortOrDefault(
        sortBy,
        sortDirection,
        defaultMemberName: "CreatedAt",
        defaultDirection: DynamicSortDirection.Desc);
```

Les valeurs `null`, les chaînes vides et les chaînes composées uniquement d’espaces sont ignorées lorsque cela s’applique. `AddSortOrDefault` utilise le membre et la direction par défaut lorsqu’aucun tri n’est demandé.

Une application peut centraliser cette construction avec :

```csharp
using DomainRelay.Mapping.Expressions.Dynamic;
using Itech.Querying;

public sealed class OrganizationQueryOptionsFactory
    : IDynamicQueryOptionsFactory<GetOrganizationsRequest>
{
    public DynamicQueryOptions Create(GetOrganizationsRequest request) =>
        new DynamicQueryOptions()
            .AddContains("Name", request.Search)
            .AddSortOrDefault(
                request.SortBy,
                request.SortDirection,
                "CreatedAt");
}
```

## Structure de la solution

```text
Itech.Common
├── src
│   ├── Itech.Application.Contracts
│   ├── Itech.Querying
│   └── Itech.Security.Contracts
├── tests
│   ├── Itech.Application.Contracts.Tests
│   ├── Itech.Querying.Tests
│   └── Itech.Security.Contracts.Tests
├── .github/workflows
│   ├── ci.yml
│   └── publish.yml
├── Directory.Build.props
├── Directory.Packages.props
└── Itech.Common.slnx
```

## Développement local

```bash
dotnet restore Itech.Common.slnx
dotnet build Itech.Common.slnx --configuration Release --no-restore
dotnet test Itech.Common.slnx --configuration Release --no-build --no-restore
dotnet pack Itech.Common.slnx --configuration Release --no-build --output artifacts
```

## Intégration continue

- `ci.yml` restaure, compile et teste la solution lors des Pull Requests et des pushs sur `main`.
- `publish.yml` compile, teste, crée les packages puis les publie sur NuGet.org.
- La publication utilise NuGet Trusted Publishing ; aucune clé API NuGet permanente ne doit être ajoutée au dépôt.

## Publication d’une nouvelle version

La version est pilotée manuellement dans `.github/workflows/publish.yml` :

```yaml
env:
  # Seule valeur à modifier avant une nouvelle publication
  PACKAGE_VERSION: "1.0.0"
```

Pour publier une nouvelle version :

1. modifier `PACKAGE_VERSION`, par exemple de `1.0.0` vers `1.0.1` ;
2. committer les changements ;
3. pousser sur la branche `main` ;
4. vérifier le workflow **Publish NuGet packages** dans GitHub Actions.

Une version déjà publiée sur NuGet ne peut pas être remplacée.

Le versionnement suit [Semantic Versioning](https://semver.org/) :

| Type de changement | Exemple |
| --- | --- |
| Correction rétrocompatible | `1.0.0` → `1.0.1` |
| Nouvelle fonctionnalité rétrocompatible | `1.0.1` → `1.1.0` |
| Rupture de compatibilité | `1.1.0` → `2.0.0` |

## Principes d’architecture

- le socle `Itech.*` reste générique et indépendant des produits ;
- les applications déclarent leurs propres codes, permissions et rôles ;
- les lectures applicatives utilisent le non-tracking par défaut lorsque EF Core est concerné ;
- le tracking doit être demandé explicitement lorsqu’un agrégat doit être modifié ;
- les options ambiguës, notamment les booléens de tracking, sont remplacées par des méthodes dédiées ou des options explicites ;
- les erreurs métier utilisent une clé stable et des paramètres pour permettre leur traduction côté client ;
- une modification incompatible d’un contrat public nécessite une nouvelle version majeure.

## Migration

Le passage progressif depuis `Locaguest.Common.lib` est décrit dans [MIGRATION.md](MIGRATION.md).

## Licence

Les packages sont publiés sous licence MIT.
