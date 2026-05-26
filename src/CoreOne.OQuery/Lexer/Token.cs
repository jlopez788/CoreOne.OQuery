namespace CoreOne.OQuery.Lexer;

public class Token
{
    public int Position { get; }
    public TokenType Type { get; }
    public string Value { get; }

    public Token(TokenType type, string value, int position)
    {
        Type = type;
        Value = value;
        Position = position;
    }

    public override string ToString() => $"{Type}: {Value}";
}