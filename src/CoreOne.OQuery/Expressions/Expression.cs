using System.Text.Json.Serialization;

namespace CoreOne.OQuery.Expressions;

[JsonDerivedType(typeof(BinaryExpression), "binary")]
[JsonDerivedType(typeof(UnaryExpression), "unary")]
[JsonDerivedType(typeof(FunctionCallExpression), "function")]
[JsonDerivedType(typeof(IdentifierExpression), "identifier")]
[JsonDerivedType(typeof(LiteralExpression), "literal")]
public abstract class Expression : Node { }
