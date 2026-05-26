using System;
using System.Threading.Tasks;
using CoreOne.OQuery.Expressions;
using CoreOne.OQuery.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;
using Xunit;

namespace CoreOne.OQuery.AspNetCore.Tests;

public class OQueryModelBinderTests
{
    private static OQueryModelBinder CreateBinder(string paramName = "query")
        => new(new OQueryOptions { QueryParameterName = paramName });

    private static Mock<ModelBindingContext> CreateMockContext(
        HttpContext httpContext,
        ModelStateDictionary? modelState = null)
    {
        var mock = new Mock<ModelBindingContext>();
        mock.Setup(c => c.HttpContext).Returns(httpContext);
        mock.Setup(c => c.ModelState).Returns(modelState ?? new ModelStateDictionary());
        mock.SetupProperty(c => c.Result);
        return mock;
    }

    // ── Null guard ────────────────────────────────────────────────────────────

    [Fact]
    public async Task BindModelAsync_NullContext_ThrowsArgumentNullException()
    {
        var binder = CreateBinder();
        await Assert.ThrowsAsync<ArgumentNullException>(() => binder.BindModelAsync(null!));
    }

    // ── Empty / missing query string ──────────────────────────────────────────

    [Fact]
    public async Task BindModelAsync_MissingQueryParam_SuccessWithEmptyQueryNode()
    {
        var binder = CreateBinder();
        var httpContext = new DefaultHttpContext(); // no query string
        var mock = CreateMockContext(httpContext);

        await binder.BindModelAsync(mock.Object);

        Assert.True(mock.Object.Result.IsModelSet);
        var queryNode = Assert.IsType<QueryNode>(mock.Object.Result.Model);
        Assert.Null(queryNode.Expression);
    }

    [Fact]
    public async Task BindModelAsync_EmptyQueryParam_SuccessWithEmptyQueryNode()
    {
        var binder = CreateBinder();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = QueryString.Create("query", "   ");
        var mock = CreateMockContext(httpContext);

        await binder.BindModelAsync(mock.Object);

        Assert.True(mock.Object.Result.IsModelSet);
        var queryNode = Assert.IsType<QueryNode>(mock.Object.Result.Model);
        Assert.Null(queryNode.Expression);
    }

    // ── Valid query ───────────────────────────────────────────────────────────

    [Fact]
    public async Task BindModelAsync_ValidQuery_SuccessWithParsedQueryNode()
    {
        var binder = CreateBinder();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = QueryString.Create("query", "Age > 18");
        var mock = CreateMockContext(httpContext);

        await binder.BindModelAsync(mock.Object);

        Assert.True(mock.Object.Result.IsModelSet);
        var queryNode = Assert.IsType<QueryNode>(mock.Object.Result.Model);
        Assert.NotNull(queryNode.Expression);
    }

    [Fact]
    public async Task BindModelAsync_CustomParamName_ReadsCorrectQueryKey()
    {
        var binder = CreateBinder(paramName: "filter");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = QueryString.Create("filter", "Name = \"Alice\"");
        var mock = CreateMockContext(httpContext);

        await binder.BindModelAsync(mock.Object);

        Assert.True(mock.Object.Result.IsModelSet);
        Assert.NotNull(((QueryNode)mock.Object.Result.Model!).Expression);
    }

    // ── Invalid query ─────────────────────────────────────────────────────────

    [Fact]
    public async Task BindModelAsync_InvalidSyntax_FailedWithModelStateError()
    {
        var binder = CreateBinder();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = QueryString.Create("query", "AND AND AND");
        var modelState = new ModelStateDictionary();
        var mock = CreateMockContext(httpContext, modelState);

        await binder.BindModelAsync(mock.Object);

        Assert.False(mock.Object.Result.IsModelSet);
        Assert.True(modelState.ContainsKey("query"));
        Assert.NotEmpty(modelState["query"]!.Errors);
    }
}
