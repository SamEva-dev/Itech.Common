# Itech.Security.Contracts

`Itech.Security.Contracts` fournit les contrats d'autorisation communs aux applications Itech.

Le package est volontairement **indépendant de DriveOS et de LocaGuest**. Il expose les types fondamentaux utilisés pour représenter une application, un contexte d'autorisation, des permissions et des rôles sans dépendre d'une API métier particulière.

## Installation

```bash
dotnet add package Itech.Security.Contracts
```

Ou dans un fichier projet :

```xml
<ItemGroup>
  <PackageReference Include="Itech.Security.Contracts" Version="x.y.z" />
</ItemGroup>
```

## Quand utiliser ce package ?

Utilisez `Itech.Security.Contracts` lorsque vous développez :

- une API qui doit identifier l'application courante ;
- un service d'identité ou d'autorisation ;
- un catalogue de permissions propre à une application ;
- un système de rôles multi-tenant ;
- une intégration avec AuthGate ou AccessManager ;
- une bibliothèque applicative qui ne doit pas dépendre de DriveOS ou LocaGuest.

## Concepts principaux

### `ApplicationCode`

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

### `AuthorizationContext`

Un contexte d'autorisation associe une application à une organisation.

```csharp
using Itech.Security.Contracts.Applications;
using Itech.Security.Contracts.Authorization;

var context = new AuthorizationContext(
    new ApplicationCode("driveos"),
    organizationId);
```

Ce modèle est utile dans un environnement multi-application et multi-tenant : une permission doit être évaluée dans le bon produit et dans la bonne organisation.

### `PermissionDefinition`

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

### Générer automatiquement un catalogue de permissions

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

## Définir des rôles

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

## Créer le contrat de sécurité d'une nouvelle application

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

## Exemple d'exposition depuis une API

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

## Bonnes pratiques

### Les codes de permissions sont des contrats publics

Une permission déjà publiée ne doit pas être renommée silencieusement.

Préférez :

```text
Students.Read
Students.Create
Students.Update
Students.Archive
```

Évitez de réutiliser un ancien code pour une autre action métier.

### Séparer contrat d'autorisation et logique métier

Ce package contient des **contrats**. Il ne doit pas contenir :

- des appels à une base de données ;
- des handlers CQRS ;
- des règles propres à DriveOS ;
- des règles propres à LocaGuest ;
- des contrôleurs ASP.NET Core.

### Toujours conserver le contexte organisationnel

Dans une architecture multi-tenant, connaître uniquement le `UserId` ne suffit pas. L'autorisation doit être évaluée dans le contexte de l'organisation concernée.

## Packages associés

- `DriveOS.Security.Contracts` : catalogue de permissions et rôles DriveOS.
- `LocaGuest.Security.Contracts` : catalogue de permissions, rôles et policies LocaGuest.
- `Itech.Application.Contracts` : pagination, tri et erreurs applicatives partagées.
- `Itech.Querying` : construction d'options de requêtes dynamiques.

## Compatibilité

Le package est conçu comme une bibliothèque de contrats légère et réutilisable. Les applications consommatrices restent responsables de l'implémentation effective de l'authentification, du stockage des rôles et de l'évaluation des permissions.
