# CoreOne.OQuery.Web

ASP.NET Core integration for [CoreOne.OQuery](https://www.nuget.org/packages/CoreOne.OQuery). Automatically binds OQuery expressions from the request query string directly to controller action parameters — no manual parsing needed.

## Install

```bash
dotnet add package CoreOne.OQuery.Web
```

## Setup

Register the model binder in `Program.cs`:

```csharp
builder.Services
    .AddControllers()
    .AddOQuery();          // <-- one line
```

## Usage

Add a `QueryNode` parameter to any action. It is automatically populated from the `query` query string parameter:

```csharp
using CoreOne.OQuery.Expressions;
using CoreOne.OQuery.Extensions;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TicketsController(AppDbContext db) => _db = db;

    [HttpGet]
    public IActionResult Get(QueryNode? query)
    {
        var results = _db.Tickets.Apply(query).ToList();
        return Ok(results);
    }
}
```

**Example requests:**

```
GET /api/tickets?query=status = "open" AND priority >= 3 LIMIT 20
GET /api/tickets?query=contains(assignee, "alice") SELECT assignee, priority PAGE 1 PAGESIZE 10
GET /api/tickets                              ← query is null, returns all records
```

## Configuration

By default the binder reads from the `query` query string key. Change it via `OQueryOptions`:

```csharp
builder.Services
    .AddControllers()
    .AddOQuery(options =>
    {
        options.QueryParameterName = "filter";   // now reads from ?filter=...
    });
```

## Query Syntax Reference

| Syntax | Example |
|--------|---------|
| Equality | `status = "open"` |
| Comparison | `priority >= 3` |
| IN list | `label IN ("bug", "urgent")` |
| AND / OR / NOT | `status = "open" AND NOT priority < 2` |
| Nested property | `user.address.city = "Berlin"` |
| `contains()` | `contains(assignee, "ali")` |
| `startsWith()` | `startsWith(status, "op")` |
| `endsWith()` | `endsWith(label, "ent")` |
| SELECT projection | `status = "open" SELECT assignee, priority` |
| Pagination | `LIMIT 20 OFFSET 0` or `PAGE 2 PAGESIZE 25` |

> See [CoreOne.OQuery](https://www.nuget.org/packages/CoreOne.OQuery) for the full query engine documentation.

## Links

- [GitHub](https://github.com/jlopez788/CoreOne.OQuery)
