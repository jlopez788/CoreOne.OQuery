using Microsoft.Extensions.DependencyInjection;

namespace CoreOne.OQuery.Web.Extensions;

/// <summary>
/// Extension methods for registering OQuery model binding in an ASP.NET Core application.
/// </summary>
public static class MvcBuilderExtensions
{
    /// <summary>
    /// Registers the OQuery model binder with default options so that controller action
    /// parameters of type <see cref="Expressions.QueryNode"/> are automatically
    /// bound from the <c>query</c> query string parameter.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Services.AddControllers().AddOQuery();
    ///
    /// // Controller:
    /// // GET /api/tickets?query=status = "open" SELECT assignee, priority LIMIT 20
    /// [HttpGet]
    /// public IActionResult Get(QueryNode query) => Ok(_db.Tickets.Apply(query).ToList());
    /// </code>
    /// </example>
    public static IMvcBuilder AddOQuery(this IMvcBuilder builder, Action<OQueryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new OQueryOptions();
        configure?.Invoke(options);

        // Register options in DI so callers can resolve OQueryOptions if needed.
        builder.Services.AddSingleton(options);

        // Insert at position 0 so the OQuery provider runs before the built-in complex-type binder.
        builder.AddMvcOptions(mvcOptions =>
            mvcOptions.ModelBinderProviders.Insert(0, new OQueryModelBinderProvider(options)));

        return builder;
    }
}