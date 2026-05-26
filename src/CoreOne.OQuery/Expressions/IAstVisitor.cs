namespace CoreOne.OQuery.Expressions;

public interface IAstVisitor<T>
{
    T Visit(BinaryExpression node);

    T Visit(UnaryExpression node);

    T Visit(FunctionCallExpression node);

    T Visit(IdentifierExpression node);

    T Visit(LiteralExpression node);

    T Visit(SelectNode node);

    T Visit(PaginationNode node);

    T Visit(QueryNode node);
}