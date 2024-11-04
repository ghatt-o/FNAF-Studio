using System.Text;
using FNAFStudio_Runtime_RCS.Data;
using FNAFStudio_Runtime_RCS.Data.Definitions;
using FNAFStudio_Runtime_RCS.Menus;
using FNAFStudio_Runtime_RCS.Office.Definitions;
using FNAFStudio_Runtime_RCS.Office.Scenes;
using Raylib_CsLo;

namespace FNAFStudio_Runtime_RCS.Util;

public static class RuntimeUtils
{
    /// <summary>
    ///     Sets the window icon for the Runtime from an image in assets.
    /// </summary>
    /// <param name="image">The name or path of the image to be used as the icon.</param>
    public static void SetGameIcon(string image)
    {
        return; // Raylib.IsTextureReady isn't in this version... TODO: Replace that function
        if (string.IsNullOrEmpty(image))
        {
            Logger.LogWarnAsync("RuntimeUtils", "Game Icon image path is null or empty.");
            return;
        }

        // DON'T RUN THIS CODE BELOW!!
        var texture = Cache.GetTexture(image);
        Raylib.SetWindowIcon(Raylib.LoadImageFromTexture(texture));
        Logger.LogErrorAsync("RuntimeUtils", $"Texture for image '{image}' not found.");
    }

    /// <summary>
    ///     Terminates the Runtime with an optional exit code.
    /// </summary>
    /// <param name="exitCode">The exit code to return on Runtime termination. Defaults to 0.</param>
    /// <returns>Always returns true after termination.</returns>
    public static bool Quit(int exitCode = 0)
    {
        Environment.Exit(exitCode);
        return true;
    }

    /// <summary>
    ///     Draws the Static Effect.
    /// </summary>
    public static void DrawStaticEffect()
    {
        // Raylib.DrawTexture(Cache.GetTexture(Globals.getStaticImage((byte)MenusCore.CurrentStaticImageIndex)), 0, 0, new Color(255, 255, 255, GameState.Project.game_info.Opacity));
        // to fix!
    }

    /// <summary>
    ///     Start the night.
    /// </summary>
    public static void StartNight()
    {
        Scene.SetScene(SceneType.Office);
    }

    /// <summary>
    ///     (Deprecated) Caches images, fonts, etc...
    /// </summary>
    public static void CacheAll()
    {
        Runtime.Cached = true;
        //await Task.Run(() => Cache.CacheFonts());
        // deprecating?
    }

    public static Color ParseStringToColor(string[] rgb)
    {
        return new Color(ParseInt(rgb[0]), ParseInt(rgb[1]), ParseInt(rgb[2]), 0);
    }

    public static Color ParseStringToColor(string red, string green, string blue)
    {
        return new Color(ParseInt(red), ParseInt(green), ParseInt(blue), 0);
    }

    public static Color ParseIntToColor(int red, int green, int blue)
    {
        return new Color(red, green, blue, 0);
    }

    public static unsafe string SBytePointerToString(sbyte* sbytePointer)
    {
        var length = 0;
        while (sbytePointer[length] != 0) length++;

        var byteArray = new byte[length];
        for (var i = 0; i < length; i++) byteArray[i] = (byte)sbytePointer[i];
        return Encoding.UTF8.GetString(byteArray);
    }

    public static int ParseInt(string value)
    {
        if (int.TryParse(value, out var result))
            return result;
        Logger.LogFatalAsync("RuntimeUtils", $"Invalid integer value: {value}");
        return 0; // Won't matter?
    }

    public static OfficeAnimatronic.Jumpscare ListToOfficeJumpscare(List<string> jumpscare)
    {
        return new OfficeAnimatronic.Jumpscare
        {
            Animation = jumpscare[0],
            Sound = jumpscare[1]
        };
    }

    public static GameJson.Input? ConvertOldCamera()
    {
        if (OfficeCore.Office == null)
        {
            Logger.LogFatalAsync("RuntimeUtils", "OfficeCore.Office is null.");
            return null;
        }

        return new GameJson.Input
        {
            Image =
                (GameState.Project.Offices[OfficeCore.Office].OldUIButtons ?? new GameJson.Uibuttons()).Camera.Image ??
                "",
            Position = (GameState.Project.Offices[OfficeCore.Office].OldUIButtons ?? new GameJson.Uibuttons()).Camera
                .Position
        };
    }

    public static GameJson.Input? ConvertOldMask()
    {
        if (OfficeCore.Office == null)
        {
            Logger.LogFatalAsync("RuntimeUtils", "OfficeCore.Office is null.");
            return null;
        }

        return new GameJson.Input
        {
            Image =
                (GameState.Project.Offices[OfficeCore.Office].OldUIButtons ?? new GameJson.Uibuttons()).Mask.Image ??
                "",
            Position = (GameState.Project.Offices[OfficeCore.Office].OldUIButtons ?? new GameJson.Uibuttons()).Mask
                .Position
        };
    }

    public static GameJson.Input DeepCopyInput(GameJson.Input OrgInput)
    {
        return new GameJson.Input
        {
            Image = OrgInput.Image,
            Position = OrgInput.Position
        };
    }

    public static GameJson.UI DeepCopyUI(GameJson.UI OrgUI)
    {
        return new GameJson.UI
        {
            IsToxic = OrgUI.IsToxic,
            Text = OrgUI.Text
        };
    }

    public static class Scene
    {
        public static void LoadScenes()
        {
            // Scene types must be in the correct order!!!!
            GameState.Scenes.Add(new MenuHandler()); // SceneType.Menu
            GameState.Scenes.Add(new OfficeHandler()); // SceneType.Office
            GameState.Scenes.Add(new CameraHandler()); // SceneType.Cameras
            GameState.Scenes.Add(new CrashHandler()); // SceneType.CrashHandler
        }

        // This will be used for Cams and I might also use it for minigames
        public static void SetScenePreserve(SceneType idx)
        {
            if ((int)idx < 0 || (int)idx >= GameState.Scenes.Count || GameState.Scenes[(int)idx] == null)
                throw new InvalidOperationException("Invalid scene index or scene is null.");

            GameState.CurrentScene = GameState.Scenes[(int)idx];
        }

        public static void SetScene(SceneType idx)
        {
            GameState.CurrentScene?.Exit();
            if ((int)idx < 0 || (int)idx >= GameState.Scenes.Count || GameState.Scenes[(int)idx] == null)
                throw new InvalidOperationException("Invalid scene index or scene is null.");

            GameState.CurrentScene = GameState.Scenes[(int)idx];
            GameState.CurrentScene.Init();
        }
    }
}