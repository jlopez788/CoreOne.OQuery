namespace CoreOne.OQuery.Expressions;

public class IdentifierExpression : Expression
{
    public required string Name { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
}
