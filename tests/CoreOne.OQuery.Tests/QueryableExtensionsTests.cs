using System.Collections.Generic;
using System.Linq;
using CoreOne.OQuery.Extensions;
using CoreOne.OQuery.Lexer;
using CoreOne.OQuery.Tests.Fixtures;
using Xunit;
using OQueryLexer = CoreOne.OQuery.Lexer.Lexer;
using OQueryParser = CoreOne.OQuery.Parser.Parser;

namespace CoreOne.OQuery.Tests;

public class QueryableExtensionsTests
{
    private static readonly List<TestEntity> Data =
    [
        new() { Name = "Alice",   Age = 30, IsActive = true,  Score = 85.5m },
        new() { Name = "Bob",     Age = 25, IsActive = false, Score = 72.0m },
        new() { Name = "Charlie", Age = 35, IsActive = true,  Score = 90.0m },
        new() { Name = "Diana",   Age = 28, IsActive = true,  Score = 78.3m },
    ];

    private static IQueryable<TestEntity> GetSource() => Data.AsQueryable();

    private static Expressions.QueryNode ParseQuery(string input)
    {
        var tokens = new OQueryLexer(input).Tokenize();
        return new OQueryParser(tokens).Parse();
    }

    // ── Apply ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Apply_WithFilter_ReturnsFilteredResults()
    {
        var query = ParseQuery("Age > 28");
        var result = GetSource().Apply(query).ToList();
        Assert.Equal(2, result.Count); // Alice, Charlie
        Assert.All(result, e => Assert.True(e.Age > 28));
    }

    [Fact]
    public void Apply_WithPagination_ReturnsPaginatedResults()
    {
        var query = ParseQuery("LIMIT 2");
        var result = GetSource().Apply(query).ToList();
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Apply_EmptyQuery_ReturnsAllResults()
    {
        var query = ParseQuery("");
        var result = GetSource().Apply(query).ToList();
        Assert.Equal(4, result.Count);
    }

    // ── ToPredicate ───────────────────────────────────────────────────────────

    [Fact]
    public void ToPredicate_ReturnsCompilableExpression()
    {
        var query = ParseQuery("IsActive = true");
        var predicate = query.ToPredicate<TestEntity>();
        var result = Data.Where(predicate.Compile()).ToList();
        Assert.Equal(3, result.Count);
        Assert.All(result, e => Assert.True(e.IsActive));
    }

    // ── Project ───────────────────────────────────────────────────────────────

    [Fact]
    public void Project_WithSelectClause_ReturnsDictionariesWithSelectedFields()
    {
        var query = ParseQuery("Name = \"Alice\" SELECT Name, Age");
        var result = GetSource().Project(query).ToList();
        Assert.Single(result);
        Assert.True(result[0].ContainsKey("Name"));
        Assert.True(result[0].ContainsKey("Age"));
        Assert.Equal("Alice", result[0]["Name"]);
    }
}
