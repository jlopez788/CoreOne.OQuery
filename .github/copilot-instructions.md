---
name: CoreOne.OQuery Coding Guidelines
description: Comprehensive coding style and architectural patterns for the CoreOne.OQuery project
---

# CoreOne.OQuery Copilot Instructions

## Project Overview

**CoreOne.OQuery** is a lightweight, extensible query language engine for .NET that parses human-readable filter expressions and evaluates them against any `IQueryable<T>`. 

- **Multi-targeted**: .NET 8.0, 9.0, 10.0
- **Core Architecture**: Lexer → Parser → AST (Visitor Pattern) → LINQ Expression Trees
- **Primary Components**:
  - `CoreOne.OQuery`: Core parsing, AST, and LINQ evaluation engine
  - `CoreOne.OQuery.AspNetCore`: ASP.NET Core model binder integration
  - `CoreOne.OQuery.Sample`: Reference implementation

---

## File Organization & Namespacing

### Namespace-to-Folder Mapping
- **Namespaces must match folder structure exactly**
  - Folder `Lexer/` → namespace `CoreOne.OQuery.Lexer`
  - Folder `Expressions/` → namespace `CoreOne.OQuery.Expressions`
  - Folder `Extensions/` → namespace `CoreOne.OQuery.Extensions`

### One Type Per File
- **Strict 1:1 ratio**: one file = one public type (class, interface, enum, struct)
- File name must match the type name: `BinaryExpression.cs`, `TokenType.cs`, `IAstVisitor.cs`

### File-Scoped Namespaces
```csharp
// ✅ Correct (C# 11 style)
namespace CoreOne.OQuery.Lexer;

public class Lexer { ... }

// ❌ Avoid
namespace CoreOne.OQuery.Lexer {
    public class Lexer { ... }
}
```

---

## Naming Conventions

### Types
- **Classes**: PascalCase → `BinaryExpression`, `QueryableEvaluator`, `OQueryModelBinder`
- **Interfaces**: I-prefix + PascalCase → `IAstVisitor`, `IFunctionProvider`, `IOperatorProvider`, `IModelBinder`
- **Enums**: PascalCase → `TokenType`, `ExpressionType`
- **Enum Members**: PascalCase → `LeftParen`, `RightParen`, `Identifier`, `String`, `Number`

### Members
- **Methods**: PascalCase, verb-first → `Visit()`, `Parse()`, `Apply()`, `BuildPredicate()`, `Tokenize()`
- **Properties**: PascalCase → `Left`, `Right`, `Operator`, `Position`, `Value`, `Type`, `Page`, `PageSize`
- **Private Fields**: _camelCase with underscore → `_input`, `_operatorProvider`, `_pos`, `_current`, `_functions`, `_parameter`
- **Local Variables**: camelCase → `current`, `tokens`, `expr`, `sb`, `paramName`
- **Parameters**: camelCase → `bindingContext`, `source`, `query`, `visitor`

---

## Class & Type Patterns

### Inheritance Strategy
- **Node** (abstract base class)
  - ↳ **Expression** (abstract)
    - ↳ BinaryExpression, UnaryExpression, LiteralExpression, FunctionCallExpression, IdentifierExpression
  - ↳ SelectNode, PaginationNode, QueryNode

### sealed Classes
**Use sealed for** implementation classes that should not be inherited:
```csharp
public sealed class QueryableEvaluator<T> : IAstVisitor<LinqExpr> { ... }
public sealed class OQueryModelBinder : IModelBinder { ... }
public sealed class OQueryModelBinderProvider : IModelBinderProvider { ... }
public sealed class OQueryOptions { ... }
```

**Don't seal** abstract base classes meant for inheritance (Node, Expression).

### Visitor Pattern
- All AST nodes inherit from `Node` with abstract `Accept<T>(IAstVisitor<T>)` method
- Implement `IAstVisitor<T>` with overloaded `Visit()` methods for each node type
- Use type-specific dispatch for polymorphic evaluation:
```csharp
public abstract T Accept<T>(IAstVisitor<T> visitor);

public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
```

### Interface Implementation
- Use **interface segregation**: small, focused contracts (IFunctionProvider, IOperatorProvider, IAstVisitor)
- Implement interfaces explicitly when needed for clarity
- Use null coalescing for optional dependencies: `operatorProvider ?? new DefaultOperatorProvider()`

---

## Constructor Patterns

### Primary Constructors (Preferred)
Use for classes with simple state capture:
```csharp
public sealed class OQueryModelBinder(IOperatorProvider? operatorProvider = null) : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        // operatorProvider is available as a parameter-captured field
        var evaluator = new QueryableEvaluator<object>(operatorProvider);
        ...
    }
}
```
**Benefits**: Fewer lines, automatic private readonly field creation, modern syntax.

### Traditional Constructors
Use for more complex initialization or when primary constructors aren't practical:
```csharp
public class Token
{
    public TokenType Type { get; set; }
    public string Value { get; set; }
    public int Position { get; set; }

    public Token(TokenType type, string value, int position)
    {
        Type = type;
        Value = value;
        Position = position;
    }
}
```

### Constructor Overloading
Provide defaults when sensible:
```csharp
public Parser(List<Token> tokens) : this(tokens, null) { }
public Parser(List<Token> tokens, IFunctionProvider? functionProvider = null)
{
    // Implementation
}
```

---

## Properties & Access Modifiers

### Auto-Properties
Use auto-properties everywhere; avoid backing fields except for lazy initialization or validation:
```csharp
public string Name { get; set; }
public int Age { get; set; }
public List<string> Tags { get; set; } = [];
```

### required Keyword
Mark properties that must be initialized:
```csharp
public required string Left { get; set; }
public required TokenType Type { get; set; }
public required object Value { get; set; }
```
**Benefit**: Compile-time enforcement that consumers initialize these properties.

### Nullable Reference Types
Enable globally; annotate all nullable members:
```csharp
public Expression? Left { get; set; }  // Nullable
public string Name { get; set; }       // Non-nullable
public int? Page { get; set; }         // Nullable value type
```

### Default Initialization
- **Collection initializers** (C# 12): `List<string> Fields { get; set; } = [];`
- **Classic syntax**: `List<Expression> Arguments { get; set; } = new List<Expression>();`
- Prefer `[]` for consistency where applicable

### Access Modifiers
- **public**: Standard for properties and public methods
- **private**: Helper methods, internal state fields
- **static**: Utility methods, shared constants/registries
- **readonly**: Static collections, parameter fields (primary constructors)

### Init-Only Properties
**Not used in this project** — prefer full get/set for simplicity.

---

## Methods & Expressions

### Expression-Bodied Methods
Use sparingly for simple, single-line operations:
```csharp
public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);

public string ToString() => $"{Type}: {Value}";
```

### Block Bodies
Prefer block bodies for complex logic:
```csharp
public List<Token> Tokenize()
{
    var tokens = new List<Token>();
    while (_pos < _input.Length)
    {
        // Complex tokenization logic
    }
    return tokens;
}
```

### Guard Clauses
Use at method entry to validate preconditions:
```csharp
public async Task BindModelAsync(ModelBindingContext bindingContext)
{
    ArgumentNullException.ThrowIfNull(bindingContext);
    // Rest of implementation
}
```

### Switch Expressions
Use for type/value dispatch:
```csharp
return node.LiteralType switch
{
    "string" => BuildStringComparison(node),
    "number" => BuildNumericComparison(node),
    "boolean" => BuildBooleanComparison(node),
    _ => throw new NotSupportedException($"Unknown type: {node.LiteralType}")
};
```

### Null Handling
- **Null coalescing**: `operatorProvider ?? new DefaultOperatorProvider()`
- **Null-conditional**: `select?.Fields`
- **Pattern matching**: `if (expr is not null)`, `is { Count: > 0 }`
- **Throw expressions**: `expr ?? throw new InvalidOperationException("Expected value")`

---

## Language Features & C# Syntax

### Nullable Reference Types (NRT)
Enabled project-wide. Use annotations consistently:
- `string name` — non-nullable string
- `string? value` — nullable string
- `IQueryable<T>` — non-nullable collection
- `Expression?` — nullable expression node

### Type Aliases
Use to avoid naming conflicts:
```csharp
using LinqExpr = System.Linq.Expressions.Expression;
using LinqExprType = System.Linq.Expressions.ExpressionType;
using OQueryLexer = CoreOne.OQuery.Lexer.Lexer;
```

### Pattern Matching
Leverage modern patterns:
```csharp
if (node.Right is not LiteralExpression { LiteralType: "list" })
    throw new InvalidOperationException("Expected list literal");

var fields = select?.Fields is { Count: > 0 } f ? f : [];
```

### var vs. Explicit Types
- **Use var** when type is obvious: `var query = new QueryNode();`
- **Explicit types** for clarity: `Expression<Func<T, bool>> predicate`, `IQueryable<T> result`
- **Preference**: var for locals, explicit for parameters and return types

### Collection Initialization
```csharp
// Modern C# 12 style (preferred where consistent)
var list = new List<T> { };  // or = [];

// Classic style (also acceptable)
var dict = new Dictionary<string, object>();
```

### LINQ Method Chains
Prefer method chain style (not query comprehensions):
```csharp
// ✅ Method chain
var result = source
    .Where(predicate)
    .Skip(offset)
    .Take(count)
    .ToList();

// ❌ Query comprehension (not used in this project)
var result = (from item in source
              where predicate(item)
              select item).ToList();
```

### Reflection
Use with `BindingFlags` for property/method lookup:
```csharp
var prop = typeof(T).GetProperty(propertyName, 
    System.Reflection.BindingFlags.Public | 
    System.Reflection.BindingFlags.Instance | 
    System.Reflection.BindingFlags.IgnoreCase);
```

---

## Exception Handling

### Throw Strategy
- **Generic Exception**: For lexer/parser errors with context information
  ```csharp
  throw new Exception($"Unexpected character '{ch}' at position {_pos}");
  throw new Exception($"Unexpected token: {_current.Type}");
  ```

- **NotSupportedException**: For unsupported operations
  ```csharp
  throw new NotSupportedException($"Operator {op} not supported");
  ```

- **InvalidOperationException**: For invalid state
  ```csharp
  throw new InvalidOperationException("Expected predicate expression");
  ```

### Try-Catch Patterns
- **Evaluators/Parsers**: Let exceptions bubble up for debugging
- **ASP.NET Core Model Binder**: Catch and convert to ModelState errors:
  ```csharp
  try
  {
      // Parsing logic
  }
  catch (Exception ex)
  {
      bindingContext.ModelState.AddModelError("query", ex.Message);
      bindingContext.Result = ModelBindingResult.Failed();
  }
  ```

---

## JSON Serialization (System.Text.Json)

### Polymorphic Type Markers
Use `[JsonDerivedType]` attributes for AST node serialization:
```csharp
[JsonDerivedType(typeof(BinaryExpression), "binary")]
[JsonDerivedType(typeof(UnaryExpression), "unary")]
[JsonDerivedType(typeof(LiteralExpression), "literal")]
public abstract class Expression : Node { ... }
```

### Serialization Order
- Apply attributes to the base abstract class
- Include a discriminator name (string identifier for each type)

---

## XML Documentation Comments

### Coverage
- Provide documentation on **all public APIs**
- Use XML tags for clarity and IDE support

### Standard Format
```csharp
/// <summary>
/// Converts a <see cref="QueryNode"/> AST into a LINQ expression tree
/// for evaluation against an <see cref="IQueryable{T}"/>.
/// </summary>
/// <remarks>
/// This visitor pattern enables flexible query evaluation. Derived classes
/// can override Visit methods to customize expression building.
/// </remarks>
public sealed class QueryableEvaluator<T> : IAstVisitor<LinqExpr> { ... }
```

### Tags
- `<summary>` — brief description (appears in IntelliSense)
- `<param name="">` — parameter documentation
- `<returns>` — return value documentation
- `<remarks>` — detailed implementation notes
- `<see cref=""/>` — cross-references
- `<example>` + `<code>` — usage examples
- `<inheritdoc />` — inherit documentation from interface/base

---

## AspNetCore Integration Patterns

### Model Binder Implementation
```csharp
public sealed class OQueryModelBinder(IOperatorProvider? operatorProvider = null) : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var queryString = bindingContext.HttpContext.Request.Query["query"].ToString();
        if (string.IsNullOrEmpty(queryString))
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return;
        }

        try
        {
            var tokens = new Lexer(queryString).Tokenize();
            var query = new Parser(tokens, operatorProvider).Parse();
            bindingContext.Result = ModelBindingResult.Success(query);
        }
        catch (Exception ex)
        {
            bindingContext.ModelState.AddModelError("query", ex.Message);
            bindingContext.Result = ModelBindingResult.Failed();
        }
    }
}
```

### Model Binder Provider
```csharp
public sealed class OQueryModelBinderProvider(IOperatorProvider? operatorProvider = null) : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        return context.Metadata.ModelType == typeof(QueryNode?)
            ? new OQueryModelBinder(operatorProvider)
            : null;
    }
}
```

### Extension Methods for Setup
```csharp
public static IMvcBuilder AddOQuery(this IMvcBuilder builder, Action<OQueryOptions>? configure = null)
{
    var options = new OQueryOptions();
    configure?.Invoke(options);

    builder.Services.AddSingleton(options.OperatorProvider);
    builder.AddMvcOptions(mvc => mvc.ModelBinderProviders.Insert(0, 
        new OQueryModelBinderProvider(options.OperatorProvider)));

    return builder;
}
```

---

## Extensibility Points

### Custom Operators (IOperatorProvider)
```csharp
public interface IOperatorProvider
{
    bool TryGetOperator(string name, out LinqExprType expressionType);
}
```

### Custom Functions (IFunctionProvider)
```csharp
public interface IFunctionProvider
{
    bool TryGetFunction(string name, List<Expression> arguments, out Expression? result);
}
```

### Custom Visitor (IAstVisitor<T>)
```csharp
public interface IAstVisitor<T>
{
    T Visit(BinaryExpression expr);
    T Visit(UnaryExpression expr);
    T Visit(LiteralExpression expr);
    // ... other node types
}
```

Implement these interfaces to add domain-specific operators, functions, or evaluation strategies.

---

## Collections & Dictionaries

### Type Choices
- **IReadOnlyDictionary/IReadOnlyList**: For public immutable collections
- **Dictionary<K, V>**: For mutable internal state
- **HashSet<T>**: For operator/function registries (case-insensitive with StringComparer.OrdinalIgnoreCase)
- **List<T>**: For dynamic token/expression lists

### Case-Insensitive Lookups
```csharp
private static readonly HashSet<string> Operators = new(StringComparer.OrdinalIgnoreCase)
{
    "AND", "OR", "NOT", "IN", "="
};
```

---

## Common Code Patterns

### Safe Property Access with Reflection
```csharp
var prop = typeof(T).GetProperty(propertyName,
    System.Reflection.BindingFlags.Public |
    System.Reflection.BindingFlags.Instance |
    System.Reflection.BindingFlags.IgnoreCase)
    ?? throw new InvalidOperationException($"Property '{propertyName}' not found");

var value = prop.GetValue(obj);
```

### Type-Specific Predicates
```csharp
public Expression BuildPredicate<T>() where T : class
{
    var parameter = Expression.Parameter(typeof(T), "x");
    // Build expression using parameter
    return Expression.Lambda<Func<T, bool>>(predicate, parameter);
}
```

### Async Model Binding
```csharp
public async Task BindModelAsync(ModelBindingContext bindingContext)
{
    // Async operations here (e.g., database lookups)
    await Task.CompletedTask;
    bindingContext.Result = ModelBindingResult.Success(value);
}
```

---

## Code Organization Best Practices

1. **Usings first** (no global usings; explicit imports at top of file)
2. **File-scoped namespace** (no braces)
3. **Type declaration** (class, interface, enum)
4. **Constants/Fields** (private static, private instance)
5. **Constructors** (primary or traditional)
6. **Properties** (public auto-properties)
7. **Methods** (public first, private last)
8. **Nested types** (if any, at the end)

---

## When Creating New Features

### Checklist
- [ ] Namespace matches folder structure
- [ ] One type per file (file name matches type)
- [ ] Apply XML documentation to public APIs
- [ ] Use PascalCase for types/methods, _camelCase for private fields
- [ ] Mark mutable reference types as `required` where appropriate
- [ ] Add unit tests in a separate test project
- [ ] Update README if public API changes
- [ ] Consider adding `sealed` to concrete implementations
- [ ] Implement `Accept(IAstVisitor<T>)` for new AST nodes
- [ ] Add `[JsonDerivedType]` for serializable nodes

---

## References

- **Architecture**: Lexer → Parser → AST Visitor → LINQ Expression Trees
- **Key Interfaces**: `IAstVisitor<T>`, `IFunctionProvider`, `IOperatorProvider`, `IModelBinder`
- **Base Classes**: `Node` (root), `Expression` (query expressions)
- **Extension Methods**: `Apply<T>()`, `ToPredicate<T>()`, `Project<T>()`
- **Sealed Implementations**: QueryableEvaluator, OQueryModelBinder, OQueryModelBinderProvider, OQueryOptions
