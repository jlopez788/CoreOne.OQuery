using CoreOne.OQuery.Expressions;
using System.Globalization;
using System.Reflection;
using LinqConstExpr = System.Linq.Expressions.ConstantExpression;

// Aliases to avoid naming conflicts with CoreOne.OQuery.Expressions.BinaryExpression / UnaryExpression
using LinqExpr = System.Linq.Expressions.Expression;
using LinqExprType = System.Linq.Expressions.ExpressionType;
using LinqParam = System.Linq.Expressions.ParameterExpression;

namespace CoreOne.OQuery.Evaluators;

/// <summary>
/// Converts a <see cref="QueryNode"/> AST into a LINQ expression tree that can be applied
/// to an <see cref="IQueryable{T}"/>, including filter and pagination.
/// </summary>
public sealed class QueryableEvaluator<T> : IAstVisitor<LinqExpr>
{
    private static readonly IReadOnlyDictionary<string, LinqExprType> OperatorMap =
        new Dictionary<string, LinqExprType>(StringComparer.OrdinalIgnoreCase) {
            ["="] = LinqExprType.Equal,
            ["!="] = LinqExprType.NotEqual,
            [">"] = LinqExprType.GreaterThan,
            [">="] = LinqExprType.GreaterThanOrEqual,
            ["<"] = LinqExprType.LessThan,
            ["<="] = LinqExprType.LessThanOrEqual,
            ["AND"] = LinqExprType.AndAlso,
            ["OR"] = LinqExprType.OrElse,
        };

    private readonly Dictionary<string, Func<IReadOnlyList<LinqExpr>, LinqExpr>> _functions;
    private readonly LinqParam _parameter = LinqExpr.Parameter(typeof(T), "x");

    /// <summary>
    /// Initialises the evaluator with the built-in functions
    /// (<c>contains</c>, <c>startsWith</c>, <c>endsWith</c>).
    /// </summary>
    public QueryableEvaluator()
    {
        _functions = BuildDefaultFunctions();
    }

    /// <summary>
    /// Initialises the evaluator with the built-in functions plus any <paramref name="customFunctions"/>.
    /// Custom entries override built-ins when names collide.
    /// </summary>
    public QueryableEvaluator(IDictionary<string, Func<IReadOnlyList<LinqExpr>, LinqExpr>> customFunctions)
    {
        _functions = BuildDefaultFunctions();
        foreach (var kv in customFunctions)
            _functions[kv.Key] = kv.Value;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Applies the <paramref name="query"/>'s filter and pagination to <paramref name="source"/>
    /// and returns the resulting <see cref="IQueryable{T}"/>.
    /// </summary>
    public IQueryable<T> Apply(IQueryable<T> source, QueryNode query)
    {
        if (query.Expression is not null)
        {
            var predicate = (System.Linq.Expressions.Expression<Func<T, bool>>)query.Accept(this);
            source = source.Where(predicate);
        }

        if (query.Pagination is not null)
            source = ApplyPagination(source, query.Pagination);

        return source;
    }

    /// <summary>
    /// Builds a strongly-typed predicate lambda from the query's filter expression.
    /// Pagination is ignored — use <see cref="Apply"/> to include it.
    /// </summary>
    public System.Linq.Expressions.Expression<Func<T, bool>> BuildPredicate(QueryNode query)
        => (System.Linq.Expressions.Expression<Func<T, bool>>)Visit(query);

    /// <summary>
    /// Applies the <paramref name="query"/>'s filter, field projection, and pagination to
    /// <paramref name="source"/> and returns <see cref="IQueryable{T}"/> of
    /// <see cref="IDictionary{TKey,TValue}"/>.
    /// When no <c>SELECT</c> clause is present, all public properties are projected.
    /// </summary>
    public IQueryable<IDictionary<string, object?>> Project(IQueryable<T> source, QueryNode query)
    {
        IQueryable<T> filtered = source;
        if (query.Expression is not null)
        {
            var predicate = (System.Linq.Expressions.Expression<Func<T, bool>>)query.Accept(this);
            filtered = filtered.Where(predicate);
        }

        var fields = query.Select?.Fields is { Count: > 0 } f
            ? f
            : [.. typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name)];

        var projected = filtered.Select(BuildProjectionLambda(fields));

        if (query.Pagination is not null)
        {
            if (query.Pagination.Page.HasValue && query.Pagination.PageSize.HasValue)
                projected = projected.Skip((query.Pagination.Page.Value - 1) * query.Pagination.PageSize.Value)
                                     .Take(query.Pagination.PageSize.Value);
            else if (query.Pagination.Limit.HasValue)
                projected = projected.Skip(query.Pagination.Offset ?? 0)
                                     .Take(query.Pagination.Limit.Value);
        }

        return projected;
    }

    // -------------------------------------------------------------------------
    // IAstVisitor<LinqExpr> implementation
    // -------------------------------------------------------------------------

    public LinqExpr Visit(BinaryExpression node)
    {
        if (node.Operator.Equals("IN", StringComparison.OrdinalIgnoreCase))
            return BuildInExpression(node);

        var left = node.Left.Accept(this);
        var right = node.Right.Accept(this);

        if (!OperatorMap.TryGetValue(node.Operator, out var exprType))
            throw new NotSupportedException($"Operator '{node.Operator}' is not supported.");

        // Coerce the constant side to match the property type (e.g. decimal → int)
        right = CoerceConstant(right, left.Type);
        left = CoerceConstant(left, right.Type);

        return LinqExpr.MakeBinary(exprType, left, right);
    }

    public LinqExpr Visit(UnaryExpression node)
    {
        if (!node.Operator.Equals("NOT", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException($"Unary operator '{node.Operator}' is not supported.");

        return LinqExpr.Not(node.Operand.Accept(this));
    }

    public LinqExpr Visit(FunctionCallExpression node)
    {
        if (!_functions.TryGetValue(node.Identifier, out var builder))
            throw new NotSupportedException($"Function '{node.Identifier}' is not supported.");

        var args = node.Arguments.Select(a => a.Accept(this)).ToArray();
        return builder(args);
    }

    public LinqExpr Visit(IdentifierExpression node)
    {
        LinqExpr current = _parameter;
        foreach (var segment in node.Name.Split('.'))
        {
            var prop = current.Type.GetProperty(
                           segment,
                           BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                       ?? throw new InvalidOperationException(
                           $"Property '{segment}' not found on type '{current.Type.Name}'.");

            current = LinqExpr.Property(current, prop);
        }
        return current;
    }

    public LinqExpr Visit(LiteralExpression node)
    {
        return node.LiteralType switch {
            "string" => LinqExpr.Constant(node.Value as string ?? node.Value?.ToString()),
            "number" => LinqExpr.Constant(decimal.Parse((string)node.Value!, CultureInfo.InvariantCulture)),
            "boolean" => LinqExpr.Constant(bool.Parse((string)node.Value!)),
            "null" => LinqExpr.Constant(null, typeof(object)),
            "guid" => LinqExpr.Constant(Guid.Parse((string)node.Value!)),
            "datetime" => LinqExpr.Constant(DateTime.Parse(
                              (string)node.Value!,
                              CultureInfo.InvariantCulture,
                              DateTimeStyles.RoundtripKind)),
            "list" => throw new InvalidOperationException(
                              "List literals cannot be used as standalone expressions. Use the IN operator."),
            _ => throw new NotSupportedException(
                              $"Literal type '{node.LiteralType}' is not supported.")
        };
    }

    /// <remarks>
    /// <see cref="SelectNode"/> has no predicate expression representation.
    /// Use <see cref="Project"/> to apply field projection.
    /// </remarks>
    public LinqExpr Visit(SelectNode node)
        => throw new NotSupportedException(
               "SelectNode cannot be converted to a predicate expression tree. Use Project() to apply field projection.");

    /// <remarks>
    /// <see cref="PaginationNode"/> has no expression-tree representation.
    /// Use <see cref="Apply"/> to handle pagination.
    /// </remarks>
    public LinqExpr Visit(PaginationNode node)
        => throw new NotSupportedException(
               "PaginationNode cannot be converted to an expression tree. Use Apply() to handle pagination.");

    public LinqExpr Visit(QueryNode node)
    {
        LinqExpr body = node.Expression is not null
            ? node.Expression.Accept(this)
            : LinqExpr.Constant(true);

        return LinqExpr.Lambda<Func<T, bool>>(body, _parameter);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static IQueryable<T> ApplyPagination(IQueryable<T> source, PaginationNode node)
    {
        if (node.Page.HasValue && node.PageSize.HasValue)
            return source.Skip((node.Page.Value - 1) * node.PageSize.Value).Take(node.PageSize.Value);

        if (node.Limit.HasValue)
            return source.Skip(node.Offset ?? 0).Take(node.Limit.Value);

        return source;
    }

    private static Dictionary<string, Func<IReadOnlyList<LinqExpr>, LinqExpr>> BuildDefaultFunctions()
    {
        return new Dictionary<string, Func<IReadOnlyList<LinqExpr>, LinqExpr>>(StringComparer.OrdinalIgnoreCase) {
            ["contains"] = args => LinqExpr.Call(
                                 args[0],
                                 typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!,
                                 args[1]),
            ["startsWith"] = args => LinqExpr.Call(
                                 args[0],
                                 typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!,
                                 args[1]),
            ["endsWith"] = args => LinqExpr.Call(
                                 args[0],
                                 typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!,
                                 args[1]),
        };
    }

    private static System.Linq.Expressions.Expression<Func<T, IDictionary<string, object?>>> BuildProjectionLambda(
        IReadOnlyList<string> fields)
    {
        var param = LinqExpr.Parameter(typeof(T), "x");
        var addMethod = typeof(Dictionary<string, object?>)
            .GetMethod("Add", [typeof(string), typeof(object)])!;

        var inits = fields.Select(fieldName => {
            var prop = typeof(T).GetProperty(
                           fieldName,
                           BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                       ?? throw new InvalidOperationException(
                           $"Property '{fieldName}' not found on type '{typeof(T).Name}'.");

            return LinqExpr.ElementInit(
                addMethod,
                LinqExpr.Constant(prop.Name), // use canonical casing from the type
                LinqExpr.Convert(LinqExpr.Property(param, prop), typeof(object)));
        });

        var dictInit = LinqExpr.ListInit(
            LinqExpr.New(typeof(Dictionary<string, object?>)),
            inits);

        var body = LinqExpr.Convert(dictInit, typeof(IDictionary<string, object?>));
        return LinqExpr.Lambda<Func<T, IDictionary<string, object?>>>(body, param);
    }

    private static LinqExpr CoerceConstant(LinqExpr expr, Type targetType)
    {
        if (expr.Type == targetType || expr is not LinqConstExpr)
            return expr;

        return LinqExpr.Convert(expr, targetType);
    }

    private LinqExpr BuildInExpression(BinaryExpression node)
    {
        var left = node.Left.Accept(this);

        if (node.Right is not LiteralExpression { LiteralType: "list" } listLiteral)
            throw new InvalidOperationException(
                "The right-hand side of the IN operator must be a list literal, e.g. field IN (a, b, c).");

        var elements = (IEnumerable<CoreOne.OQuery.Expressions.Expression>)listLiteral.Value!;

        var comparisons = elements
            .Select(e => (LinqExpr)LinqExpr.Equal(left, CoerceConstant(e.Accept(this), left.Type)))
            .ToList();

        if (comparisons.Count == 0)
            return LinqExpr.Constant(false);

        return comparisons.Aggregate((acc, e) => LinqExpr.OrElse(acc, e));
    }
}