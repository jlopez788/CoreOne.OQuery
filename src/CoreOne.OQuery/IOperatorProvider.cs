namespace CoreOne.OQuery;

public interface IOperatorProvider
{
    bool IsOperator(string op);

    IEnumerable<string> GetOperators();
}
