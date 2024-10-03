using FNAFStudio_Runtime_RCS.Data;
using FNAFStudio_Runtime_RCS.Data.Definitions;
using Raylib_CsLo;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace FNAFStudio_Runtime_RCS.Util
{
    public class CrashHandler : IScene
    {
        public string Name => "CrashHandler";

        public static string title = "An error occurred during runtime execution";
        public static string errorMessage = "";

        public Task UpdateAsync()
        {
            return Task.CompletedTask;
        }

        public void Draw()
        {
            Raylib.ClearBackground(color: RuntimeUtils.ParseStringToColor("89", "157", "220"));
            Raylib.DrawTextEx(Cache.GetFont("Arial Bold", 20), title, new(64, 64), 26, 1, Raylib.WHITE);
            Raylib.DrawTextEx(Cache.GetFont("Arial", 20), errorMessage, new(64, 128), 20, 1, Raylib.WHITE);
        }

        public static void GlobalExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
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
            var method = ex.TargetSite;
            var declaringType = method?.DeclaringType?.FullName ?? "UnknownClass";
            var methodName = method?.Name ?? "UnknownMethod";

            var stackTrace = new StackTrace(ex, true);
            var customStackTrace = new StringBuilder();
            foreach (var frame in stackTrace.GetFrames().Where(f => IsRelevantFrame(f)))
            {
                customStackTrace.AppendLine($"{frame.GetMethod()?.DeclaringType?.FullName}.{frame.GetMethod()?.Name} in {(frame.GetFileName() ?? "").Split("/FNAFStudio-Raylib/")[1]}:line {frame.GetFileLineNumber()}");
            }

            errorMessage = $"Exception in {declaringType}.{methodName}: {ex.Message}\n"
                           + $"\nStack Trace:\n{customStackTrace}";

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
                CrashHandler.LogException(ex);

                // We cant use SetScene here because
                // Task exceptions are always fatal
                // so Draw() wont even get to run
                CrashHandler.SafeBSOD();
            }
        }
    }
}