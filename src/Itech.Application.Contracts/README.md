# Itech.Application.Contracts

`Itech.Application.Contracts` regroupe les contrats applicatifs génériques réutilisés par les APIs Itech, en particulier la **pagination**, le **tri** et la représentation d'**erreurs métier localisables**.

Le package ne contient aucune dépendance vers DriveOS ou LocaGuest et peut être utilisé dans n'importe quelle application .NET.

## Installation

```bash
dotnet add package Itech.Application.Contracts
```

## Pagination

### `PageRequest`

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

### Exemple avec Entity Framework Core

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

## `PagedResult<T>`

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

### Exemple de réponse HTTP

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

## Tri

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

## Erreurs métier localisables

### `ErrorDescriptor`

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

## Exemple dans un handler CQRS

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

## Exemple de contrat de recherche complet

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

## Bonnes pratiques

- Utiliser `PageRequest` pour appliquer les mêmes limites dans toutes les APIs.
- Faire les lectures EF Core avec `AsNoTracking()` lorsque les agrégats ne sont pas modifiés.
- Exposer des clés d'erreur stables et traduisibles au frontend.
- Ne pas mettre de texte localisé dans les contrats partagés.
- Ne pas réinventer un autre `PagedResult<T>` dans chaque bounded context.
- Garder les contrats indépendants d'ASP.NET Core, EF Core et du domaine métier.

## Packages associés

- `Itech.Querying` complète ces contrats avec la construction de filtres et tris dynamiques.
- `Itech.Security.Contracts` fournit les contrats d'autorisation multi-application.
