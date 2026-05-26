using System.Text.Json.Serialization;

namespace CoreOne.OQuery.Expressions;

[JsonDerivedType(typeof(Expression), "expression")]
[JsonDerivedType(typeof(BinaryExpression), "binary")]
[JsonDerivedType(typeof(UnaryExpression), "unary")]
[JsonDerivedType(typeof(FunctionCallExpression), "function")]
[JsonDerivedType(typeof(IdentifierExpression), "identifier")]
[JsonDerivedType(typeof(LiteralExpression), "literal")]
[JsonDerivedType(typeof(PaginationNode), "pagination")]
[JsonDerivedType(typeof(SelectNode), "select")]
[JsonDerivedType(typeof(QueryNode), "query")]
public abstract class Node
{
    public abstract T Accept<T>(IAstVisitor<T> visitor);
}