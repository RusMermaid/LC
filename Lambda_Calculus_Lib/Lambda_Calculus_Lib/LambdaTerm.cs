
using System.Data;

namespace LambdaAnaliz;

public abstract record Expression;

public record Variable(
    string Name) : Expression;

public record Function(
    Variable Parameter,
    Expression Body) : Expression;

public record Application(
    Expression Function,
    Expression Argument) : Expression;

public abstract record Token;

public record LeftParentheses : Token;

public record Letter(
    string Value) : Token;

public record Lambda : Token;

public record Dot : Token;

public record RightParentheses : Token;

public static class Evaluator
{
    public static string Evaluate(this string program) => new string(program.Tokenize().Parse().Interpret().Unparse().Untokenize().ToArray());

    public static string Bruijn(this string program) => new string(program.Tokenize().Parse().ToBruijn().Unparse().Untokenize().ToArray());

    public static string BruijnBinary(this string program) => program.Tokenize().Parse().ToBruijn().ToBinary();
}

public static class Interpreter
{
    public static Expression Interpret(this Expression expression) => expression.Interpret(100);

    public static Expression Interpret(this Expression expression, int maxReductions) => expression.CanReduce() && (maxReductions >= 0) ? expression.Reduce().Interpret(--maxReductions) : expression;

    public static bool CanReduce(this Expression expression) => expression.HasBetaRedex();

    public static bool HasBetaRedex(this Expression expression)
    {
        return expression switch
        {
            Variable => false,
            Function function => function.Parameter.HasBetaRedex() || function.Body.HasBetaRedex(),
            Application application => application.Function switch
            {
                Function => true,
                _ => application.Function.HasBetaRedex() || application.Argument.HasBetaRedex()
            },
            _ => throw new SyntaxErrorException()
        };
    }

    public static Expression Reduce(this Expression expression)
    {
        return expression switch
        {
            Variable variable => variable,
            Function function => function with { Body = function.Body.Reduce() },
            Application application => application.Function switch
            {
                Function function => function.BetaReduce(application.Argument),
                Expression expression1 => new Application(expression1.Reduce(), application.Argument.Reduce())
            },
            _ => throw new SyntaxErrorException()
        };
    }

    public static Expression BetaReduce(this Function function, Expression argument) => Substitude(argument, function.Parameter, function.Body);

    public static Expression Substitude(Expression argument, Variable parameter, Expression body)
    {
        return body switch
        {
            Variable variable => parameter == body ? argument : variable,
            Function function => parameter == function.Parameter
                ? function
                : !function.Parameter.VariableOccursFree(argument)
                    ? function with { Body = Substitude(argument, parameter, function.Body) }
                    : Substitude(argument, parameter, function.AlphaConvert()),
            Application application => new Application(Substitude(argument, parameter, application.Function),
                Substitude(argument, parameter, application.Argument)),
            _ => throw new SyntaxErrorException()
        };
    }

    public static bool VariableOccursFree(this Variable variable, Expression expression)
    {
        return expression switch
        {
            Variable variable1 => variable == variable1,
            Application application => variable.VariableOccursFree(application.Function) || variable.VariableOccursFree(application.Argument),
            Function function => (function.Parameter != variable) && variable.VariableOccursFree(function.Body),
            _ => throw new SyntaxErrorException()
        };
    }

    public static Function AlphaConvert(this Function function)
    {
        return function.Parameter.NextVariableThatDoesntOccurInBody(function.Body)
            .Pipe(function: variable => new Function(variable, Convert(function.Parameter, variable, function.Body)));
    }

    public static Expression Convert(this Variable variableToConvertFrom, Variable variableToConvertTo, Expression expressionToConvert)
    {
        return expressionToConvert switch
        {
            Variable variableToConvert =>
                variableToConvert == variableToConvertFrom ? variableToConvertTo : variableToConvert,
            Application applicationToConvert =>
                new Application(Convert(variableToConvertFrom, variableToConvertTo, applicationToConvert.Function),
                    Convert(variableToConvertFrom, variableToConvertTo, applicationToConvert.Argument)),
            Function functionToConvert => (functionToConvert.Parameter == variableToConvertFrom) ||
                                          (functionToConvert.Parameter == variableToConvertTo)
                ? functionToConvert
                : new Function(functionToConvert.Parameter, Convert(variableToConvertFrom, variableToConvertTo, functionToConvert.Body)),
            _ => throw new SyntaxErrorException()
        };
    }

    public static Variable NextVariableThatDoesntOccurInBody(this Variable variable, Expression body)
    {
        return variable.NextVariable().Pipe(function: nextVariable =>
            IsVariableInBody(nextVariable, body) ? NextVariableThatDoesntOccurInBody(nextVariable, body) : nextVariable);
    }

    public static bool IsVariableInBody(this Variable variable, Expression body)
    {
        return body switch
        {
            Variable variable1 => variable == variable1,
            Application application => variable.IsVariableInBody(application.Function) || variable.IsVariableInBody(application.Argument),
            Function function => variable.IsVariableInBody(function.Parameter) || variable.IsVariableInBody(function.Body),
            _ => throw new SyntaxErrorException()
        };
    }

    public static Variable NextVariable(this Variable variable) => new Variable(variable.Name);

    public static char NextLowerCaseLetter(this char value)
    {
        return value switch
        {
            'z' => 'a',
            _ => value.NextLowerCaseLetter()
        };
    }
}

public static class BruijnConverter
{
    public static Expression ToBruijn(this Expression expression, List<char> context = null)
    {
        return expression switch
        {
            Variable variable => new DeBruijnIndex((context ?? new List<char>()).LastIndexOf(Convert.ToChar(variable.Name)) + 1),
            Function function => new Function(null,
                function.Body.ToBruijn(new List<char> { Convert.ToChar(function.Parameter.Name) }.Concat(context ?? Enumerable.Empty<char>()).ToList())),
            Application application => new Application(application.Function.ToBruijn(context), application.Argument.ToBruijn(context)),
            _ => throw new Exception()
        };
    }

    public static string ToBinary(this Expression expression)
    {
        return expression switch
        {
            DeBruijnIndex index => new string('1', index.Value) + "0",
            Function function => "00" + function.Body.ToBinary(),
            Application application => "01" + application.Function.ToBinary() + application.Argument.ToBinary(),
            _ => throw new Exception()
        };
    }

    public record DeBruijnIndex(
        int Value) : Expression;
}

public static class Parser
{
    public static Expression Parse(this IEnumerable<Token> tokens) => ParseRecursive(tokens).Expression;

    private static (Expression Expression, IEnumerable<Token> UnusedTokens) ParseRecursive(this IEnumerable<Token> tokens)
    {
        return tokens.First() switch
        {
            LeftParentheses => tokens.Skip(1).ParseRecursive().Pipe(function: left =>
                left.UnusedTokens.ParseRecursive()
                    .Pipe(function: right => (new Application(left.Expression, right.Expression), right.UnusedTokens.Skip(1)))),
            Letter letter => (new Variable(letter.Value), tokens.Skip(1)),
            Lambda => tokens.Skip(1).ParseRecursive().Pipe(function: parameter =>
                parameter.UnusedTokens.Skip(1).ParseRecursive()
                    .Pipe(function: body => (new Function((Variable)parameter.Expression, body.Expression), body.UnusedTokens))),
            _ => throw new Exception()
        };
    }

    public static IEnumerable<Token> Unparse(this Expression expression)
    {
        return expression switch
        {
            Variable variable => new List<Token> { new Letter(variable.Name) },
            Function function => new List<Token> { new Lambda() }
                .Concat(Unparse(function.Parameter))
                .Concat(new List<Token> { new Dot() })
                .Concat(Unparse(function.Body)),
            Application application => new List<Token> { new LeftParentheses() }
                .Concat(Unparse(application.Function))
                .Concat(Unparse(application.Argument))
                .Concat(new List<Token> { new RightParentheses() }),
            _ => throw new Exception()
        };
    }
}

public static class Lexer
{
    public static IEnumerable<Token> Tokenize(this IEnumerable<char> characters)
    {
        return characters.Select(character => (Token)(character switch
        {
            '(' => new LeftParentheses(),
            'λ' => new Lambda(),
            '.' => new Dot(),
            ')' => new RightParentheses(),
            _ when char.IsLower(character) => new Letter(character.ToString()),
            _ => throw new Exception()
        }));
    }

    public static IEnumerable<char> Untokenize(this IEnumerable<Token> tokens)
    {
        return (IEnumerable<char>)tokens.Select(selector: token => token is LeftParentheses ? '(' :
            token is Lambda ? 'λ' :
            token is Dot ? '.' :
            token is RightParentheses ? ')' :
            token is Letter letter ? letter.Value[0] : throw new SyntaxErrorException());
    }
}

public static class PipeExtensions
{
    public static TOutput Pipe<TInput, TOutput>(this TInput input, Func<TInput, TOutput> function) => function(input);
}