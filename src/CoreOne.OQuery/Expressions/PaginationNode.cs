namespace CoreOne.OQuery.Expressions;

public class PaginationNode : Node
{
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public int? Limit { get; set; }
    public int? Offset { get; set; }

    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
}
