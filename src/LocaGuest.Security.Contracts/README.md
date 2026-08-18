# LocaGuest.Security.Contracts

`LocaGuest.Security.Contracts` centralise le **contrat d'autorisation de LocaGuest** : application, permissions, policies, rôles et permissions par défaut.

Le package permet à LocaGuest, AuthGate, AccessManager et aux outils d'administration de partager les mêmes codes sans dupliquer des chaînes dans plusieurs projets.

## Installation

```bash
dotnet add package LocaGuest.Security.Contracts
```

Le package dépend de :

```text
Itech.Security.Contracts
```

## Application LocaGuest

```csharp
using LocaGuest.Security.Contracts;

Console.WriteLine(LocaGuestApplication.Code); // locaguest

var applicationCode = LocaGuestApplication.ApplicationCode;
```

## Permissions

Utilisez toujours les constantes exposées par `LocaGuestPermissionCodes`.

```csharp
using LocaGuest.Security.Contracts;

var readProperties = LocaGuestPermissionCodes.PropertiesRead;
var editProperties = LocaGuestPermissionCodes.PropertiesWrite;
var readContracts = LocaGuestPermissionCodes.ContractsRead;
var uploadDocument = LocaGuestPermissionCodes.DocumentsUpload;
var readPayments = LocaGuestPermissionCodes.PaymentsRead;
```

Exemples de domaines couverts par le catalogue actuel :

- tenant settings ;
- billing ;
- users ;
- roles ;
- properties ;
- tenants/locataires ;
- contracts ;
- documents ;
- rooms ;
- season ;
- payments ;
- deposits ;
- team ;
- analytics ;
- finance ;
- tax preparation ;
- audit ;
- sessions ;
- signatures ;
- rentability.

## Catalogue pour AuthGate / AccessManager

`LocaGuestPermissionCatalog.All` fournit les métadonnées de toutes les permissions déclarées.

```csharp
var catalog = LocaGuestPermissionCatalog.All;

foreach (var permission in catalog)
{
    Console.WriteLine(
        $"{permission.Category}: {permission.Code}");
}
```

Exposition depuis une API :

```csharp
app.MapGet("/api/security/permission-catalog", () =>
    Results.Ok(LocaGuestPermissionCatalog.All));
```

Un service d'administration peut alors synchroniser les permissions disponibles :

```csharp
foreach (var permission in LocaGuestPermissionCatalog.All)
{
    await permissionRegistry.UpsertAsync(
        permission.ApplicationCode,
        permission.Code,
        permission.DisplayName,
        permission.Description,
        permission.Category,
        cancellationToken);
}
```

## Rôles intégrés

```csharp
LocaGuestRoleCodes.TenantOwner
LocaGuestRoleCodes.TenantAdmin
LocaGuestRoleCodes.TenantManager
LocaGuestRoleCodes.TenantUser
LocaGuestRoleCodes.ReadOnly
LocaGuestRoleCodes.Occupant
LocaGuestRoleCodes.OccupantAdmin
LocaGuestRoleCodes.OccupantOwner
```

Lister tous les rôles :

```csharp
foreach (var role in LocaGuestRoleCodes.All)
{
    Console.WriteLine(role);
}
```

Groupes :

```csharp
LocaGuestRoleCodes.AdminRoles
LocaGuestRoleCodes.OperationalRoles
```

## Permissions par défaut d'un rôle

```csharp
var permissions =
    LocaGuestRolePermissionDefaults.GetPermissionsForRole(
        LocaGuestRoleCodes.TenantManager);

foreach (var permission in permissions)
{
    Console.WriteLine(permission);
}
```

Exemple de seeding :

```csharp
foreach (var role in LocaGuestRoleCodes.All)
{
    var permissions =
        LocaGuestRolePermissionDefaults.GetPermissionsForRole(role);

    await roleSeeder.UpsertRoleAsync(
        LocaGuestApplication.Code,
        role,
        permissions,
        cancellationToken);
}
```

Le stockage et la personnalisation finale des rôles restent sous la responsabilité d'AuthGate/AccessManager.

## Policies ASP.NET Core

`LocaGuestPolicyNameCodes` expose également les noms de policies historiques de l'application.

```csharp
LocaGuestPolicyNameCodes.ManageTenantSettings
LocaGuestPolicyNameCodes.ViewBilling
LocaGuestPolicyNameCodes.ManageUsers
LocaGuestPolicyNameCodes.ViewContracts
LocaGuestPolicyNameCodes.ViewAnalytics
LocaGuestPolicyNameCodes.IsTenantOwner
```

Exemple :

```csharp
[Authorize(Policy = LocaGuestPolicyNameCodes.ManageProperties)]
public async Task<IActionResult> UpdateProperty(...)
{
    ...
}
```

Les policies peuvent ensuite être reliées aux permissions correspondantes dans la configuration d'autorisation de l'application.

## Ajouter une permission

Ajoutez une constante stable :

```csharp
public const string InspectionsRead = "inspections.read";
public const string InspectionsWrite = "inspections.write";
```

Puis ajoutez-la au tableau global du catalogue et aux rôles bootstrap appropriés.

Une nouvelle version du package permettra alors aux consommateurs de découvrir la nouvelle permission.

## Exemple de contrôle manuel

```csharp
var requiredPermission = LocaGuestPermissionCodes.ContractsTerminate;

if (!currentUser.HasPermission(requiredPermission))
{
    return Results.Forbid();
}
```

`HasPermission` représente ici votre implémentation d'autorisation runtime.

## Bonnes pratiques

- Ne jamais recopier une chaîne de permission dans plusieurs services.
- Ajouter une permission plutôt que changer la sémantique d'une permission publiée.
- Conserver AuthGate comme source de vérité runtime après le seeding initial.
- Distinguer les rôles bootstrap des rôles personnalisés d'un tenant.
- Tester que chaque rôle intégré ne référence que des permissions existantes.
- Versionner le package avant de déployer une API utilisant une nouvelle permission.

## Packages associés

- `Itech.Security.Contracts` : primitives génériques utilisées par ce package.
- `Itech.Application.Contracts` : pagination et erreurs applicatives partagées.
- `Itech.Querying` : construction de filtres et tris dynamiques.
