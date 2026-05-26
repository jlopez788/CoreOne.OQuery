namespace CoreOne.OQuery.Expressions;

public class SelectNode : Node
{
    public List<string> Fields { get; set; } = [];

    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
}
