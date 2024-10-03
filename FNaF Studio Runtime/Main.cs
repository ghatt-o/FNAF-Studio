using FNAFStudio_Runtime_RCS.Data;
using FNAFStudio_Runtime_RCS.Data.CRScript;
using FNAFStudio_Runtime_RCS.Data.Definitions;
using FNAFStudio_Runtime_RCS.Menus;
using FNAFStudio_Runtime_RCS.Util;
using Raylib_CsLo;
using System.Diagnostics;

namespace FNAFStudio_Runtime_RCS
{
    public class Runtime
    {
        private readonly Stopwatch stopwatch = new();
        public static bool Cached = false;
        public Color FPSTextColor = new();

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CrashHandler.GlobalExceptionHandler;
            TaskScheduler.UnobservedTaskException += CrashHandler.TaskExceptionHandler;
            # region stuff
            string finalStr = "";
            bool debug = false;
            if (args.Length != 0)
            {
                finalStr = args[0];
                if (args.Length > 1 && args[1] == "/debugger:true")
                {
                    debug = true;
                }
            }
            if (finalStr == "")
            {
                finalStr = "assets";
            }
            debug = true; // while FS v3 is in dev
            GameState.DebugMode = debug;
            GameState.ProjectPath = AppDomain.CurrentDomain.BaseDirectory + finalStr;
            Runtime runtime = new();
            #endregion
            runtime.Run().HandleExceptions();
        }

        public async Task Run()
        {
            RuntimeUtils.Scene.LoadScenes();
            stopwatch.Start();
            Logger.Initialize();

            // Load game
            GameState.Project = GameJson.Game.Load(GameState.ProjectPath + "/game.json");

            // Init Raylib
            Raylib.InitWindow(1280, 720, GameState.Project.GameInfo.Title);
            Raylib.InitAudioDevice();
            RuntimeUtils.SetGameIcon(GameState.Project.GameInfo.Icon);
            //Raylib.SetTargetFPS(GameState.Project.game_info.fps); // Uncomment later, disabled for optimization
            Raylib.SetExitKey(KeyboardKey.KEY_ESCAPE);

            // Init Engine
            RuntimeUtils.Scene.SetScene(SceneType.Menu);
            MenuHandler.Startup();
            GameState.Clock.Start();

            while (!Raylib.WindowShouldClose())
            {
                if (Raylib.IsWindowFocused())
                    await UpdateAsync();

                Draw();
            }

            Raylib.CloseWindow();
        }

        public async Task UpdateAsync()
        {
            await GameState.CurrentScene.UpdateAsync();
            ScriptingAPI.TickEvents();
            await SoundPlayer.UpdateAsync();

            FPSTextColor = Raylib.GetFPS() switch
            {
                >= 100 => Raylib.GREEN,
                >= 60 => Raylib.WHITE,
                _ => Raylib.GREEN
            };
        }

        public void Draw()
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Raylib.BLACK);

            GameState.CurrentScene.Draw();

            if (!GameState.DebugMode)
            {
                Raylib.EndDrawing();
                return;
            }

            // TOO BIG!!!!
            Raylib.DrawText($"{Raylib.GetFPS()} FPS", 0, 0, 22, FPSTextColor);
            Raylib.DrawText($"Current Scene: {GameState.CurrentScene.Name}", 0, 22, 22, Raylib.WHITE);
            Raylib.DrawText("Debug Mode", 0, 44, 22, Raylib.WHITE);
            Raylib.EndDrawing();
        }
    }
}
