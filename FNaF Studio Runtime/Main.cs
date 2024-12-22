using System.Diagnostics;
using FNaFStudio_Runtime.Data;
using FNaFStudio_Runtime.Data.CRScript;
using FNaFStudio_Runtime.Data.Definitions;
using FNaFStudio_Runtime.Menus;
using FNaFStudio_Runtime.Util;
using Microsoft.Toolkit.HighPerformance.Buffers;
using Raylib_CsLo;
using Raylib_CsLo.InternalHelpers;
using System.Threading;

namespace FNaFStudio_Runtime;

public class Runtime
{
    public Color FPSTextColor;
    private readonly ManualResetEvent updateSignal = new(false);
    private readonly ManualResetEvent mainSignal = new(false);
    private bool isRunning = true;

    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += CrashHandler.GlobalExceptionHandler;
        TaskScheduler.UnobservedTaskException += CrashHandler.TaskExceptionHandler;

        #region stuff

        var finalStr = "";
        var debug = false;
        if (args.Length != 0)
        {
            finalStr = args[0];
            if (args.Length > 1 && args[1] == "/debugger:true") debug = true;
        }

        if (finalStr == "") finalStr = "assets";
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
        Logger.Initialize();

        if (!Directory.Exists(GameState.ProjectPath))
            await Logger.LogFatalAsync("Main", "ProjectPath " + GameState.ProjectPath + " doesn't exist!");
        GameState.Project = GameJson.Game.Load(GameState.ProjectPath + "/game.json");

        Raylib.InitWindow(1280, 720, GameState.Project.GameInfo.Title);
        Raylib.InitAudioDevice();
        RuntimeUtils.SetGameIcon(GameState.Project.GameInfo.Icon);

        unsafe
        {
            Shader shader = Raylib.LoadShader(null, "RPanorama.glsl");
            Raylib.SetShaderValue(shader, Raylib.GetShaderLocation(shader, "fPixelHeight"), 0.065f, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
            Raylib.SetShaderValue(shader, Raylib.GetShaderLocation(shader, "zoom"), 4, ShaderUniformDataType.SHADER_UNIFORM_INT);
            Raylib.SetShaderValue(shader, Raylib.GetShaderLocation(shader, "noWrap"), 0, ShaderUniformDataType.SHADER_UNIFORM_INT);
            GameCache.PanoramaShader = shader;
        }

        RuntimeUtils.Scene.SetScene(SceneType.Menu);
        foreach (var menu in GameState.Project.Menus)
            if (MenusCore.ConvertMenuToAPI(menu.Value) is { } newMenu)
                MenusCore.Menus.Add(menu.Key, newMenu);
        MenuUtils.GotoMenu(GameState.Project.Menus.ContainsKey("Warning") ? "Warning" : "Main");
        GameState.Clock.Start();

        Thread updateThread = new(UpdateLoop);
        updateThread.Start();

        while (!Raylib.WindowShouldClose())
        {
            if (Raylib.IsWindowFocused())
            {
                updateSignal.Set();
                foreach (var button in GameCache.Buttons.Values) 
                    button?.Update(GameState.ScrollX);
            }

            SoundPlayer.Update();
            mainSignal.WaitOne();
            Draw();
        }

        isRunning = false;
        updateSignal.Set();
        updateThread.Join();

        Raylib.CloseWindow();
    }

    private void UpdateLoop()
    {
        while (isRunning)
        {
            updateSignal.WaitOne();
            updateSignal.Reset();

            GameState.Clock.Update();
            ScriptingAPI.TickEvents();
            GameState.CurrentScene.Update();

            FPSTextColor = Raylib.GetFPS() switch
            {
                >= 100 => Raylib.GREEN,
                >= 60 => Raylib.WHITE,
                _ => Raylib.RED
            };

            mainSignal.Set();
        }
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

        Raylib.DrawText($"{Raylib.GetFPS()} FPS", 0, 0, 22, FPSTextColor);
        Raylib.DrawText($"Current Scene: {GameState.CurrentScene.Name}", 0, 22, 22, Raylib.WHITE);
        Raylib.DrawText("Debug Mode", 0, 44, 22, Raylib.WHITE);
        Raylib.EndDrawing();
    }
}
