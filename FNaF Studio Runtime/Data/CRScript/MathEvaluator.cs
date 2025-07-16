namespace FNaFStudio_Runtime.Data.CRScript;

public enum TokenType
{
    Number,
    Operator,
    Function,
    ParenL,
    ParenR
}

public class Token
{
    public Token(double number)
    {
        Type = TokenType.Number;
        NumberValue = number;
        FunctionValue = string.Empty;
    }

    public Token(char operatorChar)
    {
        Type = TokenType.Operator;
        OperatorValue = operatorChar;
        FunctionValue = string.Empty;
    }

    public Token(string function)
    {
        Type = TokenType.Function;
        FunctionValue = function;
    }

    private Token(TokenType type)
    {
        Type = type;
        FunctionValue = string.Empty;
    }

    public TokenType Type { get; }
    public double NumberValue { get; }
    public char OperatorValue { get; }
    public string FunctionValue { get; }

    public static Token ParenLeft => new(TokenType.ParenL);
    public static Token ParenRight => new(TokenType.ParenR);
}

public abstract class MathEvaluator
{
    public static double Evaluate(string expression)
    {
        try
        {
            var tokens = Tokenize(expression);
            var ast = Parse(tokens);
            return EvaluateAst(ast);
        }
        catch
        {
            return 0.0;
        }
    }

    private static List<Token> Tokenize(string expression)
    {
        var tokens = new List<Token>();
        var chars = expression.GetEnumerator();
        using var chars1 = chars as IDisposable;
        while (chars.MoveNext())
        {
            var c = chars.Current;
            switch (c)
            {
                case ' ':
                    continue;
                case '+':
                case '*':
                case '/':
                    tokens.Add(new Token(c));
                    break;
                case '-':
                    if (tokens.Count == 0 || tokens[^1].Type == TokenType.Operator ||
                        tokens[^1].Type == TokenType.ParenL)
                    {
                        var num = "-";
                        while (chars.MoveNext() && (char.IsDigit(chars.Current) || chars.Current == '.'))
                            num += chars.Current;
                        tokens.Add(new Token(double.Parse(num)));
                    }
                    else
                    {
                        tokens.Add(new Token(c));
                    }

                    break;
                case '(':
                    tokens.Add(Token.ParenLeft);
                    break;
                case ')':
                    tokens.Add(Token.ParenRight);
                    break;
                default:
                    if (char.IsDigit(c))
                    {
                        var num = c.ToString();
                        while (chars.MoveNext() && (char.IsDigit(chars.Current) || chars.Current == '.'))
                            num += chars.Current;
                        tokens.Add(new Token(double.Parse(num)));
                    }
                    else if (char.IsLetter(c))
                    {
                        var func = c.ToString();
                        while (chars.MoveNext() && char.IsLetter(chars.Current)) func += chars.Current;
                        if (func == "sin" || func == "cos")
                            tokens.Add(new Token(func));
                        else
                            throw new Exception("Unknown function");
                    }
                    else
                    {
                        throw new Exception("Unexpected character");
                    }

                    break;
            }
        }

        return tokens;
    }

    private static List<Token> Parse(List<Token> tokens)
    {
        var output = new List<Token>();
        var operators = new Stack<Token>();
        var i = 0;

        while (i < tokens.Count)
        {
            var token = tokens[i];
            switch (token.Type)
            {
                case TokenType.Number:
                case TokenType.Function:
                    output.Add(token);
                    break;
                case TokenType.Operator:
                    while (operators.Count > 0 && Precedence(operators.Peek()) >= Precedence(token))
                        output.Add(operators.Pop());
                    operators.Push(token);
                    break;
                case TokenType.ParenL:
                    operators.Push(token);
                    break;
                case TokenType.ParenR:
                    while (operators.Peek().Type != TokenType.ParenL) output.Add(operators.Pop());
                    operators.Pop();
                    break;
                default:
                    throw new Exception("Unexpected token");
            }

            i++;
        }

        while (operators.Count > 0) output.Add(operators.Pop());

        return output;
    }

    private static int Precedence(Token token)
    {
        return token.Type switch
        {
            TokenType.Operator when token.OperatorValue == '+' || token.OperatorValue == '-' => 1,
            TokenType.Operator when token.OperatorValue == '*' || token.OperatorValue == '/' => 2,
            _ => 0
        };
    }

    private static double EvaluateAst(List<Token> tokens)
    {
        var stack = new Stack<double>();

        foreach (var token in tokens)
            switch (token.Type)
            {
                case TokenType.Number:
                    stack.Push(token.NumberValue);
                    break;
                case TokenType.Operator:
                    var b = stack.Pop();
                    var a = stack.Pop();
                    stack.Push(token.OperatorValue switch
                    {
                        '+' => a + b,
                        '-' => a - b,
                        '*' => a * b,
                        '/' => a / b,
                        _ => throw new Exception("Unexpected operator")
                    });
                    break;
                case TokenType.Function:
                    var arg = stack.Pop();
                    stack.Push(token.FunctionValue switch
                    {
                        "sin" => Math.Sin(arg * Math.PI / 180.0),
                        "cos" => Math.Cos(arg * Math.PI / 180.0),
                        _ => throw new Exception("Unknown function")
                    });
                    break;
                default:
                    throw new Exception("Unexpected token");
            }

        return stack.Pop();
    }
}