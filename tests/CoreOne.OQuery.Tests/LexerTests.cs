using System;
using System.Collections.Generic;
using CoreOne.OQuery.Lexer;
using Xunit;
using OQueryLexer = CoreOne.OQuery.Lexer.Lexer;

namespace CoreOne.OQuery.Tests;

public class LexerTests
{
    private static List<Token> Tokenize(string input) => new OQueryLexer(input).Tokenize();

    // ── EOF / Whitespace ──────────────────────────────────────────────────────

    [Fact]
    public void Tokenize_EmptyString_ReturnsOnlyEof()
    {
        var tokens = Tokenize("");
        Assert.Single(tokens);
        Assert.Equal(TokenType.EndOfFile, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_Whitespace_SkipsAndReturnsEof()
    {
        var tokens = Tokenize("   ");
        Assert.Single(tokens);
        Assert.Equal(TokenType.EndOfFile, tokens[0].Type);
    }

    // ── Identifiers ───────────────────────────────────────────────────────────

    [Fact]
    public void Tokenize_Identifier_ReturnsIdentifierToken()
    {
        var tokens = Tokenize("name");
        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal("name", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_IdentifierWithUnderscore_ReturnsIdentifierToken()
    {
        var tokens = Tokenize("first_name");
        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal("first_name", tokens[0].Value);
    }

    // ── Literals ──────────────────────────────────────────────────────────────

    [Fact]
    public void Tokenize_StringLiteral_ReturnsStringToken()
    {
        var tokens = Tokenize("\"hello world\"");
        Assert.Equal(TokenType.String, tokens[0].Type);
        Assert.Equal("hello world", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_IntegerNumber_ReturnsNumberToken()
    {
        var tokens = Tokenize("42");
        Assert.Equal(TokenType.Number, tokens[0].Type);
        Assert.Equal("42", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_DecimalNumber_ReturnsNumberToken()
    {
        var tokens = Tokenize("3.14");
        Assert.Equal(TokenType.Number, tokens[0].Type);
        Assert.Equal("3.14", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_GuidLiteral_ReturnsGuidToken()
    {
        var tokens = Tokenize("guid'550e8400-e29b-41d4-a716-446655440000'");
        Assert.Equal(TokenType.Guid, tokens[0].Type);
        Assert.Equal("550e8400-e29b-41d4-a716-446655440000", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_DatetimeLiteral_ReturnsDateTimeToken()
    {
        var tokens = Tokenize("datetime'2024-01-15T10:30:00'");
        Assert.Equal(TokenType.DateTime, tokens[0].Type);
        Assert.Equal("2024-01-15T10:30:00", tokens[0].Value);
    }

    // ── Keywords ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("AND", TokenType.And)]
    [InlineData("and", TokenType.And)]
    [InlineData("OR", TokenType.Or)]
    [InlineData("or", TokenType.Or)]
    [InlineData("NOT", TokenType.Not)]
    [InlineData("not", TokenType.Not)]
    [InlineData("TRUE", TokenType.Boolean)]
    [InlineData("true", TokenType.Boolean)]
    [InlineData("FALSE", TokenType.Boolean)]
    [InlineData("false", TokenType.Boolean)]
    [InlineData("NULL", TokenType.Null)]
    [InlineData("null", TokenType.Null)]
    [InlineData("PAGE", TokenType.Page)]
    [InlineData("page", TokenType.Page)]
    [InlineData("PAGESIZE", TokenType.PageSize)]
    [InlineData("pagesize", TokenType.PageSize)]
    [InlineData("LIMIT", TokenType.Limit)]
    [InlineData("limit", TokenType.Limit)]
    [InlineData("OFFSET", TokenType.Offset)]
    [InlineData("offset", TokenType.Offset)]
    [InlineData("SELECT", TokenType.Select)]
    [InlineData("select", TokenType.Select)]
    public void Tokenize_Keywords_AreRecognizedCaseInsensitively(string keyword, TokenType expected)
    {
        var tokens = Tokenize(keyword);
        Assert.Equal(expected, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_InKeyword_ReturnsOperatorToken()
    {
        // "IN" is a registered operator in DefaultOperatorProvider
        var tokens = Tokenize("IN");
        Assert.Equal(TokenType.Operator, tokens[0].Type);
        Assert.Equal("IN", tokens[0].Value);
    }

    // ── Operators ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("=", "=")]
    [InlineData("!=", "!=")]
    [InlineData(">", ">")]
    [InlineData(">=", ">=")]
    [InlineData("<", "<")]
    [InlineData("<=", "<=")]
    public void Tokenize_BuiltinOperators_ReturnOperatorToken(string input, string expectedValue)
    {
        var tokens = Tokenize(input);
        Assert.Equal(TokenType.Operator, tokens[0].Type);
        Assert.Equal(expectedValue, tokens[0].Value);
    }

    // ── Punctuation ───────────────────────────────────────────────────────────

    [Fact]
    public void Tokenize_LeftParen_ReturnsLeftParenToken()
    {
        var tokens = Tokenize("(");
        Assert.Equal(TokenType.LeftParen, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_RightParen_ReturnsRightParenToken()
    {
        var tokens = Tokenize(")");
        Assert.Equal(TokenType.RightParen, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_Comma_ReturnsCommaToken()
    {
        var tokens = Tokenize(",");
        Assert.Equal(TokenType.Comma, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_Dot_ReturnsDotToken()
    {
        var tokens = Tokenize(".");
        Assert.Equal(TokenType.Dot, tokens[0].Type);
    }

    // ── Complex expressions ───────────────────────────────────────────────────

    [Fact]
    public void Tokenize_ComplexExpression_ProducesCorrectTokenSequence()
    {
        var tokens = Tokenize("name = \"Alice\" AND age > 30");
        // name, =, "Alice", AND, age, >, 30, EOF
        Assert.Equal(8, tokens.Count);
        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal(TokenType.Operator, tokens[1].Type);
        Assert.Equal(TokenType.String, tokens[2].Type);
        Assert.Equal(TokenType.And, tokens[3].Type);
        Assert.Equal(TokenType.Identifier, tokens[4].Type);
        Assert.Equal(TokenType.Operator, tokens[5].Type);
        Assert.Equal(TokenType.Number, tokens[6].Type);
        Assert.Equal(TokenType.EndOfFile, tokens[7].Type);
    }

    [Fact]
    public void Tokenize_SelectClause_ProducesSelectToken()
    {
        var tokens = Tokenize("SELECT name, age");
        Assert.Equal(TokenType.Select, tokens[0].Type);
        Assert.Equal(TokenType.Identifier, tokens[1].Type);
        Assert.Equal(TokenType.Comma, tokens[2].Type);
        Assert.Equal(TokenType.Identifier, tokens[3].Type);
    }

    // ── Custom operator provider ──────────────────────────────────────────────

    [Fact]
    public void Tokenize_CustomKeywordOperator_ReturnsOperatorToken()
    {
        var customProvider = new CustomOperatorProvider(["=", "!=", ">", ">=", "<", "<=", "IN", "LIKE"]);
        var tokens = new OQueryLexer("LIKE", customProvider).Tokenize();
        Assert.Equal(TokenType.Operator, tokens[0].Type);
        Assert.Equal("LIKE", tokens[0].Value);
    }

    // ── Error paths ───────────────────────────────────────────────────────────

    [Fact]
    public void Tokenize_UnexpectedCharacter_ThrowsException()
    {
        var ex = Assert.Throws<Exception>(() => Tokenize("$"));
        Assert.Contains("Unexpected character", ex.Message);
    }

    [Fact]
    public void Tokenize_UnterminatedString_ThrowsException()
    {
        var ex = Assert.Throws<Exception>(() => Tokenize("\"unterminated"));
        Assert.Contains("Unterminated string", ex.Message);
    }

    [Fact]
    public void Tokenize_UnterminatedGuid_ThrowsException()
    {
        var ex = Assert.Throws<Exception>(() => Tokenize("guid'bad-guid"));
        Assert.Contains("Unterminated guid", ex.Message);
    }

    [Fact]
    public void Tokenize_UnterminatedDatetime_ThrowsException()
    {
        var ex = Assert.Throws<Exception>(() => Tokenize("datetime'2024-01-01"));
        Assert.Contains("Unterminated datetime", ex.Message);
    }

    [Fact]
    public void Tokenize_UnknownSymbol_ThrowsException()
    {
        var ex = Assert.Throws<Exception>(() => Tokenize("@"));
        Assert.Contains("Unknown operator", ex.Message);
    }

    // ── Token.ToString ────────────────────────────────────────────────────────

    [Fact]
    public void Token_ToString_ReturnsFormattedString()
    {
        var token = new Token(TokenType.Identifier, "name", 0);
        Assert.Equal("Identifier: name", token.ToString());
    }

    [Fact]
    public void Token_Position_IsPreserved()
    {
        var tokens = Tokenize("   age");
        Assert.Equal(3, tokens[0].Position);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class CustomOperatorProvider(IEnumerable<string> operators) : IOperatorProvider
    {
        private readonly HashSet<string> _ops = new(operators, StringComparer.OrdinalIgnoreCase);
        public bool IsOperator(string op) => _ops.Contains(op);
        public IEnumerable<string> GetOperators() => _ops;
    }
}
