using System;
using System.Collections.Generic;
using System.Linq;
using CoreOne.OQuery.Evaluators;
using CoreOne.OQuery.Expressions;
using CoreOne.OQuery.Extensions;
using CoreOne.OQuery.Lexer;
using CoreOne.OQuery.Tests.Fixtures;
using Xunit;
using OQueryLexer = CoreOne.OQuery.Lexer.Lexer;
using OQueryParser = CoreOne.OQuery.Parser.Parser;

namespace CoreOne.OQuery.Tests;

public class QueryableEvaluatorTests
{
    private static readonly Guid AliceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid BobId   = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static readonly List<TestEntity> Data =
    [
        new() { Name = "Alice",   Age = 30, IsActive = true,  Score = 85.5m, Id = AliceId, CreatedAt = new DateTime(2024, 1, 15), Department = "Engineering" },
        new() { Name = "Bob",     Age = 25, IsActive = false, Score = 72.0m, Id = BobId,   CreatedAt = new DateTime(2024, 3, 20), Department = "Marketing"   },
        new() { Name = "Charlie", Age = 35, IsActive = true,  Score = 90.0m, Id = Guid.NewGuid(), CreatedAt = new DateTime(2023, 11, 5), Department = "Engineering" },
        new() { Name = "Diana",   Age = 28, IsActive = true,  Score = 78.3m, Id = Guid.NewGuid(), CreatedAt = new DateTime(2024, 6, 10), Department = null },
    ];

    private static IQueryable<TestEntity> GetSource() => Data.AsQueryable();

    private static QueryNode ParseQuery(string input)
    {
        var tokens = new OQueryLexer(input).Tokenize();
        return new OQueryParser(tokens).Parse();
    }

    // ── Apply — no filter ─────────────────────────────────────────────────────

    [Fact]
    public void Apply_NoFilter_ReturnsAllEntities()
    {
        var query = ParseQuery("");
        var result = GetSource().Apply(query).ToList();
        Assert.Equal(4, result.Count);
    }

    // ── Apply — comparison operators ──────────────────────────────────────────

    [Fact]
    public void Apply_EqualityFilter_ReturnsMatchingEntities()
    {
        var result = GetSource().Apply(ParseQuery("Name = \"Alice\"")).ToList();
        Assert.Single(result);
        Assert.Equal("Alice", result[0].Name);
    }

    [Fact]
    public void Apply_NotEqualFilter_ExcludesMatchingEntity()
    {
        var result = GetSource().Apply(ParseQuery("Name != \"Alice\"")).ToList();
        Assert.Equal(3, result.Count);
        Assert.DoesNotContain(result, e => e.Name == "Alice");
    }

    [Fact]
    public void Apply_GreaterThanFilter_ReturnsOnlyLargerValues()
    {
        var result = GetSource().Apply(ParseQuery("Age > 28")).ToList();
        Assert.All(result, e => Assert.True(e.Age > 28));
    }

    [Fact]
    public void Apply_GreaterThanOrEqualFilter_IncludesBoundaryValue()
    {
        var result = GetSource().Apply(ParseQuery("Age >= 30")).ToList();
        Assert.Equal(2, result.Count); // Alice (30), Charlie (35)
        Assert.All(result, e => Assert.True(e.Age >= 30));
    }

    [Fact]
    public void Apply_LessThanFilter_ReturnsOnlySmallerValues()
    {
        var result = GetSource().Apply(ParseQuery("Age < 30")).ToList();
        Assert.Equal(2, result.Count); // Bob (25), Diana (28)
        Assert.All(result, e => Assert.True(e.Age < 30));
    }

    [Fact]
    public void Apply_LessThanOrEqualFilter_IncludesBoundaryValue()
    {
        var result = GetSource().Apply(ParseQuery("Age <= 28")).ToList();
        Assert.Equal(2, result.Count); // Bob (25), Diana (28)
    }

    // ── Apply — logical operators ─────────────────────────────────────────────

    [Fact]
    public void Apply_AndFilter_RequiresBothConditions()
    {
        var result = GetSource().Apply(ParseQuery("IsActive = true AND Age > 28")).ToList();
        Assert.Equal(2, result.Count); // Alice (30), Charlie (35)
        Assert.All(result, e => Assert.True(e.IsActive && e.Age > 28));
    }

    [Fact]
    public void Apply_OrFilter_AcceptsEitherCondition()
    {
        var result = GetSource().Apply(ParseQuery("Name = \"Alice\" OR Name = \"Bob\"")).ToList();
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Apply_NotFilter_InvertsCondition()
    {
        var result = GetSource().Apply(ParseQuery("NOT IsActive = true")).ToList();
        Assert.Single(result); // Bob
        Assert.Equal("Bob", result[0].Name);
    }

    // ── Apply — IN operator ───────────────────────────────────────────────────

    [Fact]
    public void Apply_InOperator_ReturnsEntitiesMatchingAnyValue()
    {
        var result = GetSource().Apply(ParseQuery("Name IN (\"Alice\", \"Charlie\")")).ToList();
        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Name == "Alice");
        Assert.Contains(result, e => e.Name == "Charlie");
    }

    [Fact]
    public void Apply_InOperatorEmptyList_ReturnsNoEntities()
    {
        var evaluator = new QueryableEvaluator<TestEntity>();
        var query = new QueryNode
        {
            Expression = new BinaryExpression
            {
                Left = new IdentifierExpression { Name = "Name" },
                Operator = "IN",
                Right = new LiteralExpression { Value = new List<Expression>(), LiteralType = "list" }
            }
        };
        var result = evaluator.Apply(GetSource(), query).ToList();
        Assert.Empty(result);
    }

    // ── Apply — string functions ──────────────────────────────────────────────

    [Fact]
    public void Apply_ContainsFunction_ReturnsMatchingEntities()
    {
        var result = GetSource().Apply(ParseQuery("contains(Name, \"li\")")).ToList();
        Assert.Equal(2, result.Count); // Alice, Charlie
    }

    [Fact]
    public void Apply_StartsWithFunction_ReturnsMatchingEntities()
    {
        var result = GetSource().Apply(ParseQuery("startsWith(Name, \"A\")")).ToList();
        Assert.Single(result);
        Assert.Equal("Alice", result[0].Name);
    }

    [Fact]
    public void Apply_EndsWithFunction_ReturnsMatchingEntities()
    {
        var result = GetSource().Apply(ParseQuery("endsWith(Name, \"ob\")")).ToList();
        Assert.Single(result);
        Assert.Equal("Bob", result[0].Name);
    }

    // ── Apply — Guid filter ───────────────────────────────────────────────────

    [Fact]
    public void Apply_GuidFilter_ReturnsMatchingEntity()
    {
        var result = GetSource().Apply(ParseQuery($"Id = guid'{AliceId}'")).ToList();
        Assert.Single(result);
        Assert.Equal("Alice", result[0].Name);
    }

    // ── Apply — boolean filter ────────────────────────────────────────────────

    [Fact]
    public void Apply_BooleanTrueFilter_ReturnsActiveEntities()
    {
        var result = GetSource().Apply(ParseQuery("IsActive = true")).ToList();
        Assert.Equal(3, result.Count); // Alice, Charlie, Diana
        Assert.All(result, e => Assert.True(e.IsActive));
    }

    [Fact]
    public void Apply_BooleanFalseFilter_ReturnsInactiveEntities()
    {
        var result = GetSource().Apply(ParseQuery("IsActive = false")).ToList();
        Assert.Single(result);
        Assert.Equal("Bob", result[0].Name);
    }

    // ── Apply — pagination ────────────────────────────────────────────────────

    [Fact]
    public void Apply_PagePagination_ReturnsCorrectPage()
    {
        var result = GetSource().Apply(ParseQuery("PAGE 2 PAGESIZE 2")).ToList();
        Assert.Equal(2, result.Count);
        // Page 2 of 2: Charlie, Diana
        Assert.Equal("Charlie", result[0].Name);
        Assert.Equal("Diana", result[1].Name);
    }

    [Fact]
    public void Apply_LimitOffsetPagination_ReturnsCorrectSlice()
    {
        var result = GetSource().Apply(ParseQuery("LIMIT 2 OFFSET 1")).ToList();
        Assert.Equal(2, result.Count);
        // Skip 1, take 2: Bob, Charlie
        Assert.Equal("Bob", result[0].Name);
        Assert.Equal("Charlie", result[1].Name);
    }

    [Fact]
    public void Apply_LimitWithoutOffset_StartFromBeginning()
    {
        var result = GetSource().Apply(ParseQuery("LIMIT 2")).ToList();
        Assert.Equal(2, result.Count);
        Assert.Equal("Alice", result[0].Name);
    }

    // ── BuildPredicate ────────────────────────────────────────────────────────

    [Fact]
    public void BuildPredicate_ReturnsCompilableExpression()
    {
        var query = ParseQuery("Age > 28");
        var evaluator = new QueryableEvaluator<TestEntity>();
        var predicate = evaluator.BuildPredicate(query);
        var compiled = predicate.Compile();
        var result = Data.Where(compiled).ToList();
        Assert.Equal(2, result.Count); // Alice (30), Charlie (35)
    }

    // ── Project ───────────────────────────────────────────────────────────────

    [Fact]
    public void Project_WithSelectFields_ReturnsOnlySelectedKeys()
    {
        var query = ParseQuery("Name = \"Alice\" SELECT Name, Age");
        var result = GetSource().Project(query).ToList();
        Assert.Single(result);
        Assert.True(result[0].ContainsKey("Name"));
        Assert.True(result[0].ContainsKey("Age"));
        Assert.False(result[0].ContainsKey("IsActive"));
    }

    [Fact]
    public void Project_WithoutSelectFields_ReturnsAllProperties()
    {
        var evaluator = new QueryableEvaluator<TestEntity>();
        var query = ParseQuery("Name = \"Alice\"");
        var result = evaluator.Project(GetSource(), query).ToList();
        Assert.Single(result);
        Assert.True(result[0].ContainsKey("Name"));
        Assert.True(result[0].ContainsKey("Age"));
        Assert.True(result[0].ContainsKey("IsActive"));
    }

    [Fact]
    public void Project_WithPagePagination_AppliesPagination()
    {
        var evaluator = new QueryableEvaluator<TestEntity>();
        var query = ParseQuery("SELECT Name PAGE 2 PAGESIZE 2");
        var result = evaluator.Project(GetSource(), query).ToList();
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Project_WithLimitPagination_AppliesPagination()
    {
        var evaluator = new QueryableEvaluator<TestEntity>();
        var query = ParseQuery("SELECT Name LIMIT 1 OFFSET 2");
        var result = evaluator.Project(GetSource(), query).ToList();
        Assert.Single(result);
    }

    // ── Custom function constructor ────────────────────────────────────────────

    [Fact]
    public void QueryableEvaluator_CustomFunctions_MergeWithDefaults()
    {
        var customFns = new Dictionary<string, Func<IReadOnlyList<System.Linq.Expressions.Expression>, System.Linq.Expressions.Expression>>
        {
            ["toUpper"] = args => System.Linq.Expressions.Expression.Call(
                args[0], typeof(string).GetMethod("ToUpper", Type.EmptyTypes)!)
        };
        var evaluator = new QueryableEvaluator<TestEntity>(customFns);
        // Default 'contains' still works
        var query = ParseQuery("contains(Name, \"li\")");
        var result = evaluator.Apply(GetSource(), query).ToList();
        Assert.Equal(2, result.Count); // Alice, Charlie
    }

    // ── Visit — error paths ───────────────────────────────────────────────────

    [Fact]
    public void Visit_UnsupportedOperator_ThrowsNotSupportedException()
    {
        var evaluator = new QueryableEvaluator<TestEntity>();
        var node = new BinaryExpression
        {
            Left = new IdentifierExpression { Name = "Name" },
            Operator = "LIKE",
            Right = new LiteralExpression { Value = "Alice", LiteralType = "string" }
        };
        Assert.Throws<NotSupportedException>(() => evaluator.Visit(node));
    }

    [Fact]
    public void Visit_UnsupportedUnaryOperator_ThrowsNotSupportedException()
    {
        var evaluator = new QueryableEvaluator<TestEntity>();
        var node = new UnaryExpression
        {
            Operator = "MINUS",
            Operand = new LiteralExpression { Value = "1", LiteralType = "number" }
        };
        Assert.Throws<NotSupportedException>(() => evaluator.Visit(node));
    }

    [Fact]
    public void Visit_UnknownFunction_ThrowsNotSupportedException()
    {
        var evaluator = new QueryableEvaluator<TestEntity>();
        var node = new FunctionCallExpression { Identifier = "unknownFn", Arguments = [] };
        Assert.Throws<NotSupportedException>(() => evaluator.Visit(node));
    }

    [Fact]
    public void Visit_IdentifierExpression_MissingProperty_ThrowsInvalidOperationException()
    {
        var evaluator = new QueryableEvaluator<TestEntity>();
        var node = new IdentifierExpression { Name = "NonExistentProperty" };
        Assert.Throws<InvalidOperationException>(() => evaluator.Visit(node));
    }

    [Fact]
    public void Visit_SelectNode_ThrowsNotSupportedException()
    {
        var evaluator = new QueryableEvaluator<TestEntity>();
        var node = new SelectNode { Fields = ["Name"] };
        Assert.Throws<NotSupportedException>(() => evaluator.Visit(node));
    }

    [Fact]
    public void Visit_PaginationNode_ThrowsNotSupportedException()
    {
        var evaluator = new QueryableEvaluator<TestEntity>();
        var node = new PaginationNode { Limit = 10 };
        Assert.Throws<NotSupportedException>(() => evaluator.Visit(node));
    }

    [Fact]
    public void Visit_InOperatorWithNonListRight_ThrowsInvalidOperationException()
    {
        var evaluator = new QueryableEvaluator<TestEntity>();
        var node = new BinaryExpression
        {
            Left = new IdentifierExpression { Name = "Name" },
            Operator = "IN",
            Right = new LiteralExpression { Value = "Alice", LiteralType = "string" }
        };
        Assert.Throws<InvalidOperationException>(() => evaluator.Visit(node));
    }

    // ── Visit — literal types ─────────────────────────────────────────────────

    [Fact]
    public void Visit_StringLiteral_ReturnsStringConstant()
    {
        var evaluator = new QueryableEvaluator<TestEntity>();
        var node = new LiteralExpression { Value = "hello", LiteralType = "string" };
        var result = evaluator.Visit(node);
        var constant = Assert.IsType<System.Linq.Expressions.ConstantExpression>(result);
        Assert.Equal("hello", constant.Value);
    }

    [Fact]
    public void Visit_NumberLiteral_ReturnsDecimalConstant()
    {
        var evaluator = new QueryableEvaluator<TestEntity>();
        var node = new LiteralExpression { Value = "42.5", LiteralType = "number" };
        var result = evaluator.Visit(node);
        var constant = Assert.IsType<System.Linq.Expressions.ConstantExpression>(result);
        Assert.Equal(42.5m, constant.Value);
    }

    [Fact]
    public void Visit_BooleanLiteral_ReturnsBoolConstant()
    {
        var evaluator = new QueryableEvaluator<TestEntity>();
        var node = new LiteralExpression { Value = "true", LiteralType = "boolean" };
        var result = evaluator.Visit(node);
        var constant = Assert.IsType<System.Linq.Expressions.ConstantExpression>(result);
        Assert.Equal(true, constant.Value);
    }

    [Fact]
    public void Visit_NullLiteral_ReturnsNullConstant()
    {
        var evaluator = new QueryableEvaluator<TestEntity>();
        var node = new LiteralExpression { Value = null, LiteralType = "null" };
        var result = evaluator.Visit(node);
        var constant = Assert.IsType<System.Linq.Expressions.ConstantExpression>(result);
        Assert.Null(constant.Value);
    }

    [Fact]
    public void Visit_GuidLiteral_ReturnsGuidConstant()
    {
        var evaluator = new QueryableEvaluator<TestEntity>();
        var node = new LiteralExpression { Value = AliceId.ToString(), LiteralType = "guid" };
        var result = evaluator.Visit(node);
        var constant = Assert.IsType<System.Linq.Expressions.ConstantExpression>(result);
        Assert.Equal(AliceId, constant.Value);
    }

    [Fact]
    public void Visit_DatetimeLiteral_ReturnsDateTimeConstant()
    {
        var evaluator = new QueryableEvaluator<TestEntity>();
        var node = new LiteralExpression { Value = "2024-01-15T00:00:00", LiteralType = "datetime" };
        var result = evaluator.Visit(node);
        var constant = Assert.IsType<System.Linq.Expressions.ConstantExpression>(result);
        Assert.IsType<DateTime>(constant.Value);
    }

    [Fact]
    public void Visit_ListLiteralStandalone_ThrowsInvalidOperationException()
    {
        var evaluator = new QueryableEvaluator<TestEntity>();
        var node = new LiteralExpression { Value = new List<Expression>(), LiteralType = "list" };
        Assert.Throws<InvalidOperationException>(() => evaluator.Visit(node));
    }

    [Fact]
    public void Visit_UnsupportedLiteralType_ThrowsNotSupportedException()
    {
        var evaluator = new QueryableEvaluator<TestEntity>();
        var node = new LiteralExpression { Value = "x", LiteralType = "unknown" };
        Assert.Throws<NotSupportedException>(() => evaluator.Visit(node));
    }

    // ── Visit — QueryNode with no expression ──────────────────────────────────

    [Fact]
    public void Visit_QueryNodeWithNoExpression_ReturnsAlwaysTruePredicate()
    {
        var evaluator = new QueryableEvaluator<TestEntity>();
        var query = new QueryNode(); // no expression
        var predicate = evaluator.BuildPredicate(query);
        var result = Data.Where(predicate.Compile()).ToList();
        Assert.Equal(4, result.Count);
    }
}
