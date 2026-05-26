using CoreOne.OQuery.Web.Extensions;
using CoreOne.OQuery.Expressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OQueryLexer = CoreOne.OQuery.Lexer.Lexer;
using OQueryParser = CoreOne.OQuery.Parser.Parser;

namespace CoreOne.OQuery.Web;

/// <summary>
/// Binds a <see cref="QueryNode"/> from the request query string using the OQuery parser.
/// Register via <see cref="MvcBuilderExtensions.AddOQuery"/> rather than using this class directly.
/// </summary>
/// <param name="options">Resolved options controlling which query string key to read.</param>
public sealed class OQueryModelBinder(OQueryOptions options) : IModelBinder
{
    /// <inheritdoc />
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var paramName = options.QueryParameterName;
        var rawValue = bindingContext.HttpContext.Request.Query[paramName];

        // No parameter present or empty — return an empty QueryNode (pass-through: no filter, no pagination).
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            bindingContext.Result = ModelBindingResult.Success(new QueryNode());
            return Task.CompletedTask;
        }

        try
        {
            var tokens = new OQueryLexer(rawValue!).Tokenize();
            var queryNode = new OQueryParser(tokens).Parse();
            bindingContext.Result = ModelBindingResult.Success(queryNode);
        }
        catch (Exception ex)
        {
            bindingContext.ModelState.AddModelError(paramName, $"Invalid OQuery expression: {ex.Message}");
            bindingContext.Result = ModelBindingResult.Failed();
        }

        return Task.CompletedTask;
    }
}