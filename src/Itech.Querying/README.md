# Itech.Querying

`Itech.Querying` fournit des helpers réutilisables pour construire des `DynamicQueryOptions` à partir de requêtes applicatives.

Le package s'appuie sur `DomainRelay.Mapping.Expressions.Dynamic` et permet de centraliser la création de filtres et de tris sans dupliquer la même logique dans chaque handler ou service de lecture.

## Installation

```bash
dotnet add package Itech.Querying
```

Le package utilise également :

```text
DomainRelay.Mapping.Expressions
```

## Objectif

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

## Créer des options dynamiques

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

## Filtres disponibles

### Égalité

```csharp
options.AddEquals("Status", request.Status);
```

Produit un filtre utilisant :

```text
DynamicFilterOperator.Equals
```

Une valeur `null`, ou une chaîne vide/blanche, est ignorée.

### Différence

```csharp
options.AddNotEquals("Status", request.ExcludedStatus);
```

### Contient

```csharp
options.AddContains("LastName", request.Search);
```

Le helper utilise :

```text
DynamicFilterOperator.StringContains
```

Une chaîne `null`, vide ou blanche n'ajoute aucun filtre.

### Supérieur ou égal

```csharp
options.AddGreaterThanOrEqual("CreatedAt", request.CreatedFrom);
```

### Inférieur ou égal

```csharp
options.AddLessThanOrEqual("CreatedAt", request.CreatedTo);
```

## Tri avec valeur par défaut

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

## Créer une factory par requête

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

## Utilisation depuis un handler

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

## Enregistrement DI

Une factory peut être enregistrée dans le conteneur standard .NET :

```csharp
services.AddScoped<
    IDynamicQueryOptionsFactory<SearchStudentsRequest>,
    SearchStudentsQueryOptionsFactory>();
```

## Sécuriser les champs de tri

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

## Bonnes pratiques

- Construire les options dans une factory dédiée plutôt que dans le contrôleur.
- N'ajouter un filtre que lorsqu'une valeur est réellement fournie.
- Utiliser un tri par défaut stable pour éviter des résultats non déterministes.
- Valider/mapper les champs de tri venant du frontend.
- Garder les requêtes de lecture `AsNoTracking()` lorsque l'agrégat n'a pas besoin d'être modifié.
- Ne pas utiliser des noms de propriétés internes comme contrat HTTP si une API publique stable peut être définie.

## Packages associés

- `Itech.Application.Contracts` pour la pagination, le tri contractuel et les erreurs applicatives.
- `DomainRelay.Mapping.Expressions` pour le moteur d'expressions dynamiques utilisé par `DynamicQueryOptions`.
