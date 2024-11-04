using FNAFStudio_Runtime_RCS.Data.CRScript;
using FNAFStudio_Runtime_RCS.Data.Definitions;
using FNAFStudio_Runtime_RCS.Menus;
using FNAFStudio_Runtime_RCS.Menus.Definitions;
using FNAFStudio_Runtime_RCS.Office.Definitions;
using FNAFStudio_Runtime_RCS.Util;

namespace FNAFStudio_Runtime_RCS.Data;

public static class Globals
{
    public const float xMagic = 2.13333333f;
    public const float yMagic = 2.13649852f;

    public static string GetStaticImage(byte value)
    {
        if (value < 1) throw new ArgumentException("Static Image value is below 1.");
        if (value > 8) throw new ArgumentException("Static Image value is above 8.");

        return $"e.static{value}.png";
    }
}

public static class MenusCore
{
    public static string Menu { get; set; } = string.Empty;
    public static int CurrentStaticImageIndex { get; set; }
    public static Dictionary<string, Menu> Menus { get; } = [];
    public static bool ArrowIn { get; internal set; }

    public static Menu? ConvertMenuToAPI(GameJson.Menu menu)
    {
        var cacheMenu = new Menu();

        foreach (var el in menu.Elements)
        {
            var newElement = new MenuElement
            {
                Color = RuntimeUtils.ParseIntToColor(el.Red, el.Green, el.Blue),
                Animation = el.Animation,
                Animatronic = el.Animatronic,
                FontName = el.Fontname,
                FontSize = el.Fontsize,
                Hidden = el.Hidden,
                Text = el.Text,
                ID = el.ID,
                Sprite = el.Sprite,
                X = (int)(el.X * Globals.xMagic),
                Y = (int)(el.Y * Globals.xMagic),
                Type = el.Type
            };

            cacheMenu.Elements.Add(newElement);
        }

        cacheMenu.Properties = DeepCopyProperties(menu.Properties);
        cacheMenu.Code = menu.Code;

        return cacheMenu;
    }

    public static GameJson.Properties DeepCopyProperties(GameJson.Properties props)
    {
        return new GameJson.Properties
        {
            BackgroundImage = props.BackgroundImage,
            BackgroundMusic = props.BackgroundMusic,
            BackgroundColor = props.BackgroundColor,
            ButtonArrows = props.ButtonArrows,
            ButtonArrowColor = props.ButtonArrowColor,
            ButtonArrowFont = props.ButtonArrowFont,
            ButtonArrowStr = props.ButtonArrowStr,
            FadeIn = props.FadeIn,
            FadeOut = props.FadeOut,
            FadeSpeed = props.FadeSpeed,
            Panorama = props.Panorama,
            StaticEffect = props.StaticEffect,
            MenuScroll = props.MenuScroll
        };
    }
}

public static class OfficeCore
{
    public static bool LoadingLock = false;

    public static Dictionary<string, OfficeGame> OfficeCache = [];
    public static string? Office { get; set; } = "office";
    public static OfficeGame? OfficeState { get; set; }
}

public static class GameState
{
    public static string SelectedButtonID = string.Empty;

    public static TickManager Clock = new();

    public static bool DebugMode = false;

    public static List<IScene> Scenes = [];
    public static IScene CurrentScene = new MenuHandler();

    public static GameJson.Game Project = new();
    public static string ProjectPath { get; set; } = "assets";
}