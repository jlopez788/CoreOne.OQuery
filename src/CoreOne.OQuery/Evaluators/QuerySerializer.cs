using CoreOne.OQuery.Expressions;

namespace CoreOne.OQuery.Evaluators;

/// <summary>
/// A simple visitor that converts the AST back to a string representation (or can be used as a base for SQL/Mongo transpilers).
/// </summary>
public class QuerySerializer : IAstVisitor<string>
{
    public string Visit(BinaryExpression node)
    {
        return $"({node.Left.Accept(this)} {node.Operator} {node.Right.Accept(this)})";
    }

    public string Visit(UnaryExpression node)
    {
        return $"({node.Operator} {node.Operand.Accept(this)})";
    }

    public string Visit(FunctionCallExpression node)
    {
        var args = string.Join(", ", node.Arguments.Select(a => a.Accept(this)));
        return $"{node.Identifier}({args})";
    }

    public string Visit(IdentifierExpression node)
    {
        return node.Name;
    }

    public string Visit(LiteralExpression node)
    {
        if (node.LiteralType == "string")
            return $"\"{node.Value}\"";
        if (node.LiteralType == "list")
        {
            if (node.Value is IEnumerable<Expression> elements)
            {
                return $"({string.Join(", ", elements.Select(e => e.Accept(this)))})";
            }
        }
        return node.Value?.ToString() ?? "null";
    }

    public string Visit(SelectNode node)
    {
        return $"SELECT {string.Join(", ", node.Fields)}";
    }

    public string Visit(PaginationNode node)
    {
        if (node.Page.HasValue)
            return $"PAGE {node.Page} PAGESIZE {node.PageSize}";
        if (node.Limit.HasValue)
            return $"LIMIT {node.Limit} OFFSET {node.Offset}";
        return "";
    }

    public string Visit(QueryNode node)
    {
        var expr = node.Expression?.Accept(this) ?? "";
        var sel  = node.Select?.Accept(this) ?? "";
        var pag  = node.Pagination?.Accept(this) ?? "";
        return string.Join(" ", new[] { expr, sel, pag }.Where(s => !string.IsNullOrEmpty(s)));
    }
}