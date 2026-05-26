# CoreOne.OQuery

A lightweight, extensible query language engine for .NET. Write human-readable filter expressions and evaluate them against any `IQueryable<T>`.

| Package | Version | Description |
|---------|---------|-------------|
| [CoreOne.OQuery](https://www.nuget.org/packages/CoreOne.OQuery) | 1.0.0 | Core query engine — parsing, AST, LINQ evaluation |
| [CoreOne.OQuery.AspNetCore](https://www.nuget.org/packages/CoreOne.OQuery.AspNetCore) | 1.0.0 | ASP.NET Core model binder integration |

## How It Works

```
Query string  →  Lexer  →  Tokens  →  Parser  →  AST (QueryNode)  →  LINQ predicate  →  IQueryable<T>
```

OQuery expressions are plain text — easy to send from a frontend, store in a database, or build programmatically.

## Quick Example

```csharp
// Core library
var tokens = new Lexer("status = \"open\" AND priority >= 3 LIMIT 20").Tokenize();
var query  = new Parser(tokens).Parse();
var result = dbContext.Tickets.Apply(query).ToList();
```

```csharp
// ASP.NET Core — controller receives a pre-parsed QueryNode automatically
[HttpGet]
public IActionResult Get(QueryNode? query) => Ok(_db.Tickets.Apply(query).ToList());
// GET /api/tickets?query=status = "open" LIMIT 20
```

## Supported Query Features

- **Comparison operators** — `=`, `!=`, `>`, `>=`, `<`, `<=`
- **IN lists** — `label IN ("bug", "urgent")`
- **Logical operators** — `AND`, `OR`, `NOT`, grouping with `()`
- **Nested property paths** — `user.address.city = "Berlin"`
- **Built-in string functions** — `contains()`, `startsWith()`, `endsWith()`
- **Field projection** — `SELECT assignee, priority`
- **Pagination** — `LIMIT 20 OFFSET 40` or `PAGE 2 PAGESIZE 25`
- **Custom operators and functions** — implement `IOperatorProvider` / `IFunctionProvider`

## Documentation

- [CoreOne.OQuery README](src/CoreOne.OQuery/README.md) — full API reference, extension methods, extensibility
- [CoreOne.OQuery.AspNetCore README](src/CoreOne.OQuery.AspNetCore/README.md) — setup, controller usage, options

## License

MIT