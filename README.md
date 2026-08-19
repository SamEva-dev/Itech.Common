# Itech.Common — Shared Contracts & Infrastructure Building Blocks

`Itech.Common` est le dépôt qui centralise les **contrats techniques réutilisables** des applications Itech et les catalogues de sécurité spécifiques à DriveOS et LocaGuest.

Il ne correspond pas à un package NuGet monolithique : le dépôt publie plusieurs packages indépendants afin que chaque application ne référence que les contrats dont elle a réellement besoin.

## Packages disponibles

| Package | Rôle | Dépendance principale |
|---|---|---|
| `Itech.Security.Contracts` | Contrats d’autorisation neutres : applications, permissions, rôles et contexte d’autorisation | Aucune dépendance produit |
| `Itech.Application.Contracts` | Pagination, tri et erreurs applicatives réutilisables | Aucune dépendance produit |
| `Itech.Querying` | Helpers de construction de `DynamicQueryOptions` | `DomainRelay.Mapping.Expressions` |
| `DriveOS.Security.Contracts` | Catalogue de permissions et rôles DriveOS | `Itech.Security.Contracts` |
| `LocaGuest.Security.Contracts` | Catalogue de permissions, policies et rôles LocaGuest | `Itech.Security.Contracts` |

## Installation

Installez uniquement les packages nécessaires à votre projet :

```bash
dotnet add package Itech.Security.Contracts
dotnet add package Itech.Application.Contracts
dotnet add package Itech.Querying
dotnet add package DriveOS.Security.Contracts
dotnet add package LocaGuest.Security.Contracts
```

> Les exemples ci-dessous utilisent `x.y.z` lorsqu’une version doit être choisie. Utilisez la version compatible avec votre solution.

## Principes de conception

- **Contrats stables** : les codes publics de permissions, rôles et applications constituent des contrats inter-services.
- **Faible couplage** : le socle Itech ne dépend pas de DriveOS ou LocaGuest.
- **Multi-tenant explicite** : l’autorisation est évaluée dans un contexte application + organisation.
- **Lecture efficace** : les exemples de pagination sont compatibles avec des lectures EF Core `AsNoTracking()`.
- **Erreurs localisables** : le frontend doit pouvoir s’appuyer sur une clé stable et des paramètres plutôt que sur un message backend comme contrat.
- **Catalogues centralisés** : AuthGate, AccessManager et les applications métier doivent consommer les mêmes codes publiés.

## Choisir le bon package

Utilisez `Itech.Security.Contracts` pour construire un système d’autorisation ou un nouveau catalogue applicatif. Utilisez `DriveOS.Security.Contracts` ou `LocaGuest.Security.Contracts` lorsqu’un projet doit consommer les permissions concrètes du produit concerné. Utilisez `Itech.Application.Contracts` pour les contrats de pagination/erreur partagés et `Itech.Querying` pour la construction normalisée de filtres et tris dynamiques.

---

## Itech.Security.Contracts

**Socle d’autorisation multi-application.**

`Itech.Security.Contracts` fournit les contrats d'autorisation communs aux applications Itech.

Le package est volontairement **indépendant de DriveOS et de LocaGuest**. Il expose les types fondamentaux utilisés pour représenter une application, un contexte d'autorisation, des permissions et des rôles sans dépendre d'une API métier particulière.

### Installation

```bash
dotnet add package Itech.Security.Contracts
```

Ou dans un fichier projet :

```xml
<ItemGroup>
  <PackageReference Include="Itech.Security.Contracts" Version="x.y.z" />
</ItemGroup>
```

### Quand utiliser ce package ?

Utilisez `Itech.Security.Contracts` lorsque vous développez :

- une API qui doit identifier l'application courante ;
- un service d'identité ou d'autorisation ;
- un catalogue de permissions propre à une application ;
- un système de rôles multi-tenant ;
- une intégration avec AuthGate ou AccessManager ;
- une bibliothèque applicative qui ne doit pas dépendre de DriveOS ou LocaGuest.

### Concepts principaux

#### `ApplicationCode`

`ApplicationCode` représente l'identifiant stable d'une application.

```csharp
using Itech.Security.Contracts.Applications;

var application = new ApplicationCode("DriveOS");

Console.WriteLine(application.Value); // driveos
```

Le code est automatiquement :

- validé ;
- `Trim()` ;
- normalisé en minuscules ;
- limité à 100 caractères.

Conversions disponibles :

```csharp
ApplicationCode application = (ApplicationCode)"driveos";
string value = application;
```

#### `AuthorizationContext`

Un contexte d'autorisation associe une application à une organisation.

```csharp
using Itech.Security.Contracts.Applications;
using Itech.Security.Contracts.Authorization;

var context = new AuthorizationContext(
    new ApplicationCode("driveos"),
    organizationId);
```

Ce modèle est utile dans un environnement multi-application et multi-tenant : une permission doit être évaluée dans le bon produit et dans la bonne organisation.

#### `PermissionDefinition`

Une permission portable est décrite par :

```csharp
public sealed record PermissionDefinition(
    string ApplicationCode,
    string Code,
    string DisplayName,
    string Description,
    string Category);
```

Exemple :

```csharp
var permission = new PermissionDefinition(
    ApplicationCode: "driveos",
    Code: "Students.Read",
    DisplayName: "Read Students",
    Description: "Allows read access to Students.",
    Category: "Students");
```

#### Générer automatiquement un catalogue de permissions

`PermissionCatalogFactory` permet de transformer une liste de codes stables en métadonnées affichables par un back-office tel qu'AccessManager.

```csharp
using Itech.Security.Contracts.Authorization;

string[] codes =
[
    "Students.Read",
    "Students.Create",
    "Students.Update"
];

IReadOnlyList<PermissionDefinition> catalog =
    PermissionCatalogFactory.Create("driveos", codes);
```

Pour `Students.Read`, le catalogue produit notamment :

- application : `driveos` ;
- code : `Students.Read` ;
- nom lisible : `Read Students` ;
- catégorie : `Students`.

Cela permet à l'application propriétaire de ne maintenir que les **codes fonctionnels stables**, tout en laissant la couche IAM générer une présentation cohérente.

### Définir des rôles

`RoleDefinition` associe un code de rôle, une application et une portée.

```csharp
using Itech.Security.Contracts.Applications;
using Itech.Security.Contracts.Authorization;

var role = new RoleDefinition(
    applicationCode: new ApplicationCode("driveos"),
    code: "DriveOS.BranchManager",
    scope: RoleScope.Organization,
    description: "Manages a driving-school branch.");
```

Portées disponibles :

```csharp
RoleScope.Organization
RoleScope.Application
RoleScope.Platform
```

Le package fournit également le rôle plateforme :

```csharp
using Itech.Security.Contracts.Platform;

string roleCode = PlatformRoleCodes.SuperAdministrator;
// platform.super-administrator
```

### Créer le contrat de sécurité d'une nouvelle application

Une application consommatrice devrait conserver ses codes dans son propre package de contrats.

```csharp
using Itech.Security.Contracts.Applications;
using Itech.Security.Contracts.Authorization;

public static class MyApplication
{
    public const string Code = "myapp";
    public static ApplicationCode ApplicationCode { get; } = new(Code);
}

public static class MyPermissionCodes
{
    public const string CustomersRead = "Customers.Read";
    public const string CustomersCreate = "Customers.Create";

    public static readonly string[] All =
    [
        CustomersRead,
        CustomersCreate
    ];
}

public static class MyPermissionCatalog
{
    public static IReadOnlyList<PermissionDefinition> All { get; } =
        PermissionCatalogFactory.Create(
            MyApplication.Code,
            MyPermissionCodes.All);
}
```

Le service IAM peut ensuite découvrir ou synchroniser ce catalogue sans dépendre du domaine métier de l'application.

### Exemple d'exposition depuis une API

```csharp
app.MapGet("/api/security/permissions", () =>
{
    return MyPermissionCatalog.All;
});
```

Réponse typique :

```json
[
  {
    "applicationCode": "myapp",
    "code": "Customers.Create",
    "displayName": "Create Customers",
    "description": "Allows create access to Customers.",
    "category": "Customers"
  },
  {
    "applicationCode": "myapp",
    "code": "Customers.Read",
    "displayName": "Read Customers",
    "description": "Allows read access to Customers.",
    "category": "Customers"
  }
]
```

### Bonnes pratiques

#### Les codes de permissions sont des contrats publics

Une permission déjà publiée ne doit pas être renommée silencieusement.

Préférez :

```text
Students.Read
Students.Create
Students.Update
Students.Archive
```

Évitez de réutiliser un ancien code pour une autre action métier.

#### Séparer contrat d'autorisation et logique métier

Ce package contient des **contrats**. Il ne doit pas contenir :

- des appels à une base de données ;
- des handlers CQRS ;
- des règles propres à DriveOS ;
- des règles propres à LocaGuest ;
- des contrôleurs ASP.NET Core.

#### Toujours conserver le contexte organisationnel

Dans une architecture multi-tenant, connaître uniquement le `UserId` ne suffit pas. L'autorisation doit être évaluée dans le contexte de l'organisation concernée.

### Packages associés

- `DriveOS.Security.Contracts` : catalogue de permissions et rôles DriveOS.
- `LocaGuest.Security.Contracts` : catalogue de permissions, rôles et policies LocaGuest.
- `Itech.Application.Contracts` : pagination, tri et erreurs applicatives partagées.
- `Itech.Querying` : construction d'options de requêtes dynamiques.

### Compatibilité

Le package est conçu comme une bibliothèque de contrats légère et réutilisable. Les applications consommatrices restent responsables de l'implémentation effective de l'authentification, du stockage des rôles et de l'évaluation des permissions.

---

## Itech.Application.Contracts

**Contrats applicatifs génériques.**

`Itech.Application.Contracts` regroupe les contrats applicatifs génériques réutilisés par les APIs Itech, en particulier la **pagination**, le **tri** et la représentation d'**erreurs métier localisables**.

Le package ne contient aucune dépendance vers DriveOS ou LocaGuest et peut être utilisé dans n'importe quelle application .NET.

### Installation

```bash
dotnet add package Itech.Application.Contracts
```

### Pagination

#### `PageRequest`

`PageRequest` centralise les règles de pagination.

```csharp
using Itech.Application.Contracts.Pagination;

var page = new PageRequest(page: 2, pageSize: 25);

Console.WriteLine(page.Page);     // 2
Console.WriteLine(page.PageSize); // 25
Console.WriteLine(page.Skip);     // 25
```

Valeurs communes :

```csharp
PaginationParameters.DefaultPage      // 1
PaginationParameters.DefaultPageSize  // 20
PaginationParameters.MaximumPageSize  // 100
```

Les valeurs invalides sont rejetées immédiatement :

```csharp
new PageRequest(0, 20);   // ArgumentOutOfRangeException
new PageRequest(1, 101);  // ArgumentOutOfRangeException
```

#### Exemple avec Entity Framework Core

```csharp
var request = new PageRequest(page, pageSize);

var query = dbContext.Students
    .AsNoTracking()
    .OrderBy(x => x.LastName);

var totalCount = await query.LongCountAsync(cancellationToken);

var items = await query
    .Skip(request.Skip)
    .Take(request.PageSize)
    .Select(x => new StudentListItem(
        x.Id,
        x.FirstName,
        x.LastName))
    .ToListAsync(cancellationToken);

var result = new PagedResult<StudentListItem>(
    items,
    request.Page,
    request.PageSize,
    totalCount);
```

### `PagedResult<T>`

`PagedResult<T>` encapsule le résultat paginé et calcule les métadonnées de navigation.

```csharp
var result = new PagedResult<string>(
    ["A", "B", "C"],
    page: 2,
    pageSize: 3,
    totalCount: 10);

Console.WriteLine(result.TotalPages);     // 4
Console.WriteLine(result.HasPreviousPage); // true
Console.WriteLine(result.HasNextPage);     // true
```

Structure exposée :

```text
Items
Page
PageSize
TotalCount
TotalPages
HasPreviousPage
HasNextPage
```

#### Exemple de réponse HTTP

```csharp
app.MapGet("/api/students", async (
    int page,
    int pageSize,
    StudentsDbContext db,
    CancellationToken ct) =>
{
    var request = new PageRequest(page, pageSize);

    var query = db.Students.AsNoTracking();
    var count = await query.LongCountAsync(ct);

    var items = await query
        .OrderBy(x => x.LastName)
        .Skip(request.Skip)
        .Take(request.PageSize)
        .Select(x => new { x.Id, x.FirstName, x.LastName })
        .ToListAsync(ct);

    return Results.Ok(new PagedResult<object>(
        items.Cast<object>().ToArray(),
        request.Page,
        request.PageSize,
        count));
});
```

### Tri

Le package fournit un enum stable :

```csharp
using Itech.Application.Contracts.Sorting;

SortDirection direction = SortDirection.Descending;
```

Valeurs :

```text
Ascending
Descending
```

Il peut être utilisé dans les contrats de requête sans imposer une technologie particulière de persistence.

```csharp
public sealed record GetStudentsRequest(
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    SortDirection SortDirection = SortDirection.Ascending);
```

### Erreurs métier localisables

#### `ErrorDescriptor`

`ErrorDescriptor` représente une erreur applicative par une **clé stable** et des paramètres optionnels.

```csharp
using Itech.Application.Contracts.Errors;

var error = new ErrorDescriptor("students.not_found");
```

Avec paramètres :

```csharp
var error = new ErrorDescriptor(
    "students.age.minimum",
    new Dictionary<string, object?>
    {
        ["minimumAge"] = 17,
        ["actualAge"] = 16
    });
```

L'objectif est d'éviter d'utiliser un message backend comme contrat public.

À éviter :

```json
{
  "message": "The student must be at least 17 years old."
}
```

À préférer :

```json
{
  "key": "students.age.minimum",
  "parameters": {
    "minimumAge": 17,
    "actualAge": 16
  }
}
```

Le frontend peut alors traduire l'erreur :

```json
{
  "students.age.minimum": "L'élève doit avoir au moins {{minimumAge}} ans."
}
```

ou en anglais :

```json
{
  "students.age.minimum": "The student must be at least {{minimumAge}} years old."
}
```

### Exemple dans un handler CQRS

```csharp
public async Task<Result<StudentResponse>> Handle(
    GetStudentQuery query,
    CancellationToken cancellationToken)
{
    var student = await repository.GetByIdAsync(
        query.StudentId,
        cancellationToken);

    if (student is null)
    {
        return Result.Failure<StudentResponse>(
            new ErrorDescriptor(
                "students.not_found",
                new Dictionary<string, object?>
                {
                    ["studentId"] = query.StudentId
                }));
    }

    return mapper.Map<StudentResponse>(student);
}
```

L'adaptation exacte à votre type `Result` dépend de votre couche applicative ; `ErrorDescriptor` reste volontairement indépendant de cette implémentation.

### Exemple de contrat de recherche complet

```csharp
using Itech.Application.Contracts.Pagination;
using Itech.Application.Contracts.Sorting;

public sealed record SearchStudentsRequest(
    string? Search,
    int Page = PaginationParameters.DefaultPage,
    int PageSize = PaginationParameters.DefaultPageSize,
    string? SortBy = null,
    SortDirection SortDirection = SortDirection.Ascending)
{
    public PageRequest ToPageRequest() => new(Page, PageSize);
}
```

### Bonnes pratiques

- Utiliser `PageRequest` pour appliquer les mêmes limites dans toutes les APIs.
- Faire les lectures EF Core avec `AsNoTracking()` lorsque les agrégats ne sont pas modifiés.
- Exposer des clés d'erreur stables et traduisibles au frontend.
- Ne pas mettre de texte localisé dans les contrats partagés.
- Ne pas réinventer un autre `PagedResult<T>` dans chaque bounded context.
- Garder les contrats indépendants d'ASP.NET Core, EF Core et du domaine métier.

### Packages associés

- `Itech.Querying` complète ces contrats avec la construction de filtres et tris dynamiques.
- `Itech.Security.Contracts` fournit les contrats d'autorisation multi-application.

---

## Itech.Querying

**Construction de requêtes dynamiques.**

`Itech.Querying` fournit des helpers réutilisables pour construire des `DynamicQueryOptions` à partir de requêtes applicatives.

Le package s'appuie sur `DomainRelay.Mapping.Expressions.Dynamic` et permet de centraliser la création de filtres et de tris sans dupliquer la même logique dans chaque handler ou service de lecture.

### Installation

```bash
dotnet add package Itech.Querying
```

Le package utilise également :

```text
DomainRelay.Mapping.Expressions
```

### Objectif

Sans helper commun, un endpoint de recherche finit souvent par accumuler du code conditionnel :

```csharp
if (!string.IsNullOrWhiteSpace(request.Search))
{
    options.Filters.Add(...);
}

if (request.Status is not null)
{
    options.Filters.Add(...);
}
```

`Itech.Querying` réduit ce bruit et applique un comportement cohérent : les valeurs `null` ou les chaînes vides sont ignorées automatiquement.

### Créer des options dynamiques

```csharp
using DomainRelay.Mapping.Expressions.Dynamic;
using Itech.Querying;

var options = new DynamicQueryOptions();

options
    .AddContains("LastName", "Martin")
    .AddEquals("Status", "Active")
    .AddSortOrDefault(
        sortBy: null,
        sortDirection: null,
        defaultMemberName: "CreatedAt");
```

### Filtres disponibles

#### Égalité

```csharp
options.AddEquals("Status", request.Status);
```

Produit un filtre utilisant :

```text
DynamicFilterOperator.Equals
```

Une valeur `null`, ou une chaîne vide/blanche, est ignorée.

#### Différence

```csharp
options.AddNotEquals("Status", request.ExcludedStatus);
```

#### Contient

```csharp
options.AddContains("LastName", request.Search);
```

Le helper utilise :

```text
DynamicFilterOperator.StringContains
```

Une chaîne `null`, vide ou blanche n'ajoute aucun filtre.

#### Supérieur ou égal

```csharp
options.AddGreaterThanOrEqual("CreatedAt", request.CreatedFrom);
```

#### Inférieur ou égal

```csharp
options.AddLessThanOrEqual("CreatedAt", request.CreatedTo);
```

### Tri avec valeur par défaut

```csharp
options.AddSortOrDefault(
    sortBy: request.SortBy,
    sortDirection: request.SortDirection,
    defaultMemberName: "CreatedAt",
    defaultDirection: DynamicSortDirection.Desc);
```

Comportement :

- si `SortBy` n'est pas fourni, le membre par défaut est utilisé ;
- dans ce cas, `defaultDirection` est appliqué ;
- si `SortBy` est fourni et `SortDirection == "desc"`, le tri est descendant ;
- toute autre valeur de direction produit un tri ascendant.

Exemples :

```csharp
options.AddSortOrDefault(null, null, "CreatedAt");
// CreatedAt DESC

options.AddSortOrDefault("LastName", "asc", "CreatedAt");
// LastName ASC

options.AddSortOrDefault("LastName", "desc", "CreatedAt");
// LastName DESC
```

### Créer une factory par requête

Pour éviter que le handler connaisse les détails des membres filtrables, utilisez `IDynamicQueryOptionsFactory<TRequest>`.

```csharp
using DomainRelay.Mapping.Expressions.Dynamic;
using Itech.Querying;

public sealed record SearchStudentsRequest(
    string? Search,
    string? Status,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo,
    string? SortBy,
    string? SortDirection);

public sealed class SearchStudentsQueryOptionsFactory
    : IDynamicQueryOptionsFactory<SearchStudentsRequest>
{
    public DynamicQueryOptions Create(SearchStudentsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var options = new DynamicQueryOptions();

        options
            .AddContains("LastName", request.Search)
            .AddEquals("Status", request.Status)
            .AddGreaterThanOrEqual("CreatedAt", request.CreatedFrom)
            .AddLessThanOrEqual("CreatedAt", request.CreatedTo)
            .AddSortOrDefault(
                request.SortBy,
                request.SortDirection,
                defaultMemberName: "CreatedAt",
                defaultDirection: DynamicSortDirection.Desc);

        return options;
    }
}
```

### Utilisation depuis un handler

```csharp
public sealed class SearchStudentsQueryHandler
{
    private readonly IDynamicQueryOptionsFactory<SearchStudentsRequest> _optionsFactory;
    private readonly IStudentReadService _readService;

    public SearchStudentsQueryHandler(
        IDynamicQueryOptionsFactory<SearchStudentsRequest> optionsFactory,
        IStudentReadService readService)
    {
        _optionsFactory = optionsFactory;
        _readService = readService;
    }

    public async Task<IReadOnlyList<StudentResponse>> Handle(
        SearchStudentsRequest request,
        CancellationToken cancellationToken)
    {
        var options = _optionsFactory.Create(request);

        return await _readService.SearchAsync(
            options,
            cancellationToken);
    }
}
```

La manière d'appliquer `DynamicQueryOptions` à une projection ou une requête dépend de l'intégration DomainRelay utilisée par votre application.

### Enregistrement DI

Une factory peut être enregistrée dans le conteneur standard .NET :

```csharp
services.AddScoped<
    IDynamicQueryOptionsFactory<SearchStudentsRequest>,
    SearchStudentsQueryOptionsFactory>();
```

### Sécuriser les champs de tri

Ne transmettez pas directement un nom de propriété fourni par le client sans validation si votre couche dynamique accepte des chemins de membres arbitraires.

Préférez une whitelist :

```csharp
private static string ResolveSortMember(string? sortBy) =>
    sortBy?.Trim().ToLowerInvariant() switch
    {
        "name" => "LastName",
        "createdat" => "CreatedAt",
        "status" => "Status",
        _ => "CreatedAt"
    };
```

Puis :

```csharp
options.AddSortOrDefault(
    ResolveSortMember(request.SortBy),
    request.SortDirection,
    "CreatedAt");
```

### Bonnes pratiques

- Construire les options dans une factory dédiée plutôt que dans le contrôleur.
- N'ajouter un filtre que lorsqu'une valeur est réellement fournie.
- Utiliser un tri par défaut stable pour éviter des résultats non déterministes.
- Valider/mapper les champs de tri venant du frontend.
- Garder les requêtes de lecture `AsNoTracking()` lorsque l'agrégat n'a pas besoin d'être modifié.
- Ne pas utiliser des noms de propriétés internes comme contrat HTTP si une API publique stable peut être définie.

### Packages associés

- `Itech.Application.Contracts` pour la pagination, le tri contractuel et les erreurs applicatives.
- `DomainRelay.Mapping.Expressions` pour le moteur d'expressions dynamiques utilisé par `DynamicQueryOptions`.

---

## DriveOS.Security.Contracts

**Contrats d’autorisation DriveOS.**

`DriveOS.Security.Contracts` est le **contrat d'autorisation public de DriveOS**.

Il contient les codes de permissions, les rôles intégrés, le catalogue de métadonnées et la matrice de permissions par défaut utilisés par DriveOS, AuthGate et les outils d'administration tels qu'AccessManager.

> Les codes publiés par ce package sont des contrats stables. Une permission existante ne doit pas être renommée ou réutilisée pour une autre action métier après publication.

### Installation

```bash
dotnet add package DriveOS.Security.Contracts
```

Le package dépend de :

```text
Itech.Security.Contracts
```

### Identité de l'application

```csharp
using DriveOS.Security.Contracts;

Console.WriteLine(DriveOsApplication.Code); // driveos

var applicationCode = DriveOsApplication.ApplicationCode;
```

`DriveOsApplication.ApplicationCode` est un `Itech.Security.Contracts.Applications.ApplicationCode`.

### Utiliser une permission dans une API

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

### Vérification dans du code applicatif

Votre infrastructure d'autorisation peut travailler exclusivement avec le code stable :

```csharp
var permission = DriveOsPermissionCodes.CrmLeads.Create;

if (!currentUser.HasPermission(permission))
{
    return Results.Forbid();
}
```

`HasPermission` est volontairement illustratif : l'évaluation concrète est fournie par votre couche IAM/AuthGate, pas par ce package.

### Catalogue complet pour AuthGate / AccessManager

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

### Exemple de synchronisation côté IAM

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

### Rôles DriveOS

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

### Matrice de permissions par défaut

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

#### Important : matrice bootstrap, pas source de vérité runtime

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

### Ajouter une nouvelle permission

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

### Ne jamais coder les permissions en dur

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

### Frontend

Le frontend ne doit pas inventer ses propres chaînes de permissions. Les permissions de l'utilisateur doivent être retournées par le backend/AuthGate et comparées aux codes issus du contrat de sécurité exposé côté API.

Exemple conceptuel :

```typescript
if (auth.hasPermission('CrmLeads.Read')) {
  // afficher l'onglet Prospects
}
```

Pour réduire les chaînes dupliquées, une application frontend peut générer ses constantes depuis le catalogue ou les maintenir dans un module synchronisé avec ce package.

### Versionnement

Lorsqu'une permission est publiée :

1. ne pas changer sa signification ;
2. ne pas la renommer silencieusement ;
3. ajouter une nouvelle permission lorsqu'une nouvelle capacité apparaît ;
4. conserver une migration IAM lorsqu'une ancienne permission doit être retirée ;
5. publier une nouvelle version du package avant de déployer les consommateurs.

### Packages associés

- `Itech.Security.Contracts` : primitives d'autorisation génériques.
- `Itech.Application.Contracts` : contrats applicatifs génériques.
- `Itech.Querying` : helpers de requêtes dynamiques.

---

## LocaGuest.Security.Contracts

**Contrats d’autorisation LocaGuest.**

`LocaGuest.Security.Contracts` centralise le **contrat d'autorisation de LocaGuest** : application, permissions, policies, rôles et permissions par défaut.

Le package permet à LocaGuest, AuthGate, AccessManager et aux outils d'administration de partager les mêmes codes sans dupliquer des chaînes dans plusieurs projets.

### Installation

```bash
dotnet add package LocaGuest.Security.Contracts
```

Le package dépend de :

```text
Itech.Security.Contracts
```

### Application LocaGuest

```csharp
using LocaGuest.Security.Contracts;

Console.WriteLine(LocaGuestApplication.Code); // locaguest

var applicationCode = LocaGuestApplication.ApplicationCode;
```

### Permissions

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

### Catalogue pour AuthGate / AccessManager

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

### Rôles intégrés

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

### Permissions par défaut d'un rôle

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

### Policies ASP.NET Core

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

### Ajouter une permission

Ajoutez une constante stable :

```csharp
public const string InspectionsRead = "inspections.read";
public const string InspectionsWrite = "inspections.write";
```

Puis ajoutez-la au tableau global du catalogue et aux rôles bootstrap appropriés.

Une nouvelle version du package permettra alors aux consommateurs de découvrir la nouvelle permission.

### Exemple de contrôle manuel

```csharp
var requiredPermission = LocaGuestPermissionCodes.ContractsTerminate;

if (!currentUser.HasPermission(requiredPermission))
{
    return Results.Forbid();
}
```

`HasPermission` représente ici votre implémentation d'autorisation runtime.

### Bonnes pratiques

- Ne jamais recopier une chaîne de permission dans plusieurs services.
- Ajouter une permission plutôt que changer la sémantique d'une permission publiée.
- Conserver AuthGate comme source de vérité runtime après le seeding initial.
- Distinguer les rôles bootstrap des rôles personnalisés d'un tenant.
- Tester que chaque rôle intégré ne référence que des permissions existantes.
- Versionner le package avant de déployer une API utilisant une nouvelle permission.

### Packages associés

- `Itech.Security.Contracts` : primitives génériques utilisées par ce package.
- `Itech.Application.Contracts` : pagination et erreurs applicatives partagées.
- `Itech.Querying` : construction de filtres et tris dynamiques.

---

## Intégration recommandée dans une architecture Itech

Une architecture typique peut répartir les responsabilités ainsi :

```text
Itech.Security.Contracts
        ↑
        ├── DriveOS.Security.Contracts ──→ DriveOS / AuthGate / AccessManager
        └── LocaGuest.Security.Contracts ─→ LocaGuest / AuthGate / AccessManager

Itech.Application.Contracts ─────────────→ APIs / Application layer / Front contracts
Itech.Querying ──────────────────────────→ Read services / Query handlers
```

Les applications métier doivent éviter de recopier localement les chaînes de permissions. Le package de contrats correspondant reste la source de vérité.

## Compatibilité et versionnement

Une modification est potentiellement **breaking** lorsqu’elle renomme ou supprime une permission, un rôle, un code d’application ou modifie la sémantique d’un contrat public. L’ajout d’une nouvelle permission est généralement additif, mais nécessite la synchronisation du catalogue côté système d’identité/administration.

Lors d’une montée de version :

1. mettre à jour le package concerné ;
2. reconstruire l’application consommatrice ;
3. synchroniser les catalogues de permissions si nécessaire ;
4. vérifier les rôles par défaut ;
5. exécuter les tests d’autorisation et de non-régression.

## Publication NuGet

Ce repository utilise **un README commun**. Chaque package NuGet embarque ce fichier sous le nom `README.md`. Il ne faut pas ajouter un second README au même `PackagePath`, sinon NuGet.org rejette le `.nupkg` pour fichiers dupliqués.

Configuration attendue dans chaque projet packable :

```xml
<PropertyGroup>
  <PackageReadmeFile>README.md</PackageReadmeFile>
</PropertyGroup>

<ItemGroup>
  <None Include="../../README.md"
        Pack="true"
        PackagePath=""
        Link="README.md" />
</ItemGroup>
```

## Contribution

Avant de modifier un contrat public, vérifiez ses consommateurs. Les catalogues de permissions doivent rester déterministes, sans doublons de codes, et les changements incompatibles doivent être versionnés explicitement.

## Licence

Voir les métadonnées de chaque package NuGet et la licence du repository.
