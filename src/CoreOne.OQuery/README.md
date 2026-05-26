# CoreOne.OQuery

A lightweight query language engine for .NET. Write human-readable filter expressions and evaluate them against any `IQueryable<T>` — no configuration required.

## Install

```bash
dotnet add package CoreOne.OQuery
```

## Quick Start

```csharp
using CoreOne.OQuery.Extensions;
using CoreOne.OQuery.Lexer;
using CoreOne.OQuery.Parser;

// 1. Parse a query string into an AST
var tokens = new Lexer("status = \"open\" AND priority >= 3 LIMIT 20").Tokenize();
var query  = new Parser(tokens).Parse();

// 2. Apply it to any IQueryable<T>
var results = dbContext.Tickets.Apply(query).ToList();

// 3. Or get an Expression<Func<T, bool>> for manual use
Expression<Func<Ticket, bool>> predicate = dbContext.Tickets.ToPredicate<Ticket>(query);

// 4. Project to a dictionary (dynamic field selection)
IEnumerable<Dictionary<string, object?>> rows = dbContext.Tickets.Project(query).ToList();
```

## Query Syntax

### Filtering

| Syntax | Example |
|--------|---------|
| Equality | `status = "open"` |
| Inequality | `status != "closed"` |
| Comparison | `priority >= 3` |
| IN list | `label IN ("bug", "urgent")` |
| Logical AND | `status = "open" AND priority > 2` |
| Logical OR | `priority > 4 OR label = "urgent"` |
| Logical NOT | `NOT status = "closed"` |
| Grouping | `(status = "open" OR priority > 3) AND assignee = "alice"` |
| Nested property | `user.address.city = "Berlin"` |

### Built-in Functions

| Function | Description | Example |
|----------|-------------|---------|
| `contains(prop, value)` | String contains | `contains(assignee, "ali")` |
| `startsWith(prop, value)` | String starts with | `startsWith(status, "op")` |
| `endsWith(prop, value)` | String ends with | `endsWith(label, "ent")` |

### Field Projection (SELECT)

```
status = "open" SELECT assignee, priority
```

Returns only the specified fields as `Dictionary<string, object?>` rows via `Project<T>()`.

### Pagination

| Syntax | Description |
|--------|-------------|
| `LIMIT 20 OFFSET 40` | Skip 40, take 20 |
| `PAGE 2 PAGESIZE 25` | Page-based (1-indexed) |

Pagination can be combined with filters:

```
status = "open" AND priority >= 3 LIMIT 10 OFFSET 0
```

## Extension Methods

All entry points live in `CoreOne.OQuery.Extensions`:

| Method | Returns | Description |
|--------|---------|-------------|
| `Apply(query)` | `IQueryable<T>` | Filter + paginate in one call |
| `ToPredicate<T>(query)` | `Expression<Func<T, bool>>` | Filter predicate only |
| `Project(query)` | `IEnumerable<Dictionary<string, object?>>` | Field projection |

## Extensibility

### Custom Operators

```csharp
public class MyOperators : IOperatorProvider
{
    public bool TryEvaluate(string op, object? left, object? right, out bool result)
    {
        if (op == "like") { result = left?.ToString()?.Contains(right?.ToString() ?? "") ?? false; return true; }
        result = false;
        return false;
    }
}
```

### Custom Functions

```csharp
public class MyFunctions : IFunctionProvider
{
    public bool TryInvoke(string name, object?[] args, out object? result)
    {
        if (name == "len") { result = args[0]?.ToString()?.Length ?? 0; return true; }
        result = null;
        return false;
    }
}
```

Register via `Providers.Register(new MyOperators(), new MyFunctions())`.

## Links

- [CoreOne.OQuery.Web](https://www.nuget.org/packages/CoreOne.OQuery.Web) — ASP.NET Core model binding integration
- [GitHub](https://github.com/jlopez788/CoreOne.OQuery)
