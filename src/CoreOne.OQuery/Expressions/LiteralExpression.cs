namespace CoreOne.OQuery.Expressions;

public class LiteralExpression : Expression
{
    public required object? Value { get; set; }
    public required string LiteralType { get; set; } // "string", "number", "boolean", "null", "guid", "datetime", "list"

    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
}