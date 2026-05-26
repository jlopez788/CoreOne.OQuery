using CoreOne.OQuery.Evaluators;
using CoreOne.OQuery.Expressions;

// Aliases to avoid naming conflicts with CoreOne.OQuery.Expressions.BinaryExpression / UnaryExpression

namespace CoreOne.OQuery.Extensions;

/// <summary>
/// Extension methods for applying an OQuery <see cref="QueryNode"/> AST to an <see cref="IQueryable{T}"/> sequence.
/// </summary>
public static class QueryableExtensions
{
    /// <inheritdoc cref="QueryableEvaluator{T}.Apply"/>
    public static IQueryable<T> Apply<T>(this IQueryable<T> source, QueryNode query)
        => new QueryableEvaluator<T>().Apply(source, query);

    /// <inheritdoc cref="QueryableEvaluator{T}.BuildPredicate"/>
    public static System.Linq.Expressions.Expression<Func<T, bool>> ToPredicate<T>(this QueryNode query)
        => new QueryableEvaluator<T>().BuildPredicate(query);

    /// <inheritdoc cref="QueryableEvaluator{T}.Project"/>
    public static IQueryable<IDictionary<string, object?>> Project<T>(this IQueryable<T> source, QueryNode query)
        => new QueryableEvaluator<T>().Project(source, query);
}
