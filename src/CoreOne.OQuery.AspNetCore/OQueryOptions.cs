namespace CoreOne.OQuery.Web;

/// <summary>
/// Configuration options for the OQuery ASP.NET Core integration.
/// </summary>
public sealed class OQueryOptions
{
    /// <summary>
    /// The query string parameter name that contains the OQuery expression.
    /// Defaults to <c>"query"</c>.
    /// </summary>
    /// <example>GET /api/tickets?query=status = "open" SELECT assignee LIMIT 10</example>
    public string QueryParameterName { get; set; } = "query";
}