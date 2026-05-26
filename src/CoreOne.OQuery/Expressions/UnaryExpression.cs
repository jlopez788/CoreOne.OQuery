namespace CoreOne.OQuery.Expressions;

public class UnaryExpression : Expression
{
    public required string Operator { get; set; }
    public required Expression Operand { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
}
