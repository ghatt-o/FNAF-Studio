using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FNAFStudio_Runtime_RCS.Data.Definitions;
using Raylib_CsLo;

namespace FNAFStudio_Runtime_RCS.Util;

public static class Logger
{
    public enum LogLevel
    {
        INFO,
        WARN,
        ERROR,
        FATAL,
        DEBUG
    }

    private static readonly string[] SPLASH =
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

    private static readonly SemaphoreSlim semaphore = new(1, 1);

    private static readonly ConsoleColor[] colors =
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
            TraceLogLevel.LOG_ALL => LogLevel.INFO,
            TraceLogLevel.LOG_TRACE => LogLevel.WARN,
            TraceLogLevel.LOG_DEBUG => LogLevel.WARN,
            TraceLogLevel.LOG_INFO => LogLevel.INFO,
            TraceLogLevel.LOG_WARNING => LogLevel.WARN,
            TraceLogLevel.LOG_ERROR => LogLevel.ERROR,
            TraceLogLevel.LOG_FATAL => LogLevel.FATAL,
            TraceLogLevel.LOG_NONE => LogLevel.INFO,
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
        };

        LogCustomAsync(newLogLevel, "Raylib", RuntimeUtils.SBytePointerToString(text)).Wait();
    }

    public static async Task LogCustomAsync(LogLevel logLevel, string module, string message, bool tofiles = true)
    {
        await semaphore.WaitAsync();
        try
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"{timestamp} ");
            Console.ForegroundColor = colors[(int)logLevel];
            Console.Write($"{logLevel} ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("(");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{module}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"): {message}");

            if (tofiles)
            {
                using StreamWriter writer = new("engine.log", true);
                await writer.WriteLineAsync($"{timestamp} {logLevel} ({module}): {message}");
            }
        }
        finally
        {
            semaphore.Release();
        }

        if (logLevel == LogLevel.FATAL)
        {
            CrashHandler.errorMessage = message;
            RuntimeUtils.Scene.SetScene(SceneType.CrashHandler);
        }
    }

    public static async Task<bool> LineAsync(string module, string message)
    {
        await LogCustomAsync(LogLevel.INFO, module, message);
        return true;
    }

    public static Task LogAsync(string module, string message)
    {
        return LogCustomAsync(LogLevel.INFO, module, message);
    }

    public static Task LogErrorAsync(string module, string message)
    {
        return LogCustomAsync(LogLevel.ERROR, module, message);
    }

    public static Task LogFatalAsync(string module, string message)
    {
        return LogCustomAsync(LogLevel.FATAL, module, message);
    }

    public static Task LogWarnAsync(string module, string message)
    {
        return LogCustomAsync(LogLevel.WARN, module, message);
    }

    public static async Task DrawSplashAsync()
    {
        await semaphore.WaitAsync();
        try
        {
            using StreamWriter writer = new("engine.log", true);
            Console.Clear();
            foreach (var line in SPLASH)
            {
                Console.WriteLine(line);
                await writer.WriteLineAsync(line);
            }
        }
        finally
        {
            semaphore.Release();
        }
    }
}