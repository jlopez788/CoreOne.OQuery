using CoreOne.OQuery.Expressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CoreOne.OQuery.Web;

/// <summary>
/// Activates <see cref="OQueryModelBinder"/> for action parameters of type <see cref="QueryNode"/>.
/// </summary>
/// <param name="options">Options forwarded to the binder instance.</param>
public sealed class OQueryModelBinderProvider(OQueryOptions options) : IModelBinderProvider
{
    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Metadata.ModelType == typeof(QueryNode) ?
            new OQueryModelBinder(options) : null;
    }
}