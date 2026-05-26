using CoreOne.OQuery.Expressions;
using CoreOne.OQuery.Lexer;

namespace CoreOne.OQuery.Parser;

public class Parser(List<Token> tokens, IFunctionProvider? functionProvider = null)
{
    private readonly IFunctionProvider _functionProvider = functionProvider ?? new DefaultFunctionProvider();
    private readonly List<Token> _tokens = tokens;
    private int _current = 0;

    public QueryNode Parse()
    {
        var query = new QueryNode();
        if (!IsSelectKeyword(Peek().Type) && !IsPaginationKeyword(Peek().Type) && Peek().Type != TokenType.EndOfFile)
        {
            query.Expression = ParseExpression();
        }

        if (IsSelectKeyword(Peek().Type))
        {
            query.Select = ParseSelectClause();
        }

        if (IsPaginationKeyword(Peek().Type))
        {
            query.Pagination = ParsePagination();
        }

        return query;
    }

    private static bool IsPaginationKeyword(TokenType type) => type == TokenType.Page || type == TokenType.Limit;

    private static bool IsSelectKeyword(TokenType type) => type == TokenType.Select;

    private Token Advance()
    {
        if (!IsAtEnd())
            _current++;
        return Previous();
    }

    private bool Check(TokenType type)
    {
        if (IsAtEnd())
            return false;
        return Peek().Type == type;
    }

    private Token Consume(TokenType type, string message)
    {
        if (Check(type))
            return Advance();
        throw new Exception($"{message} at position {Peek().Position}");
    }

    private bool IsAtEnd() => Peek().Type == TokenType.EndOfFile;

    private bool Match(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }
        return false;
    }

    private Expression ParseComparison()
    {
        Expression expr = ParseTerm();

        if (Match(TokenType.Operator))
        {
            string op = Previous().Value;
            Expression right = ParseTerm();
            return new BinaryExpression { Left = expr, Operator = op.ToUpper(), Right = right };
        }

        return expr;
    }

    private Expression ParseExpression()
    {
        return ParseLogicalOr();
    }

    private Expression ParseFunctionCall()
    {
        Token name = Consume(TokenType.Identifier, "Expect function name.");
        if (!_functionProvider.IsFunction(name.Value))
        {
            // We still parse it as a function call, but the evaluator might fail if it doesn't know it.
            // Or we could throw here if we want strictness.
        }
        Consume(TokenType.LeftParen, "Expect '(' after function name.");
        var arguments = new List<Expression>();
        if (Peek().Type != TokenType.RightParen)
        {
            do
            {
                arguments.Add(ParseExpression());
            } while (Match(TokenType.Comma));
        }
        Consume(TokenType.RightParen, "Expect ')' after arguments.");
        return new FunctionCallExpression {
            Identifier = name.Value,
            Arguments = arguments
        };
    }

    private Expression ParseLogicalAnd()
    {
        Expression expr = ParseUnary();

        while (Match(TokenType.And))
        {
            string op = Previous().Value;
            Expression right = ParseUnary();
            expr = new BinaryExpression { Left = expr, Operator = op.ToUpper(), Right = right };
        }

        return expr;
    }

    private Expression ParseLogicalOr()
    {
        Expression expr = ParseLogicalAnd();

        while (Match(TokenType.Or))
        {
            string op = Previous().Value;
            Expression right = ParseLogicalAnd();
            expr = new BinaryExpression { Left = expr, Operator = op.ToUpper(), Right = right };
        }

        return expr;
    }

    private Expression ParseMemberAccess()
    {
        Token first = Consume(TokenType.Identifier, "Expect identifier.");
        string path = first.Value;
        while (Match(TokenType.Dot))
        {
            Token next = Consume(TokenType.Identifier, "Expect identifier after '.'.");
            path += "." + next.Value;
        }
        return new IdentifierExpression { Name = path };
    }

    private PaginationNode ParsePagination()
    {
        var node = new PaginationNode();
        if (Match(TokenType.Page))
        {
            node.Page = int.Parse(Consume(TokenType.Number, "Expect page number.").Value);
            Consume(TokenType.PageSize, "Expect 'pageSize' after page number.");
            node.PageSize = int.Parse(Consume(TokenType.Number, "Expect page size.").Value);
        }
        else if (Match(TokenType.Limit))
        {
            node.Limit = int.Parse(Consume(TokenType.Number, "Expect limit value.").Value);
            if (Match(TokenType.Offset))
            {
                node.Offset = int.Parse(Consume(TokenType.Number, "Expect offset value.").Value);
            }
        }
        return node;
    }

    private Expression ParsePrimary()
    {
        if (Match(TokenType.LeftParen))
        {
            Expression expr = ParseExpression();
            Consume(TokenType.RightParen, "Expect ')' after expression.");
            return expr;
        }

        return ParseComparison();
    }

    private SelectNode ParseSelectClause()
    {
        Advance(); // consume SELECT token
        var node = new SelectNode();
        do
        {
            var field = Consume(TokenType.Identifier, "Expect field name after SELECT.");
            node.Fields.Add(field.Value);
        } while (Match(TokenType.Comma));
        return node;
    }

    private Expression ParseTerm()
    {
        if (Match(TokenType.Boolean, TokenType.Number, TokenType.String, TokenType.Null, TokenType.Guid, TokenType.DateTime))
        {
            return new LiteralExpression {
                Value = Previous().Value,
                LiteralType = Previous().Type.ToString().ToLower()
            };
        }

        if (Peek().Type == TokenType.Identifier)
        {
            if (PeekNext().Type == TokenType.LeftParen)
            {
                return ParseFunctionCall();
            }
            return ParseMemberAccess();
        }

        if (Match(TokenType.LeftParen))
        {
            List<Expression> elements = new();
            if (Peek().Type != TokenType.RightParen)
            {
                do
                {
                    elements.Add(ParseTerm());
                } while (Match(TokenType.Comma));
            }
            Consume(TokenType.RightParen, "Expect ')' after list elements.");
            return new LiteralExpression { Value = elements, LiteralType = "list" };
        }

        throw new Exception($"Unexpected token {Peek().Type} at position {Peek().Position}");
    }

    private Expression ParseUnary()
    {
        if (Match(TokenType.Not))
        {
            string op = Previous().Value;
            Expression operand = ParseUnary();
            return new UnaryExpression {
                Operator = op.ToUpper(),
                Operand = operand
            };
        }

        return ParsePrimary();
    }

    private Token Peek() => _tokens[_current];

    private Token PeekNext() => _current + 1 < _tokens.Count ? _tokens[_current + 1] : _tokens[^1];

    private Token Previous() => _tokens[_current - 1];
}