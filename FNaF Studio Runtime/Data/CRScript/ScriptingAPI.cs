using FNaFStudio_Runtime.Menus;
using FNaFStudio_Runtime.Office;
using FNaFStudio_Runtime.Office.Scenes;
using FNaFStudio_Runtime.Util;
using Raylib_CsLo;

namespace FNaFStudio_Runtime.Data.CRScript;

public class ScriptingAPI
{
    public ScriptingAPI()
    {
        Actions = new Dictionary<string, Func<List<string>, bool>>
        {
            // General blocks
            { "quit", Quit },
            { "goto_menu", GotoMenu },
            { "set_data", SetData },
            { "set_var", SetVar },
            { "compare_values", CompareValues },

            // Sounds
            { "play_sound", PlaySound },
            { "stop_channel", StopChannel },
            { "set_channel_volume", SetVolume },

            // Menu property events
            { "set_background", SetBackground },

            // Element manipulation
            { "hide_element", HideElement },
            { "show_element", ShowElement },
            { "is_button_selected", IsElementSelected },
            { "is_image_selected", IsElementSelected },
            { "set_sprite", SetSprite },
            { "set_text", SetText },

            // Object manipulation
            { "hide_office_object", HideOfficeObject },
            { "show_office_object", ShowOfficeObject },
            { "is_mouse_over_object", IsMouseOverObject },
            { "is_mouse_over_sprite", IsMouseOverSprite },
            { "set_object_panorama", SetObjectPanorama },

            // Button arrows and other Menu misc
            { "dbutton_arrows", DisableArrows },
            { "ebutton_arrows", EnableArrows },

            // Special actions
            { "start_new_game", StartNewGame },
            { "continue", ContinueGame },
            { "start_night", StartNightGame },
            { "setoff", SetOff }, // used in menus

            // Office
            // Special actions
            { "office", SetOffice },
            { "set_time", SetTime }, // BROKEN, FIX

            // Office States
            { "if_office_state", IfOfficeState },
            { "change_office_state", SetOfficeState },

            // Animatronics
            { "move_anim_light", MoveAnimToLight },

            // Cameras
            { "if_current_cam", IfCurrentCamera },
            { "if_camera_up", IfCameraUp },
            { "change_camera", ChangeCamera },
            { "hide_cam_sprite", HideCamSprite },
            { "show_cam_sprite", ShowCamSprite },
            { "is_object_visible", IsObjectVisible },

            // Power
            { "set_power_usage", SetPowerUsage },
            { "set_power_level", SetPowerLevel },

            // Misc
            { "line", SendMsg },
            { "comment", _ => true } // A no-op action
        };
    }

    private bool IsObjectVisible(List<string> args)
    {
        if (OfficeCore.OfficeState != null && !OfficeCore.LoadingLock)
        {
            if (OfficeCore.OfficeState.CameraUI.Sprites.TryGetValue(EventManager.GetExpr(args[0]), out var Object))
                return Object.Visible;
            else return false;
        }
        else
        {
            return false;
        }
    }

    public Dictionary<string, Func<List<string>, bool>> Actions { get; }

    private bool IfCameraUp(List<string> args)
    {
        if (OfficeCore.OfficeState != null)
            return OfficeCore.OfficeState.Player.IsCameraUp;
        return false;
    }

    private bool IfCurrentCamera(List<string> args)
    {
        if (OfficeCore.OfficeState != null)
            return OfficeCore.OfficeState.Player.CurrentCamera == EventManager.GetExpr(args[0]);
        return false;
    }

    public static bool MoveAnimToLight(List<string> args)
    {
        PathFinder.MoveAnimToNode(args[0], args[1]);
        return true;
    }

    private static bool ChangeCamera(List<string> args)
    {
        if (OfficeCore.OfficeState != null && OfficeCore.OfficeState.Cameras.ContainsKey(args[0]))
        {
            OfficeCore.OfficeState.Player.SetCamera(args[0]);
        }
        else
            Logger.LogFatalAsync("ScriptingAPI",
                "OfficeState is null or invalid argument provided for Change Camera. (" + args[0] + ")");
        return false;
    }

    private bool SetTime(List<string> args)
    {
        if (args.Count < 1)
        {
            Logger.LogFatalAsync("ScriptingAPI", "Argument required for Set Time.");
            return false;
        }

        var hours = RuntimeUtils.ParseInt(args[0]);
        if (hours > -1 && hours < 7)
        {
            TimeManager.SetTime(hours, 0, 0);
            return true;
        }

        if (hours == 12)
        {
            TimeManager.SetTime(0, 0, 0);
            return true;
        }

        Logger.LogFatalAsync("ScriptingAPI",
            "Invalid argument provided for Set Time. Value must be a number above -1 and below 7.");

        return false;
    }

    private bool SetPowerUsage(List<string> args)
    {
        if (args.Count < 1)
        {
            Logger.LogFatalAsync("ScriptingAPI", "Argument required for Set Power Usage.");
            return false;
        }

        var usage = RuntimeUtils.ParseInt(args[0]);
        if (OfficeCore.OfficeState != null)
        {
            OfficeCore.OfficeState.Power.Usage = usage;
            return true;
        }

        Logger.LogFatalAsync("ScriptingAPI", "OfficeState is null or invalid argument provided for Set Power Usage.");

        return false;
    }

    private bool SetPowerLevel(List<string> args)
    {
        if (args.Count < 1)
        {
            Logger.LogFatalAsync("ScriptingAPI", "Argument required for Set Power Level.");
            return false;
        }

        var level = RuntimeUtils.ParseInt(args[0]);
        if (OfficeCore.OfficeState != null)
        {
            OfficeCore.OfficeState.Power.Level = level;
            return true;
        }

        Logger.LogFatalAsync("ScriptingAPI", "OfficeState is null or invalid argument provided for Set Power Level.");

        return false;
    }

    private bool SendMsg(List<string> args)
    {
        var msg = args.FirstOrDefault();
        if (msg != null)
            Logger.LineAsync("User Message", msg).Wait();
        else Logger.LineAsync("User Message", string.Empty).Wait();
        return true;
    }

    public static bool StartNight(bool newGame, int night = -1)
    {
        var nightValue = newGame ? 1 :
            EventManager.dataValues.TryGetValue("Night", out var value) ? int.Parse(value ?? "1") : night;
        if (newGame) EventManager.SetDataValue("Night", "1");
        OfficeHandler.StartOffice(nightValue);
        return true;
    }

    private bool StartNewGame(List<string> args)
    {
        return StartNight(true);
    }

    private bool ContinueGame(List<string> args)
    {
        return StartNight(false);
    }

    private bool StartNightGame(List<string> args)
    {
        return StartNight(false, RuntimeUtils.ParseInt(EventManager.GetExpr(args[0])));
    }

    private bool SetOff(List<string> args)
    {
        OfficeCore.Office = args[0];
        return true;
    }

    private bool SetOffice(List<string> args)
    {
        if (OfficeCore.OfficeState != null)
        {
            OfficeCore.Office = args[0];
            OfficeCore.LoadingLock = true;
            var Night = OfficeCore.OfficeState.Night;
            OfficeHandler.ReloadOfficeData(Night);
            OfficeCore.LoadingLock = false;
            return true;
        }

        return false;
    }

    private bool IfOfficeState(List<string> args)
    {
        if (OfficeCore.OfficeState != null)
            return OfficeCore.OfficeState.Office.State == EventManager.GetExpr(args[0]);
        return false;
    }

    private bool SetOfficeState(List<string> args)
    {
        if (OfficeCore.OfficeState != null)
        {
            var arg = EventManager.GetExpr(args[0]);
            if (OfficeCore.OfficeState.Office.States.ContainsKey(arg))
                OfficeCore.OfficeState.Office.State = arg;
        }

        return false;
    }

    private bool HideOfficeObject(List<string> args)
    {
        if (OfficeCore.OfficeState != null && !OfficeCore.LoadingLock)
        {
            if (OfficeCore.OfficeState.Office.Objects.TryGetValue(EventManager.GetExpr(args[0]), out var Object))
                Object.Visible = false;
            else return false;
        }
        else
        {
            return false;
        }

        return true;
    }

    private bool ShowOfficeObject(List<string> args)
    {
        if (OfficeCore.OfficeState != null && !OfficeCore.LoadingLock)
        {
            if (OfficeCore.OfficeState.Office.Objects.TryGetValue(EventManager.GetExpr(args[0]), out var Object))
                Object.Visible = true;
            else return false;
        }
        else
        {
            return false;
        }

        return true;
    }

    private bool SetObjectPanorama(List<string> args)
    {
        if (OfficeCore.OfficeState != null && !OfficeCore.LoadingLock)
        {
            if (OfficeCore.OfficeState.Office.Sprites.TryGetValue(EventManager.GetExpr(args[0]), out var Object))
                Object.AbovePanorama = bool.Parse(args[1]);
            else return false;
        }
        else
        {
            return false;
        }

        return true;
    }

    private bool HideCamSprite(List<string> args)
    {
        if (OfficeCore.OfficeState != null && !OfficeCore.LoadingLock)
        {
            if (OfficeCore.OfficeState.CameraUI.Sprites.TryGetValue(EventManager.GetExpr(args[0]), out var Sprite))
                Sprite.Visible = false;
            else if (OfficeCore.OfficeState.CameraUI.Buttons.TryGetValue(EventManager.GetExpr(args[0]), out var Button))
                Button.Visible = false;
            else return false;
        }
        else
        {
            return false;
        }

        return true;
    }

    private bool ShowCamSprite(List<string> args)
    {
        if (OfficeCore.OfficeState != null && !OfficeCore.LoadingLock)
        {
            if (OfficeCore.OfficeState.CameraUI.Sprites.TryGetValue(EventManager.GetExpr(args[0]), out var Sprite))
                Sprite.Visible = true;
            else if (OfficeCore.OfficeState.CameraUI.Buttons.TryGetValue(EventManager.GetExpr(args[0]), out var Button))
                Button.Visible = true;
            else return false;
        }
        else
        {
            return false;
        }

        return true;
    }

    private bool IsMouseOverObject(List<string> args)
    {
        if (GameCache.Buttons.TryGetValue(EventManager.GetExpr(args[0]), out var Button))
            return Button.IsHovered;
        return false;
    }

    private bool IsMouseOverSprite(List<string> args)
    {
        if (GameCache.Buttons.TryGetValue(EventManager.GetExpr(args[0]), out var Button))
            return Button.IsHovered;
        return false;
    }

    public static bool Quit(List<string> args)
    {
        return RuntimeUtils.Quit();
    }

    public static bool PlaySound(List<string> args)
    {
        SoundPlayer.PlayOnChannel(args[0], bool.Parse(args[2]), int.Parse(args[1]));
        return true;
    }

    public static bool StopChannel(List<string> args)
    {
        SoundPlayer.StopChannel(int.Parse(args[0]));
        return true;
    }

    public static bool SetVolume(List<string> args)
    {
        SoundPlayer.SetChannelVolume(int.Parse(args[0]), float.Parse(args[1]));
        return true;
    }

    public static bool GotoMenu(List<string> args)
    {
        return MenuUtils.GotoMenu(EventManager.GetExpr(args[0]));
    }

    public static bool SetBackground(List<string> args)
    {
        return MenuUtils.SetBackground(EventManager.GetExpr(args[0]));
    }

    public static bool IsElementSelected(List<string> args)
    {
        return MenuUtils.Element.IsElementSelected(EventManager.GetExpr(args[0]));
    }

    public static bool HideElement(List<string> args)
    {
        return MenuUtils.Element.HideElement(EventManager.GetExpr(args[0]));
    }

    public static bool ShowElement(List<string> args)
    {
        return MenuUtils.Element.ShowElement(EventManager.GetExpr(args[0]));
    }

    public static bool SetText(List<string> args)
    {
        return MenuUtils.Element.SetText(EventManager.GetExpr(args[0]), EventManager.GetExpr(args[1]));
    }

    public static bool SetSprite(List<string> args)
    {
        return MenuUtils.Element.SetSprite(EventManager.GetExpr(args[0]), EventManager.GetExpr(args[1]));
    }

    public static bool EnableArrows(List<string> args)
    {
        MenuHandler.menuReference.Properties.ButtonArrows = true;
        return true;
    }

    public static bool DisableArrows(List<string> args)
    {
        MenuHandler.menuReference.Properties.ButtonArrows = false;
        return true;
    }

    public static bool SetData(List<string> args)
    {
        EventManager.SetDataValue(EventManager.GetExpr(args[0]), EventManager.GetExpr(args[1]));
        return true;
    }

    public static bool SetVar(List<string> args)
    {
        EventManager.SetVariableValue(EventManager.GetExpr(args[0]), EventManager.GetExpr(args[1]));
        return true;
    }

    public static bool CompareValues(List<string> args)
    {
        var lhs = EventManager.GetExpr(args[0]);
        var rhs = EventManager.GetExpr(args[2]);

        switch (args[1])
        {
            case "==":
                return lhs == rhs;
            case "<>":
                return lhs != rhs;
            case ">":
                return double.Parse(lhs) > double.Parse(rhs);
            case "<":
                return double.Parse(lhs) < double.Parse(rhs);
            case ">=":
                return double.Parse(lhs) >= double.Parse(rhs);
            case "<=":
                return double.Parse(lhs) <= double.Parse(rhs);
            default:
                Logger.LogErrorAsync("ScriptingAPI", "Invalid operator");
                return false;
        }
    }

    public static void TickEvents()
    {
        var mouseButtons = new[]
        {
            (MouseButton.MOUSE_BUTTON_LEFT, "user_left_clicked"),
            (MouseButton.MOUSE_BUTTON_RIGHT, "user_right_clicked"),
            (MouseButton.MOUSE_BUTTON_MIDDLE, "user_middle_clicked")
        };

        foreach (var (button, eventName) in mouseButtons)
            if (Raylib.IsMouseButtonPressed(button))
                EventManager.TriggerEvent(eventName, []);
    }
}
