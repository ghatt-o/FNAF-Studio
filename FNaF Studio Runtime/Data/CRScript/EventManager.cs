using FNAFStudio_Runtime_RCS.Data.Definitions;
using FNAFStudio_Runtime_RCS.Util;
using Raylib_CsLo;
using System.Text.RegularExpressions;

namespace FNAFStudio_Runtime_RCS.Data.CRScript
{
    public partial class EventManager
    {
        private static readonly List<Tuple<string, List<string>, List<GameJson.Code>>> listeners = [];
        private static readonly Dictionary<string, Func<List<string>, bool>> codeBlocks = new ScriptingAPI().Actions;
        private static readonly Dictionary<string, string> variables = [];
        public static readonly Dictionary<string, string> dataValues = [];
        private static readonly object lockObj = new();

        public static void RegisterListener(string eventName, List<string> args, List<GameJson.Code> subcode)
        {
            Logger.LogAsync("EventManager", $"Registering Event: {eventName} With args: {string.Join(", ", args)}");
            int interval = 0;
            if (eventName == "every_num_ticks" && args.FirstOrDefault() != null &&
                int.TryParse(GetExpr(args.FirstOrDefault()), out interval) && interval > 0)
            {
                GameState.Clock.OnEveryNumTicks(interval, () => TriggerEvent(eventName, args));
            }
            else if (eventName == "every_num_ticks")
            {
                Logger.LogErrorAsync("EventManager", "Invalid tick interval given for Event: 'every_num_ticks': " + interval);
            }

            var listener = new Tuple<string, List<string>, List<GameJson.Code>>(eventName, args, subcode);
            lock (lockObj)
            {
                listeners.Add(listener);
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
            lock (lockObj)
            {
                listeners.RemoveAll(listener => listener.Item1 == eventName && listener.Item2.SequenceEqual(args));
            }
        }

        public static void KillAllListeners()
        {
            int count;
            lock (lockObj)
            {
                count = listeners.Count;
                listeners.Clear();
            }
            Logger.LogAsync("EventManager", $"Killed {count} Listeners");
        }

        public static void RegisterCodeBlock(string blockName, Func<List<string>, bool> func)
        {
            lock (lockObj)
            {
                codeBlocks[blockName] = func;
            }
        }

        public static void TriggerEvent(string eventName, List<string> args)
        {
            List<Tuple<string, List<string>, List<GameJson.Code>>> listenersCopy;
            lock (lockObj)
            {
                listenersCopy = listeners
                    .Select(listener => new Tuple<string, List<string>, List<GameJson.Code>>(listener.Item1, [.. listener.Item2], [.. listener.Item3]))
                    .ToList();
            }

            foreach (var listener in listenersCopy)
            {
                if (listener.Item1.Equals(eventName, StringComparison.CurrentCultureIgnoreCase) && listener.Item2.SequenceEqual(args))
                {
                    foreach (var code in listener.Item3)
                    {
                        RunBlock(code);
                    }
                }
            }
        }

        public static void RunBlock(GameJson.Code code)
        {
            Func<List<string>, bool>? func;
            lock (lockObj)
            {
                if (!codeBlocks.TryGetValue(code.Block.ToLower(), out func))
                {
                    Logger.LogAsync("EventManager", $"Function for code block '{code.Block.ToLower()}' not found");
                    return;
                }
            }

            bool result = func(code.Args);
            if (result)
            {
                foreach (var subcode in code.Subcode)
                {
                    RunBlock(subcode);
                }
            }
        }

        public static string GetExpr(string? expression)
        {
            string result = "ExprErr: Null Expression";
            if (expression != null)
                result = ParseExpression(expression);
            return result;
        }

        public static string ParseExpression(string expression)
        {
            var result = expression;
            var reRandom = RandRegex();
            var rng = new Random();

            result = reRandom.Replace(result, match =>
            {
                var a = int.Parse(GetExpr(match.Groups[1].Value).Trim());
                var b = int.Parse(GetExpr(match.Groups[2].Value).Trim());
                return rng.Next(a, b).ToString();
            });

            var patterns = new Dictionary<string, string>
            {
                { "var", @"%var\((.*?)\)" },
                { "data", @"%data\((.*?)\)" },
                { "math", @"%math\((.*?)\)" },
                { "ai", @"%ai\((.*?)\)" },
                { "mousex", @"%mouse\((x)\)" },
                { "mousey", @"%mouse\((y)\)" },
                { "mouse", @"%mouse\((.*?)\)" },
                { "game", @"%game\((.*?)\)" }
            };

            foreach (var kvp in patterns)
            {
                var re = new Regex(kvp.Value);
                result = re.Replace(result, match =>
                {
                    var content = match.Groups[1].Value;
                    return kvp.Key switch
                    {
                        "var" => variables.TryGetValue(content, out var varValue) ? varValue : $"Variable '{content}' not found.",
                        "data" => dataValues.TryGetValue(content, out var dataValue) ? dataValue : $"Data Value '{content}' not found.",
                        "math" => EvaluateMathExpression(content),
                        "ai" => EvaluateAIExpression(content).ToString(),
                        "mousex" => Raylib.GetMouseX().ToString(),
                        "mousey" => Raylib.GetMouseY().ToString(),
                        "mouse" => "Invalid mouse expression",
                        "game" => $"Game expressions are not implemented",
                        _ => $"Unknown expression type: {kvp.Key}",
                    };
                });
            }

            return result;
        }

        public static void SetVariableValue(string name, string data)
        {
            lock (lockObj)
            {
                variables[name] = data;
            }
        }

        public static void SetDataValue(string name, string data)
        {
            lock (lockObj)
            {
                dataValues[name] = data;
            }
        }

        public static string EvaluateMathExpression(string expression)
        {
            var result = MathEvaluator.Evaluate(expression);
            return result.ToString();
        }

        public static int EvaluateAIExpression(string expression) =>
            // TODO: Implement later
            0;

        [GeneratedRegex(@"%random\(([^,]+),([^)]+)\)")]
        private static partial Regex RandRegex();
    }
}