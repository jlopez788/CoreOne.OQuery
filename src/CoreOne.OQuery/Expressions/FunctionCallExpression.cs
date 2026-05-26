namespace CoreOne.OQuery.Expressions;

public class FunctionCallExpression : Expression
{
    public required string Identifier { get; set; }
    public List<Expression> Arguments { get; set; } = new List<Expression>();

    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
}
