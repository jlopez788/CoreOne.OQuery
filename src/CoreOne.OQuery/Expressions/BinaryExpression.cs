namespace CoreOne.OQuery.Expressions;

public class BinaryExpression : Expression
{
    public required Expression Left { get; set; }
    public required string Operator { get; set; }
    public required Expression Right { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
}
