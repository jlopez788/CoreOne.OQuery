using System.Collections.Generic;
using CoreOne.OQuery.Evaluators;
using CoreOne.OQuery.Expressions;
using Xunit;

namespace CoreOne.OQuery.Tests;

public class QuerySerializerTests
{
    private readonly QuerySerializer _serializer = new();

    // ── BinaryExpression ──────────────────────────────────────────────────────

    [Fact]
    public void Visit_BinaryExpression_WrapsInParensWithOperator()
    {
        var node = new BinaryExpression
        {
            Left = new IdentifierExpression { Name = "age" },
            Operator = ">",
            Right = new LiteralExpression { Value = "30", LiteralType = "number" }
        };
        Assert.Equal("(age > 30)", _serializer.Visit(node));
    }

    // ── UnaryExpression ───────────────────────────────────────────────────────

    [Fact]
    public void Visit_UnaryExpression_WrapsInParensWithOperator()
    {
        var node = new UnaryExpression
        {
            Operator = "NOT",
            Operand = new IdentifierExpression { Name = "isActive" }
        };
        Assert.Equal("(NOT isActive)", _serializer.Visit(node));
    }

    // ── FunctionCallExpression ────────────────────────────────────────────────

    [Fact]
    public void Visit_FunctionCallExpression_NoArgs_SerializesCorrectly()
    {
        var node = new FunctionCallExpression { Identifier = "myFunc", Arguments = [] };
        Assert.Equal("myFunc()", _serializer.Visit(node));
    }

    [Fact]
    public void Visit_FunctionCallExpression_WithArgs_SerializesCorrectly()
    {
        var node = new FunctionCallExpression
        {
            Identifier = "contains",
            Arguments =
            [
                new IdentifierExpression { Name = "name" },
                new LiteralExpression { Value = "Alice", LiteralType = "string" }
            ]
        };
        Assert.Equal("contains(name, \"Alice\")", _serializer.Visit(node));
    }

    // ── IdentifierExpression ──────────────────────────────────────────────────

    [Fact]
    public void Visit_IdentifierExpression_ReturnsName()
    {
        var node = new IdentifierExpression { Name = "address.city" };
        Assert.Equal("address.city", _serializer.Visit(node));
    }

    // ── LiteralExpression ─────────────────────────────────────────────────────

    [Fact]
    public void Visit_StringLiteral_WrapsInDoubleQuotes()
    {
        var node = new LiteralExpression { Value = "hello", LiteralType = "string" };
        Assert.Equal("\"hello\"", _serializer.Visit(node));
    }

    [Fact]
    public void Visit_NumberLiteral_ReturnsRawValue()
    {
        var node = new LiteralExpression { Value = "42", LiteralType = "number" };
        Assert.Equal("42", _serializer.Visit(node));
    }

    [Fact]
    public void Visit_BooleanLiteral_ReturnsRawValue()
    {
        var node = new LiteralExpression { Value = "true", LiteralType = "boolean" };
        Assert.Equal("true", _serializer.Visit(node));
    }

    [Fact]
    public void Visit_NullLiteral_ReturnsNullString()
    {
        var node = new LiteralExpression { Value = null, LiteralType = "null" };
        Assert.Equal("null", _serializer.Visit(node));
    }

    [Fact]
    public void Visit_ListLiteral_SerializesElements()
    {
        var node = new LiteralExpression
        {
            LiteralType = "list",
            Value = new List<Expression>
            {
                new LiteralExpression { Value = "1", LiteralType = "number" },
                new LiteralExpression { Value = "2", LiteralType = "number" }
            }
        };
        Assert.Equal("(1, 2)", _serializer.Visit(node));
    }

    [Fact]
    public void Visit_ListLiteralWithNonExpressionValue_FallsBackToToString()
    {
        // Value is not IEnumerable<Expression>, falls back to Value?.ToString()
        var node = new LiteralExpression { Value = "someRawValue", LiteralType = "list" };
        Assert.Equal("someRawValue", _serializer.Visit(node));
    }

    // ── SelectNode ────────────────────────────────────────────────────────────

    [Fact]
    public void Visit_SelectNode_SerializesFields()
    {
        var node = new SelectNode { Fields = ["name", "age"] };
        Assert.Equal("SELECT name, age", _serializer.Visit(node));
    }

    // ── PaginationNode ────────────────────────────────────────────────────────

    [Fact]
    public void Visit_PaginationNode_PageBased_SerializesCorrectly()
    {
        var node = new PaginationNode { Page = 2, PageSize = 10 };
        Assert.Equal("PAGE 2 PAGESIZE 10", _serializer.Visit(node));
    }

    [Fact]
    public void Visit_PaginationNode_LimitBased_SerializesCorrectly()
    {
        var node = new PaginationNode { Limit = 20, Offset = 40 };
        Assert.Equal("LIMIT 20 OFFSET 40", _serializer.Visit(node));
    }

    [Fact]
    public void Visit_PaginationNode_NoPaginationSet_ReturnsEmptyString()
    {
        var node = new PaginationNode();
        Assert.Equal("", _serializer.Visit(node));
    }

    // ── QueryNode ─────────────────────────────────────────────────────────────

    [Fact]
    public void Visit_QueryNodeWithExpressionOnly_SerializesExpression()
    {
        var node = new QueryNode
        {
            Expression = new BinaryExpression
            {
                Left = new IdentifierExpression { Name = "age" },
                Operator = ">",
                Right = new LiteralExpression { Value = "18", LiteralType = "number" }
            }
        };
        Assert.Equal("(age > 18)", _serializer.Visit(node));
    }

    [Fact]
    public void Visit_QueryNodeWithAllParts_SerializesAllParts()
    {
        var node = new QueryNode
        {
            Expression = new BinaryExpression
            {
                Left = new IdentifierExpression { Name = "age" },
                Operator = ">",
                Right = new LiteralExpression { Value = "18", LiteralType = "number" }
            },
            Select = new SelectNode { Fields = ["name"] },
            Pagination = new PaginationNode { Page = 1, PageSize = 5 }
        };
        var result = _serializer.Visit(node);
        Assert.Contains("(age > 18)", result);
        Assert.Contains("SELECT name", result);
        Assert.Contains("PAGE 1 PAGESIZE 5", result);
    }

    [Fact]
    public void Visit_EmptyQueryNode_ReturnsEmptyString()
    {
        var node = new QueryNode();
        Assert.Equal("", _serializer.Visit(node));
    }
}
