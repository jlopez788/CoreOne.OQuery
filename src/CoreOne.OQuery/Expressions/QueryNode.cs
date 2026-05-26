namespace CoreOne.OQuery.Expressions;

public class QueryNode : Node
{
    public Expression? Expression { get; set; }
    public SelectNode? Select { get; set; }
    public PaginationNode? Pagination { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
}
