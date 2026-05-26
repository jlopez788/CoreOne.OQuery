using System.Text;

namespace CoreOne.OQuery.Lexer;

public class Lexer
{
    private readonly string _input;
    private readonly IOperatorProvider _operatorProvider;
    private int _pos;

    public Lexer(string input, IOperatorProvider? operatorProvider = null)
    {
        _input = input;
        _operatorProvider = operatorProvider ?? new DefaultOperatorProvider();
        _pos = 0;
    }

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();
        while (_pos < _input.Length)
        {
            char current = _input[_pos];

            if (char.IsWhiteSpace(current))
            {
                _pos++;
                continue;
            }

            if (current == '(')
            {
                tokens.Add(new Token(TokenType.LeftParen, "(", _pos++));
            }
            else if (current == ')')
            {
                tokens.Add(new Token(TokenType.RightParen, ")", _pos++));
            }
            else if (current == ',')
            {
                tokens.Add(new Token(TokenType.Comma, ",", _pos++));
            }
            else if (current == '.')
            {
                tokens.Add(new Token(TokenType.Dot, ".", _pos++));
            }
            else if (IsSymbolStart(current))
            {
                tokens.Add(ReadOperator());
            }
            else if (current == '"')
            {
                tokens.Add(ReadString());
            }
            else if (char.IsDigit(current))
            {
                tokens.Add(ReadNumber());
            }
            else if (char.IsLetter(current) || current == '_')
            {
                tokens.Add(ReadIdentifierOrKeyword());
            }
            else
            {
                throw new Exception($"Unexpected character '{current}' at position {_pos}");
            }
        }

        tokens.Add(new Token(TokenType.EndOfFile, "", _pos));
        return tokens;
    }

    private Token HandleSpecialLiterals(string value, int start)
    {
        if (value.Equals("guid", StringComparison.OrdinalIgnoreCase) && _pos < _input.Length && _input[_pos] == '\'')
        {
            _pos++;
            int litStart = _pos;
            while (_pos < _input.Length && _input[_pos] != '\'')
                _pos++;
            if (_pos >= _input.Length)
                throw new Exception("Unterminated guid literal");
            string litValue = _input.Substring(litStart, _pos - litStart);
            _pos++;
            return new Token(TokenType.Guid, litValue, start);
        }
        if (value.Equals("datetime", StringComparison.OrdinalIgnoreCase) && _pos < _input.Length && _input[_pos] == '\'')
        {
            _pos++;
            int litStart = _pos;
            while (_pos < _input.Length && _input[_pos] != '\'')
                _pos++;
            if (_pos >= _input.Length)
                throw new Exception("Unterminated datetime literal");
            string litValue = _input.Substring(litStart, _pos - litStart);
            _pos++;
            return new Token(TokenType.DateTime, litValue, start);
        }
        return new Token(TokenType.Identifier, value, start);
    }

    private bool IsSymbolStart(char c)
    {
        // Symbols that are not part of other tokens
        return "=!><~@#%^&|?:".Contains(c);
    }

    private char Peek() => _pos + 1 < _input.Length ? _input[_pos + 1] : '\0';

    private Token ReadIdentifierOrKeyword()
    {
        int start = _pos;
        while (_pos < _input.Length && (char.IsLetterOrDigit(_input[_pos]) || _input[_pos] == '_'))
        {
            _pos++;
        }
        string value = _input.Substring(start, _pos - start);
        string upperValue = value.ToUpper();

        if (_operatorProvider.IsOperator(upperValue))
        {
            return new Token(TokenType.Operator, value, start);
        }

        return upperValue switch {
            "AND" => new Token(TokenType.And, value, start),
            "OR" => new Token(TokenType.Or, value, start),
            "NOT" => new Token(TokenType.Not, value, start),
            "TRUE" => new Token(TokenType.Boolean, value, start),
            "FALSE" => new Token(TokenType.Boolean, value, start),
            "NULL" => new Token(TokenType.Null, value, start),
            "PAGE" => new Token(TokenType.Page, value, start),
            "PAGESIZE" => new Token(TokenType.PageSize, value, start),
            "LIMIT" => new Token(TokenType.Limit, value, start),
            "OFFSET" => new Token(TokenType.Offset, value, start),
            "SELECT" => new Token(TokenType.Select, value, start),
            _ => HandleSpecialLiterals(value, start)
        };
    }

    private Token ReadNumber()
    {
        int start = _pos;
        while (_pos < _input.Length && (char.IsDigit(_input[_pos]) || _input[_pos] == '.'))
        {
            _pos++;
        }
        return new Token(TokenType.Number, _input.Substring(start, _pos - start), start);
    }

    private Token ReadOperator()
    {
        int start = _pos;
        // Try 2-char operator
        if (_pos + 1 < _input.Length)
        {
            string twoCharOp = _input.Substring(_pos, 2);
            if (_operatorProvider.IsOperator(twoCharOp))
            {
                _pos += 2;
                return new Token(TokenType.Operator, twoCharOp, start);
            }
        }

        // Try 1-char operator
        string oneCharOp = _input.Substring(_pos, 1);
        if (_operatorProvider.IsOperator(oneCharOp))
        {
            _pos++;
            return new Token(TokenType.Operator, oneCharOp, start);
        }

        throw new Exception($"Unknown operator at position {start}");
    }

    private Token ReadString()
    {
        int start = _pos;
        _pos++; // skip "
        var sb = new StringBuilder();
        while (_pos < _input.Length && _input[_pos] != '"')
        {
            sb.Append(_input[_pos++]);
        }
        if (_pos >= _input.Length)
            throw new Exception("Unterminated string literal");
        _pos++; // skip "
        return new Token(TokenType.String, sb.ToString(), start);
    }
}