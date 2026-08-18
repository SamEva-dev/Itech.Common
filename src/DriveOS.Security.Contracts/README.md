# DriveOS.Security.Contracts

`DriveOS.Security.Contracts` est le **contrat d'autorisation public de DriveOS**.

Il contient les codes de permissions, les rôles intégrés, le catalogue de métadonnées et la matrice de permissions par défaut utilisés par DriveOS, AuthGate et les outils d'administration tels qu'AccessManager.

> Les codes publiés par ce package sont des contrats stables. Une permission existante ne doit pas être renommée ou réutilisée pour une autre action métier après publication.

## Installation

```bash
dotnet add package DriveOS.Security.Contracts
```

Le package dépend de :

```text
Itech.Security.Contracts
```

## Identité de l'application

```csharp
using DriveOS.Security.Contracts;

Console.WriteLine(DriveOsApplication.Code); // driveos

var applicationCode = DriveOsApplication.ApplicationCode;
```

`DriveOsApplication.ApplicationCode` est un `Itech.Security.Contracts.Applications.ApplicationCode`.

## Utiliser une permission dans une API

Exemple avec une policy construite depuis un code de permission :

```csharp
using DriveOS.Security.Contracts;

string requiredPermission =
    DriveOsPermissionCodes.Organizations.Read;
```

Quelques exemples actuellement exposés :

```csharp
DriveOsPermissionCodes.Organizations.Read
DriveOsPermissionCodes.Organizations.Create
DriveOsPermissionCodes.Branches.Read
DriveOsPermissionCodes.Branches.Update
DriveOsPermissionCodes.BranchManagers.Assign
DriveOsPermissionCodes.CrmLeads.Read
DriveOsPermissionCodes.CrmActivities.Create
DriveOsPermissionCodes.CrmAssessments.Schedule
DriveOsPermissionCodes.CrmOffers.Read
```

Les permissions sont organisées par domaine fonctionnel :

```csharp
DriveOsPermissionCodes.Organizations.All
DriveOsPermissionCodes.Branches.All
DriveOsPermissionCodes.BranchManagers.All
DriveOsPermissionCodes.CrmLeads.All
DriveOsPermissionCodes.CrmActivities.All
```

Cela permet d'attribuer un groupe cohérent de permissions sans dupliquer leurs chaînes.

## Vérification dans du code applicatif

Votre infrastructure d'autorisation peut travailler exclusivement avec le code stable :

```csharp
var permission = DriveOsPermissionCodes.CrmLeads.Create;

if (!currentUser.HasPermission(permission))
{
    return Results.Forbid();
}
```

`HasPermission` est volontairement illustratif : l'évaluation concrète est fournie par votre couche IAM/AuthGate, pas par ce package.

## Catalogue complet pour AuthGate / AccessManager

`DriveOsPermissionCatalog.All` expose les permissions sous forme de `PermissionDefinition`.

```csharp
using DriveOS.Security.Contracts;

var catalog = DriveOsPermissionCatalog.All;

foreach (var permission in catalog)
{
    Console.WriteLine(
        $"{permission.ApplicationCode} | " +
        $"{permission.Category} | " +
        $"{permission.Code}");
}
```

Exemple d'exposition HTTP :

```csharp
app.MapGet("/api/security/permission-catalog", () =>
    Results.Ok(DriveOsPermissionCatalog.All));
```

Ce endpoint peut être consommé par un service d'administration afin de synchroniser automatiquement les permissions disponibles.

## Exemple de synchronisation côté IAM

```csharp
foreach (var definition in DriveOsPermissionCatalog.All)
{
    await permissionRegistry.UpsertAsync(
        applicationCode: definition.ApplicationCode,
        permissionCode: definition.Code,
        displayName: definition.DisplayName,
        description: definition.Description,
        category: definition.Category,
        cancellationToken);
}
```

Le mécanisme `UpsertAsync` appartient à votre application. Le package fournit le **catalogue**, pas le stockage.

## Rôles DriveOS

`DriveOsRoleCodes` centralise les codes de rôles prédéfinis.

```csharp
DriveOsRoleCodes.OrganizationOwner
DriveOsRoleCodes.OrganizationAdministrator
DriveOsRoleCodes.Director
DriveOsRoleCodes.BranchManager
DriveOsRoleCodes.PedagogicalManager
DriveOsRoleCodes.AdministrativeManager
DriveOsRoleCodes.Secretary
DriveOsRoleCodes.Accountant
DriveOsRoleCodes.FleetManager
DriveOsRoleCodes.ExamCoordinator
DriveOsRoleCodes.Instructor
DriveOsRoleCodes.SalesAdvisor
DriveOsRoleCodes.ComplianceOfficer
DriveOsRoleCodes.TrainingCoordinator
DriveOsRoleCodes.Receptionist
DriveOsRoleCodes.SupportAgent
DriveOsRoleCodes.ReadOnly
```

Lister tous les rôles :

```csharp
foreach (var role in DriveOsRoleCodes.All)
{
    Console.WriteLine(role);
}
```

Groupes utiles :

```csharp
DriveOsRoleCodes.PlatformRoles
DriveOsRoleCodes.TenantAdministrationRoles
DriveOsRoleCodes.BranchAdministrationRoles
```

## Matrice de permissions par défaut

`DriveOsRolePermissionDefaults` fournit la matrice initiale de seeding.

```csharp
var permissions =
    DriveOsRolePermissionDefaults.GetPermissions(
        DriveOsRoleCodes.BranchManager);

foreach (var permission in permissions)
{
    Console.WriteLine(permission);
}
```

Ou :

```csharp
if (DriveOsRolePermissionDefaults.TryGetPermissions(
        DriveOsRoleCodes.Secretary,
        out var secretaryPermissions))
{
    // seed / synchronization
}
```

Accès à toute la matrice :

```csharp
IReadOnlyDictionary<string, IReadOnlyCollection<string>> matrix =
    DriveOsRolePermissionDefaults.All;
```

### Important : matrice bootstrap, pas source de vérité runtime

La matrice définit des **valeurs par défaut pour l'initialisation**. Après synchronisation, AuthGate reste la source de vérité et peut conserver des personnalisations propres à chaque tenant.

Un bon flux est :

```text
DriveOS.Security.Contracts
        ↓
Catalogue + rôles par défaut
        ↓
AuthGate / AccessManager synchronise
        ↓
Base IAM = source de vérité runtime
        ↓
Utilisateurs / rôles / personnalisations par organisation
```

## Ajouter une nouvelle permission

Ajoutez d'abord un code stable dans le groupe fonctionnel correspondant.

```csharp
public static class Students
{
    public const string Read = "Students.Read";
    public const string Create = "Students.Create";

    public static readonly string[] All =
    [
        Read,
        Create
    ];
}
```

Puis incluez ce groupe dans `DriveOsPermissionCodes.All` et, si nécessaire, dans les rôles bootstrap concernés.

Après publication d'une nouvelle version du package, AuthGate/AccessManager peut synchroniser le nouveau catalogue.

## Ne jamais coder les permissions en dur

À éviter :

```csharp
[Authorize(Policy = "Branches.Update")]
```

À préférer :

```csharp
[Authorize(Policy = DriveOsPermissionCodes.Branches.Update)]
```

Même principe dans les tests :

```csharp
Assert.Contains(
    DriveOsPermissionCodes.CrmLeads.Read,
    DriveOsRolePermissionDefaults.GetPermissions(
        DriveOsRoleCodes.SalesAdvisor));
```

## Frontend

Le frontend ne doit pas inventer ses propres chaînes de permissions. Les permissions de l'utilisateur doivent être retournées par le backend/AuthGate et comparées aux codes issus du contrat de sécurité exposé côté API.

Exemple conceptuel :

```typescript
if (auth.hasPermission('CrmLeads.Read')) {
  // afficher l'onglet Prospects
}
```

Pour réduire les chaînes dupliquées, une application frontend peut générer ses constantes depuis le catalogue ou les maintenir dans un module synchronisé avec ce package.

## Versionnement

Lorsqu'une permission est publiée :

1. ne pas changer sa signification ;
2. ne pas la renommer silencieusement ;
3. ajouter une nouvelle permission lorsqu'une nouvelle capacité apparaît ;
4. conserver une migration IAM lorsqu'une ancienne permission doit être retirée ;
5. publier une nouvelle version du package avant de déployer les consommateurs.

## Packages associés

- `Itech.Security.Contracts` : primitives d'autorisation génériques.
- `Itech.Application.Contracts` : contrats applicatifs génériques.
- `Itech.Querying` : helpers de requêtes dynamiques.
