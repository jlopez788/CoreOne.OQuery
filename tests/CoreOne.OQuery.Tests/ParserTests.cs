using System;
using System.Collections.Generic;
using System.Linq;
using CoreOne.OQuery.Expressions;
using CoreOne.OQuery.Lexer;
using Xunit;
using OQueryLexer = CoreOne.OQuery.Lexer.Lexer;
using OQueryParser = CoreOne.OQuery.Parser.Parser;

namespace CoreOne.OQuery.Tests;

public class ParserTests
{
    private static QueryNode Parse(string input)
    {
        var tokens = new OQueryLexer(input).Tokenize();
        return new OQueryParser(tokens).Parse();
    }

    // ── Empty / EOF ───────────────────────────────────────────────────────────

    [Fact]
    public void Parse_EmptyString_ReturnsQueryNodeWithNoExpression()
    {
        var query = Parse("");
        Assert.Null(query.Expression);
        Assert.Null(query.Select);
        Assert.Null(query.Pagination);
    }

    // ── Binary expressions ────────────────────────────────────────────────────

    [Fact]
    public void Parse_SimpleEquality_ReturnsBinaryExpression()
    {
        var query = Parse("name = \"Alice\"");
        var binary = Assert.IsType<BinaryExpression>(query.Expression);
        Assert.Equal("=", binary.Operator);
        Assert.IsType<IdentifierExpression>(binary.Left);
        Assert.IsType<LiteralExpression>(binary.Right);
    }

    [Fact]
    public void Parse_NumberComparison_ReturnsBinaryExpression()
    {
        var query = Parse("age > 30");
        var binary = Assert.IsType<BinaryExpression>(query.Expression);
        Assert.Equal(">", binary.Operator);
        var literal = Assert.IsType<LiteralExpression>(binary.Right);
        Assert.Equal("number", literal.LiteralType);
        Assert.Equal("30", literal.Value);
    }

    // ── Logical operators ─────────────────────────────────────────────────────

    [Fact]
    public void Parse_AndExpression_ReturnsBinaryExpressionWithAndOperator()
    {
        var query = Parse("age > 18 AND isActive = true");
        var binary = Assert.IsType<BinaryExpression>(query.Expression);
        Assert.Equal("AND", binary.Operator);
        Assert.IsType<BinaryExpression>(binary.Left);
        Assert.IsType<BinaryExpression>(binary.Right);
    }

    [Fact]
    public void Parse_OrExpression_ReturnsBinaryExpressionWithOrOperator()
    {
        var query = Parse("name = \"Alice\" OR name = \"Bob\"");
        var binary = Assert.IsType<BinaryExpression>(query.Expression);
        Assert.Equal("OR", binary.Operator);
    }

    [Fact]
    public void Parse_ChainedAnd_LeftAssociative()
    {
        var query = Parse("a = 1 AND b = 2 AND c = 3");
        var outer = Assert.IsType<BinaryExpression>(query.Expression);
        Assert.Equal("AND", outer.Operator);
        var inner = Assert.IsType<BinaryExpression>(outer.Left);
        Assert.Equal("AND", inner.Operator);
    }

    // ── Unary NOT ─────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_NotExpression_ReturnsUnaryExpression()
    {
        var query = Parse("NOT isActive = true");
        var unary = Assert.IsType<UnaryExpression>(query.Expression);
        Assert.Equal("NOT", unary.Operator);
        Assert.IsType<BinaryExpression>(unary.Operand);
    }

    // ── Identifiers & member access ───────────────────────────────────────────

    [Fact]
    public void Parse_DottedMemberAccess_ReturnsIdentifierWithFullPath()
    {
        var query = Parse("address.city = \"NYC\"");
        var binary = Assert.IsType<BinaryExpression>(query.Expression);
        var identifier = Assert.IsType<IdentifierExpression>(binary.Left);
        Assert.Equal("address.city", identifier.Name);
    }

    // ── Literals ──────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_BooleanLiteral_ReturnsLiteralExpressionWithBooleanType()
    {
        var query = Parse("isActive = true");
        var binary = Assert.IsType<BinaryExpression>(query.Expression);
        var literal = Assert.IsType<LiteralExpression>(binary.Right);
        Assert.Equal("boolean", literal.LiteralType);
    }

    [Fact]
    public void Parse_NullLiteral_ReturnsLiteralExpressionWithNullType()
    {
        var query = Parse("name = null");
        var binary = Assert.IsType<BinaryExpression>(query.Expression);
        var literal = Assert.IsType<LiteralExpression>(binary.Right);
        Assert.Equal("null", literal.LiteralType);
    }

    [Fact]
    public void Parse_GuidLiteral_ReturnsLiteralExpressionWithGuidType()
    {
        var query = Parse("id = guid'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'");
        var binary = Assert.IsType<BinaryExpression>(query.Expression);
        var literal = Assert.IsType<LiteralExpression>(binary.Right);
        Assert.Equal("guid", literal.LiteralType);
    }

    [Fact]
    public void Parse_DatetimeLiteral_ReturnsLiteralExpressionWithDatetimeType()
    {
        var query = Parse("createdAt = datetime'2024-01-15T00:00:00'");
        var binary = Assert.IsType<BinaryExpression>(query.Expression);
        var literal = Assert.IsType<LiteralExpression>(binary.Right);
        Assert.Equal("datetime", literal.LiteralType);
    }

    // ── IN / List literals ────────────────────────────────────────────────────

    [Fact]
    public void Parse_InExpression_ReturnsBinaryExpressionWithListLiteralOnRight()
    {
        var query = Parse("age IN (18, 25, 30)");
        var binary = Assert.IsType<BinaryExpression>(query.Expression);
        Assert.Equal("IN", binary.Operator);
        var list = Assert.IsType<LiteralExpression>(binary.Right);
        Assert.Equal("list", list.LiteralType);
        var elements = Assert.IsAssignableFrom<IEnumerable<Expression>>(list.Value);
        Assert.Equal(3, elements.Count());
    }

    [Fact]
    public void Parse_EmptyListLiteral_ReturnsListLiteralWithNoElements()
    {
        var query = Parse("age IN ()");
        var binary = Assert.IsType<BinaryExpression>(query.Expression);
        var list = Assert.IsType<LiteralExpression>(binary.Right);
        Assert.Equal("list", list.LiteralType);
        var elements = Assert.IsAssignableFrom<IEnumerable<Expression>>(list.Value);
        Assert.Empty(elements);
    }

    // ── Parenthesized expression ──────────────────────────────────────────────

    [Fact]
    public void Parse_ParenthesizedExpression_GroupsCorrectly()
    {
        var query = Parse("(name = \"Alice\")");
        Assert.IsType<BinaryExpression>(query.Expression);
    }

    [Fact]
    public void Parse_NestedParens_ResolveCorrectly()
    {
        var query = Parse("(age > 18 AND isActive = true) OR name = \"Admin\"");
        var outer = Assert.IsType<BinaryExpression>(query.Expression);
        Assert.Equal("OR", outer.Operator);
    }

    // ── Function calls ────────────────────────────────────────────────────────

    [Fact]
    public void Parse_FunctionCallWithTwoArgs_ReturnsFunctionCallExpression()
    {
        var query = Parse("contains(name, \"Alice\")");
        var func = Assert.IsType<FunctionCallExpression>(query.Expression);
        Assert.Equal("contains", func.Identifier);
        Assert.Equal(2, func.Arguments.Count);
        Assert.IsType<IdentifierExpression>(func.Arguments[0]);
        Assert.IsType<LiteralExpression>(func.Arguments[1]);
    }

    [Fact]
    public void Parse_FunctionCallWithNoArgs_ReturnsFunctionCallExpression()
    {
        var query = Parse("myFunc()");
        var func = Assert.IsType<FunctionCallExpression>(query.Expression);
        Assert.Equal("myFunc", func.Identifier);
        Assert.Empty(func.Arguments);
    }

    // ── SELECT clause ─────────────────────────────────────────────────────────

    [Fact]
    public void Parse_SelectClause_ReturnsSelectNodeWithFields()
    {
        var query = Parse("name = \"Alice\" SELECT name, age");
        Assert.NotNull(query.Select);
        Assert.Equal(["name", "age"], query.Select!.Fields);
    }

    [Fact]
    public void Parse_SelectOnly_NoExpression()
    {
        var query = Parse("SELECT name");
        Assert.Null(query.Expression);
        Assert.NotNull(query.Select);
        Assert.Equal(["name"], query.Select!.Fields);
    }

    // ── Pagination ────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_PagePagination_ReturnsPaginationWithPageAndPageSize()
    {
        var query = Parse("name = \"Alice\" PAGE 2 PAGESIZE 10");
        Assert.NotNull(query.Pagination);
        Assert.Equal(2, query.Pagination!.Page);
        Assert.Equal(10, query.Pagination.PageSize);
        Assert.Null(query.Pagination.Limit);
    }

    [Fact]
    public void Parse_LimitOffsetPagination_ReturnsPaginationWithLimitAndOffset()
    {
        var query = Parse("LIMIT 20 OFFSET 40");
        Assert.Null(query.Expression);
        Assert.NotNull(query.Pagination);
        Assert.Equal(20, query.Pagination!.Limit);
        Assert.Equal(40, query.Pagination.Offset);
    }

    [Fact]
    public void Parse_LimitWithoutOffset_OffsetIsNull()
    {
        var query = Parse("LIMIT 10");
        Assert.NotNull(query.Pagination);
        Assert.Equal(10, query.Pagination!.Limit);
        Assert.Null(query.Pagination.Offset);
    }

    // ── Combined query ────────────────────────────────────────────────────────

    [Fact]
    public void Parse_ExpressionSelectAndPagination_AllPartsPopulated()
    {
        var query = Parse("age > 18 SELECT name, age PAGE 1 PAGESIZE 5");
        Assert.NotNull(query.Expression);
        Assert.NotNull(query.Select);
        Assert.Equal(2, query.Select!.Fields.Count);
        Assert.NotNull(query.Pagination);
        Assert.Equal(1, query.Pagination!.Page);
    }

    // ── Error paths ───────────────────────────────────────────────────────────

    [Fact]
    public void Parse_UnexpectedToken_ThrowsException()
    {
        var ex = Assert.Throws<Exception>(() => Parse("AND"));
        Assert.Contains("Unexpected token", ex.Message);
    }

    [Fact]
    public void Parse_MissingClosingParen_ThrowsException()
    {
        var ex = Assert.Throws<Exception>(() => Parse("(name = \"test\""));
        Assert.Contains("Expect ')'", ex.Message);
    }

    [Fact]
    public void Parse_MissingPageSize_ThrowsException()
    {
        // PAGE without PAGESIZE token following
        var ex = Assert.Throws<Exception>(() => Parse("PAGE 1 name"));
        Assert.Contains("Expect 'pageSize'", ex.Message);
    }
}
