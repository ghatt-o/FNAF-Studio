using System.Globalization;
using System.Text.RegularExpressions;
using FNaFStudio_Runtime.Data.Definitions;
using FNaFStudio_Runtime.Util;
using Raylib_CsLo;

namespace FNaFStudio_Runtime.Data.CRScript;

public abstract partial class EventManager
{
    private static readonly List<Tuple<string, List<string>, List<GameJson.Code>>> Listeners = [];
    private static readonly Dictionary<string, Func<List<string>, bool>> CodeBlocks = new ScriptingApi().Actions;
    private static readonly Dictionary<string, string> Variables = [];
    public static readonly Dictionary<string, string> DataValues = [];
    private static readonly Lock LockObj = new();

    [GeneratedRegex(@"%var\((.*?)\)")]
    private static partial Regex VarRegex();
    
    [GeneratedRegex(@"%data\((.*?)\)")]
    private static partial Regex DataRegex();
    
    [GeneratedRegex(@"%math\((.*?)\)")]
    private static partial Regex MathRegex();
    
    [GeneratedRegex(@"%ai\((.*?)\)")]
    private static partial Regex AiRegex();
    
    [GeneratedRegex(@"%mouse\((x)\)")]
    private static partial Regex MouseXRegex();
    
    [GeneratedRegex(@"%mouse\((y)\)")]
    private static partial Regex MouseYRegex();
    
    [GeneratedRegex(@"%mouse\((.*?)\)")]
    private static partial Regex MouseRegex();
    
    [GeneratedRegex(@"%game\((.*?)\)")]
    private static partial Regex GameRegex();
    
    [GeneratedRegex(@"%random\(([^,]+),([^)]+)\)")]
    private static partial Regex RandRegex();

    private const string VariableNotFoundTemplate = "Variable '{0}' not found.";
    private const string DataValueNotFoundTemplate = "Data Value '{0}' not found.";
    private const string InvalidMouseExpression = "Invalid mouse expression";
    private const string GameNotImplemented = "Game expressions are not implemented";
    private const string ExpressionError = "ExprErr: Null Expression";

    private static void RegisterListener(string eventName, List<string> args, List<GameJson.Code> subcode)
    {
        Logger.LogAsync("EventManager", $"Registering Event: {eventName} With args: {string.Join(", ", args)}");
        var interval = 0;
        if (eventName == "every_num_ticks" && args.FirstOrDefault() != null &&
            int.TryParse(GetExpr(args.FirstOrDefault()), out interval) && interval > 0)
            GameState.Clock.OnEveryNumTicks(interval, () => TriggerEvent(eventName, args));
        else if (eventName == "every_num_ticks")
            Logger.LogErrorAsync("EventManager",
                "Invalid tick interval given for Event: 'every_num_ticks': " + interval);

        var listener = new Tuple<string, List<string>, List<GameJson.Code>>(eventName, args, subcode);
        lock (LockObj)
        {
            Listeners.Add(listener);
        }
    }

    public static void RunScript(List<GameJson.Code> script)
    {
        foreach (var code in script)
        {
            var args = new List<string>();
            if (code.Args.Count > 0)
                args = code.Args.Select(arg => arg.Trim()).ToList();
            RegisterListener(code.Block.ToLower(), args, code.Subcode);
        }
    }

    public static void KillListener(string eventName, List<string> args)
    {
        lock (LockObj)
        {
            Listeners.RemoveAll(listener => listener.Item1 == eventName && listener.Item2.SequenceEqual(args));
        }
    }

    public static void KillAllListeners()
    {
        int count;
        lock (LockObj)
        {
            count = Listeners.Count;
            Listeners.Clear();
        }

        Logger.LogAsync("EventManager", $"Killed {count} Listeners");
    }

    public static void RegisterCodeBlock(string blockName, Func<List<string>, bool> func)
    {
        lock (LockObj)
        {
            CodeBlocks[blockName] = func;
        }
    }

    public static void TriggerEvent(string eventName, List<string> args)
    {
        var snapshot = new List<(string, List<string>, List<GameJson.Code>)>();
    
        lock (LockObj)
        {
            foreach (var listener in Listeners)
            {
                if (listener.Item1.Equals(eventName, StringComparison.CurrentCultureIgnoreCase) 
                    && listener.Item2.SequenceEqual(args))
                {
                    snapshot.Add((listener.Item1, listener.Item2, listener.Item3));
                }
            }
        }

        foreach (var (_, _, codes) in snapshot)
        foreach (var code in codes)
            RunBlock(code);
    }

    private static void RunBlock(GameJson.Code code)
    {
        Func<List<string>, bool>? func;
        lock (LockObj)
        {
            if (!CodeBlocks.TryGetValue(code.Block.ToLower(), out func))
            {
               // Logger.LogAsync("EventManager", $"Function for code block '{code.Block.ToLower()}' not found");
                return;
            }
        }

        var result = func(code.Args);
        if (result)
            foreach (var subcode in code.Subcode)
                RunBlock(subcode);
    }

    public static string GetExpr(string? expression)
    {
        var result = ExpressionError;
        if (expression != null)
            result = ParseExpression(expression);
        return result;
    }

    private static string ParseExpression(string expression)
    {
        var result = expression;
        var reRandom = RandRegex();
        var rng = new Random();

        // Handle random expressions first
        result = reRandom.Replace(result, match =>
        {
            var a = int.Parse(GetExpr(match.Groups[1].Value).Trim());
            var b = int.Parse(GetExpr(match.Groups[2].Value).Trim());
            return rng.Next(a, b).ToString();
        });

        result = VarRegex().Replace(result, match =>
        {
            var content = match.Groups[1].Value;
            return Variables.TryGetValue(content, out var varValue)
                ? varValue
                : string.Format(VariableNotFoundTemplate, content);
        });

        result = DataRegex().Replace(result, match =>
        {
            var content = match.Groups[1].Value;
            return DataValues.TryGetValue(content, out var dataValue)
                ? dataValue
                : string.Format(DataValueNotFoundTemplate, content);
        });

        result = MathRegex().Replace(result, match =>
        {
            var content = match.Groups[1].Value;
            return EvaluateMathExpression(content);
        });

        result = AiRegex().Replace(result, match =>
        {
            var content = match.Groups[1].Value;
            return EvaluateAiExpression(content).ToString();
        });

        result = MouseXRegex().Replace(result, _ =>
            Raylib.GetMouseX().ToString());

        result = MouseYRegex().Replace(result, _ =>
            Raylib.GetMouseY().ToString());

        result = MouseRegex().Replace(result, match =>
        {
            var content = match.Groups[1].Value;
            return content switch
            {
                "x" => Raylib.GetMouseX().ToString(),
                "y" => Raylib.GetMouseY().ToString(),
                _ => InvalidMouseExpression
            };
        });

        result = GameRegex().Replace(result, match =>
            GameNotImplemented);

        return result;
    }

    public static void SetVariableValue(string name, string data)
    {
        lock (LockObj)
        {
            Variables[name] = data;
        }
    }

    public static void SetDataValue(string name, string data)
    {
        lock (LockObj)
        {
            DataValues[name] = data;
        }
    }

    private static string EvaluateMathExpression(string expression)
    {
        var result = MathEvaluator.Evaluate(expression);
        return result.ToString(CultureInfo.InvariantCulture);
    }

    private static int EvaluateAiExpression(string expression)
    {
        // TODO: Implement later
        return 0;
    }
}