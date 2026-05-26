using System;
using CoreOne.OQuery.Expressions;
using CoreOne.OQuery.Web;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Moq;
using Xunit;

namespace CoreOne.OQuery.AspNetCore.Tests;

public class OQueryModelBinderProviderTests
{
    private static OQueryModelBinderProvider CreateProvider()
        => new(new OQueryOptions());

    private static Mock<ModelBinderProviderContext> CreateContext(Type modelType)
    {
        var identity = ModelMetadataIdentity.ForType(modelType);
        var mockMetadata = new Mock<ModelMetadata>(identity);
        var mockContext = new Mock<ModelBinderProviderContext>();
        mockContext.Setup(c => c.Metadata).Returns(mockMetadata.Object);
        return mockContext;
    }

    // ── Null guard ────────────────────────────────────────────────────────────

    [Fact]
    public void GetBinder_NullContext_ThrowsArgumentNullException()
    {
        var provider = CreateProvider();
        Assert.Throws<ArgumentNullException>(() => provider.GetBinder(null!));
    }

    // ── QueryNode type ────────────────────────────────────────────────────────

    [Fact]
    public void GetBinder_QueryNodeModelType_ReturnsOQueryModelBinder()
    {
        var provider = CreateProvider();
        var mockContext = CreateContext(typeof(QueryNode));

        var binder = provider.GetBinder(mockContext.Object);

        Assert.IsType<OQueryModelBinder>(binder);
    }

    // ── Other types ───────────────────────────────────────────────────────────

    [Fact]
    public void GetBinder_StringModelType_ReturnsNull()
    {
        var provider = CreateProvider();
        var mockContext = CreateContext(typeof(string));

        var binder = provider.GetBinder(mockContext.Object);

        Assert.Null(binder);
    }

    [Fact]
    public void GetBinder_IntModelType_ReturnsNull()
    {
        var provider = CreateProvider();
        var mockContext = CreateContext(typeof(int));

        var binder = provider.GetBinder(mockContext.Object);

        Assert.Null(binder);
    }
}
