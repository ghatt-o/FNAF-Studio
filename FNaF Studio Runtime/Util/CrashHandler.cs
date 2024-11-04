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
        if (ex.TargetSite != null)
        {
            var method = ex.TargetSite;
            var declaringType = method?.DeclaringType?.FullName ?? "UnknownClass";
            var methodName = method?.Name ?? "UnknownMethod";

            var stackTrace = new StackTrace(ex, true);
            var customStackTrace = new StringBuilder();
            foreach (var frame in stackTrace.GetFrames().Where(f => IsRelevantFrame(f)))
                customStackTrace.AppendLine(
                    $"{frame.GetMethod()?.DeclaringType?.FullName}.{frame.GetMethod()?.Name} in {(frame.GetFileName() ?? "").Split("/FNAFStudio/")[1]}:line {frame.GetFileLineNumber()}");

            errorMessage = $"Exception in {declaringType}.{methodName}: {ex.Message}\n"
                           + $"\nStack Trace:\n{customStackTrace}";
        }

        Logger.LogFatalAsync("CrashHandler", "\n" + errorMessage);
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
    public static async void HandleExceptions(this Task task)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            try
            {
                // CrashHandler itself is crashing
                // how ironic
                CrashHandler.LogException(ex);
            }
            catch
            {
                // ignored
            }

            // We cant use SetScene here because
            // Task exceptions are always fatal
            // so Draw() wont even get to run
            CrashHandler.SafeBSOD();
        }
    }
}