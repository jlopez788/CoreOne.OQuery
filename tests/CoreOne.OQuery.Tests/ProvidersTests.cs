using System.Linq;
using Xunit;

namespace CoreOne.OQuery.Tests;

public class ProvidersTests
{
    // ── DefaultOperatorProvider ───────────────────────────────────────────────

    [Theory]
    [InlineData("=")]
    [InlineData("!=")]
    [InlineData(">")]
    [InlineData(">=")]
    [InlineData("<")]
    [InlineData("<=")]
    [InlineData("IN")]
    [InlineData("in")]   // Case-insensitive
    [InlineData("In")]
    public void DefaultOperatorProvider_IsOperator_RecognizesBuiltinOperators(string op)
    {
        var provider = new DefaultOperatorProvider();
        Assert.True(provider.IsOperator(op));
    }

    [Theory]
    [InlineData("LIKE")]
    [InlineData("~=")]
    [InlineData("AND")]
    [InlineData("")]
    public void DefaultOperatorProvider_IsOperator_ReturnsFalseForUnknownOperators(string op)
    {
        var provider = new DefaultOperatorProvider();
        Assert.False(provider.IsOperator(op));
    }

    [Fact]
    public void DefaultOperatorProvider_GetOperators_ReturnsSevenBuiltinOperators()
    {
        var provider = new DefaultOperatorProvider();
        var ops = provider.GetOperators().ToList();
        Assert.Equal(7, ops.Count);
    }

    // ── DefaultFunctionProvider ───────────────────────────────────────────────

    [Theory]
    [InlineData("contains")]
    [InlineData("startsWith")]
    [InlineData("endsWith")]
    [InlineData("CONTAINS")]    // Case-insensitive
    [InlineData("STARTSWITH")]
    [InlineData("ENDSWITH")]
    public void DefaultFunctionProvider_IsFunction_RecognizesBuiltinFunctions(string name)
    {
        var provider = new DefaultFunctionProvider();
        Assert.True(provider.IsFunction(name));
    }

    [Theory]
    [InlineData("length")]
    [InlineData("toUpper")]
    [InlineData("indexOf")]
    [InlineData("")]
    public void DefaultFunctionProvider_IsFunction_ReturnsFalseForUnknownFunctions(string name)
    {
        var provider = new DefaultFunctionProvider();
        Assert.False(provider.IsFunction(name));
    }
}
