using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FNaFStudio_Runtime.Data.Definitions;
using Raylib_CsLo;

namespace FNaFStudio_Runtime.Util;

public static class Logger
{
    public enum LogLevel
    {
        Info,
        Warn,
        Error,
        Fatal,
        Debug
    }

    private static readonly string[] Splash =
    [
        @" __________________      _____  ___________ ___________ _______    ________.___ _______ ___________    ",
        @"\_   _____/\      \    /  _  \ \_   _____/ \_   _____/ \      \  /  _____/|   |\      \ \_   _____/    ",
        @" |    __)  /   |   \  /  /_\  \ |    __)    |    __)_  /   |   \/   \  ___|   |/   |   \ |    __)_     ",
        @" |     \  /    |    \/    |    \|     \     |        \/    |    \    \_\  \   /    |    \|        \    ",
        @" \___  /  \____|__  /\____|__  /\___  /    /_______  /\____|__  /\______  /___\____|__  /_______  /    ",
        @"     \/           \/         \/     \/             \/         \/        \/            \/        \/     ",
        @"                                   ____________________ ________                                       ",
        @"                                   \______   \______   \\_____  \                                      ",
        @"  ______   ______   ______   ______ |     ___/|       _/ /   |   \   ______   ______   ______   ______ ",
        @" /_____/  /_____/  /_____/  /_____/ |    |    |    |   \/    |    \ /_____/  /_____/  /_____/  /_____/ ",
        @"                                    |____|    |____|_  /\_______  /                                    ",
        @"                                                     \/         \/                                     "
    ]; // don't log that

    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    private static readonly ConsoleColor[] Colors =
    [
        ConsoleColor.Cyan, ConsoleColor.DarkYellow, ConsoleColor.Red, ConsoleColor.DarkRed, ConsoleColor.Yellow
    ];

    public static unsafe void Initialize()
    {
        Raylib.SetTraceLogLevel((int)TraceLogLevel.LOG_WARNING);
        Raylib.SetTraceLogCallback(&RaylibHook);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void RaylibHook(int logLevel, sbyte* text, sbyte* args)
    {
        //var message = Logger.GetLogMessage(new IntPtr(text), new IntPtr(args));

        var newLogLevel = (TraceLogLevel)logLevel switch
        {
            TraceLogLevel.LOG_ALL => LogLevel.Info,
            TraceLogLevel.LOG_TRACE => LogLevel.Warn,
            TraceLogLevel.LOG_DEBUG => LogLevel.Warn,
            TraceLogLevel.LOG_INFO => LogLevel.Info,
            TraceLogLevel.LOG_WARNING => LogLevel.Warn,
            TraceLogLevel.LOG_ERROR => LogLevel.Error,
            TraceLogLevel.LOG_FATAL => LogLevel.Fatal,
            TraceLogLevel.LOG_NONE => LogLevel.Info,
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
        };

        LogCustomAsync(newLogLevel, "Raylib", RuntimeUtils.SBytePointerToString(text)).Wait();
    }

    private static async Task LogCustomAsync(LogLevel logLevel, string module, string message, bool tofiles = true)
    {
        await Semaphore.WaitAsync();
        try
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"{timestamp} ");
            Console.ForegroundColor = Colors[(int)logLevel];
            Console.Write($"{logLevel} ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("(");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{module}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"): {message}");

            if (tofiles)
            {
                await using StreamWriter writer = new("engine.log", true);
                await writer.WriteLineAsync($"{timestamp} {logLevel} ({module}): {message}");
            }
        }
        finally
        {
            Semaphore.Release();
        }

        if (logLevel == LogLevel.Fatal)
        {
            CrashHandler.ErrorMessage = message;
            RuntimeUtils.Scene.SetScene(SceneType.CrashHandler);
        }
    }

    public static async Task<bool> LineAsync(string module, string message)
    {
        await LogCustomAsync(LogLevel.Info, module, message);
        return true;
    }

    public static Task LogAsync(string module, string message)
    {
        return LogCustomAsync(LogLevel.Info, module, message);
    }

    public static Task LogErrorAsync(string module, string message)
    {
        return LogCustomAsync(LogLevel.Error, module, message);
    }

    public static Task LogFatalAsync(string module, string message)
    {
        return LogCustomAsync(LogLevel.Fatal, module, message);
    }

    public static Task LogWarnAsync(string module, string message)
    {
        return LogCustomAsync(LogLevel.Warn, module, message);
    }

    public static async Task DrawSplashAsync()
    {
        await Semaphore.WaitAsync();
        try
        {
            await using StreamWriter writer = new("engine.log", true);
            Console.Clear();
            foreach (var line in Splash)
            {
                Console.WriteLine(line);
                await writer.WriteLineAsync(line);
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }
}