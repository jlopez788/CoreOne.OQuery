namespace CoreOne.OQuery;

public class DefaultOperatorProvider : IOperatorProvider
{
    private readonly HashSet<string> _operators = new(StringComparer.OrdinalIgnoreCase)
    {
        "=", "!=", ">", ">=", "<", "<=", "IN"
    };

    public IEnumerable<string> GetOperators() => _operators;

    public bool IsOperator(string op) => _operators.Contains(op);
}

public class DefaultFunctionProvider : IFunctionProvider
{
    private readonly HashSet<string> _functions = new(StringComparer.OrdinalIgnoreCase)
    {
        "contains", "startsWith", "endsWith"
    };

    public bool IsFunction(string name) => _functions.Contains(name);
}