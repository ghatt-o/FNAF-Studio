using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Text;
using FNAFStudio_Runtime_RCS.Data;
using FNAFStudio_Runtime_RCS.Data.Definitions;
using Raylib_CsLo;

namespace FNAFStudio_Runtime_RCS.Util;

public class CrashHandler : IScene
{
    public static string title = "An error occurred during runtime execution";
    public static string errorMessage = "";
    public string Name => "CrashHandler";

    public Task UpdateAsync()
    {
        return Task.CompletedTask;
    }

    public void Draw()
    {
        Raylib.ClearBackground(RuntimeUtils.ParseStringToColor("89", "157", "220"));
        Raylib.DrawTextEx(Cache.GetFont("Arial Bold", 20), title, new Vector2(64, 64), 26, 1, Raylib.WHITE);
        Raylib.DrawTextEx(Cache.GetFont("Arial", 20), errorMessage, new Vector2(64, 128), 20, 1, Raylib.WHITE);
    }

    public static void GlobalExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = (Exception)e.ExceptionObject;
        LogException(ex);
    }

    public static void TaskExceptionHandler(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Exception ex = e.Exception;
        e.SetObserved();
        LogException(ex);
    }

    public static void LogException(Exception ex)
    {
        try
        {
            var declaringType = ex.TargetSite?.DeclaringType?.FullName ?? "UnknownClass";
            var methodName = ex.TargetSite?.Name ?? "UnknownMethod";

            var stackTrace = new StackTrace(ex, true);
            var customStackTrace = new StringBuilder();

            foreach (var frame in stackTrace.GetFrames()?.Where(IsRelevantFrame) ?? [])
            {
                var frameType = frame.GetMethod()?.DeclaringType?.FullName ?? "UnknownClass";
                var frameMethodName = frame.GetMethod()?.Name ?? "UnknownMethod";
                var filePath = frame.GetFileName()?.Split(["/FNaFStudio/"], StringSplitOptions.None).LastOrDefault() ?? "UnknownFile";
                customStackTrace.AppendLine($"{frameType}.{frameMethodName} in {filePath}:line {frame.GetFileLineNumber()}");
            }

            var errorMessage = $"Exception in {declaringType}.{methodName}: {ex.Message}\nStack Trace:\n{customStackTrace}";
            Logger.LogFatalAsync("CrashHandler", "\n" + errorMessage);
        }
        catch (Exception logEx)
        {
            Logger.LogErrorAsync("CrashHandler", $"Logging failed: {logEx.Message}");
        }
    }

    private static bool IsRelevantFrame(StackFrame frame)
    {
        var method = frame.GetMethod();
        return method != null && method.DeclaringType != null &&
               method.DeclaringType.Assembly == Assembly.GetExecutingAssembly();
    }

    public static void SafeBSOD()
    {
        // Note: this will NOT create a new window
        RuntimeUtils.Scene.SetScene(SceneType.CrashHandler);
        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Raylib.BLACK);
            GameState.CurrentScene.Draw();
            Raylib.EndDrawing();
        }
    }
}

public static class TaskExtensions
{
    public static void HandleExceptions(this Task task)
    {
        try
        {
            task.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            CrashHandler.LogException(ex);
            CrashHandler.SafeBSOD();
        }
    }
}