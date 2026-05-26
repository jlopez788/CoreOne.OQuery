namespace CoreOne.OQuery.Lexer;

public enum TokenType
{
    Identifier,
    String,
    Number,
    Boolean,
    Null,
    Guid,
    DateTime,
    Operator,
    LeftParen,
    RightParen,
    Comma,
    Dot,
    And,
    Or,
    Not,
    In,
    Page,
    PageSize,
    Limit,
    Offset,
    Select,
    EndOfFile
}