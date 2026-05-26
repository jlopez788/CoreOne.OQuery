using System;
using System.Linq;
using CoreOne.OQuery.Web;
using CoreOne.OQuery.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace CoreOne.OQuery.AspNetCore.Tests;

public class MvcBuilderExtensionsTests
{
    private sealed class StubMvcBuilder(IServiceCollection services) : IMvcBuilder
    {
        public IServiceCollection Services { get; } = services;
        public ApplicationPartManager PartManager { get; } = new();
    }

    // ── Null guard ────────────────────────────────────────────────────────────

    [Fact]
    public void AddOQuery_NullBuilder_ThrowsArgumentNullException()
    {
        IMvcBuilder? builder = null;
        Assert.Throws<ArgumentNullException>(() => builder!.AddOQuery());
    }

    // ── Default options ───────────────────────────────────────────────────────

    [Fact]
    public void AddOQuery_NoConfiguration_RegistersDefaultOptions()
    {
        var services = new ServiceCollection();
        var builder = new StubMvcBuilder(services);

        builder.AddOQuery();

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<OQueryOptions>();
        Assert.Equal("query", options.QueryParameterName);
    }

    // ── Custom options ────────────────────────────────────────────────────────

    [Fact]
    public void AddOQuery_WithConfiguration_AppliesCustomOptions()
    {
        var services = new ServiceCollection();
        var builder = new StubMvcBuilder(services);

        builder.AddOQuery(opt => opt.QueryParameterName = "filter");

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<OQueryOptions>();
        Assert.Equal("filter", options.QueryParameterName);
    }

    // ── Return value (fluent chaining) ────────────────────────────────────────

    [Fact]
    public void AddOQuery_ReturnsSameBuilderForChaining()
    {
        var services = new ServiceCollection();
        var builder = new StubMvcBuilder(services);

        var result = builder.AddOQuery();

        Assert.Same(builder, result);
    }

    // ── MvcOptions registration ───────────────────────────────────────────────

    [Fact]
    public void AddOQuery_RegistersModelBinderProviderViaConfigureOptions()
    {
        var services = new ServiceCollection();
        var builder = new StubMvcBuilder(services);

        builder.AddOQuery();

        var sp = services.BuildServiceProvider();
        // AddMvcOptions calls services.Configure<MvcOptions>, which registers IConfigureOptions<MvcOptions>
        var configureOptions = sp.GetServices<IConfigureOptions<MvcOptions>>().ToList();
        Assert.NotEmpty(configureOptions);
    }
}
