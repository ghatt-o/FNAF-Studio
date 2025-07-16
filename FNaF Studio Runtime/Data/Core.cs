using FNaFStudio_Runtime.Data.CRScript;
using FNaFStudio_Runtime.Data.Definitions;
using FNaFStudio_Runtime.Menus;
using FNaFStudio_Runtime.Menus.Definitions;
using FNaFStudio_Runtime.Office.Definitions;
using FNaFStudio_Runtime.Util;

namespace FNaFStudio_Runtime.Data;

public static class Globals
{
    public const float XMagic = 2.13333333f;
    public const float YMagic = 2.13649852f;

    public static string GetStaticImage(byte value)
    {
        return value switch
        {
            < 1 => throw new ArgumentException("Static Image value is below 1."),
            > 8 => throw new ArgumentException("Static Image value is above 8."),
            _ => $"e.static{value}.png"
        };
    }
}

public static class MenusCore
{
    public static int CurrentStaticImageIndex { get; set; }
    public static Dictionary<string, Menu> Menus { get; } = [];
    public static bool ArrowIn { get; internal set; }

    public static Menu ConvertMenuToApi(GameJson.Menu menu)
    {
        var cacheMenu = new Menu();

        foreach (var newElement in menu.Elements.Select(el => new MenuElement
                 {
                     Color = RuntimeUtils.ParseIntToColor(el.Red, el.Green, el.Blue),
                     Animation = el.Animation,
                     Animatronic = el.Animatronic,
                     FontName = el.Fontname,
                     FontSize = el.Fontsize,
                     Hidden = el.Hidden,
                     Text = el.Text,
                     Id = el.ID,
                     Sprite = el.Sprite,
                     X = (int)(el.X * Globals.XMagic),
                     Y = (int)(el.Y * Globals.XMagic),
                     Type = el.Type
                 }))
        {
            cacheMenu.Elements.Add(newElement);
        }

        cacheMenu.Properties = DeepCopyProperties(menu.Properties);
        cacheMenu.Code = menu.Code;

        return cacheMenu;
    }

    private static GameJson.Properties DeepCopyProperties(GameJson.Properties props)
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
    public static int CurStateWidth = 0; // required to fix state cache failure on multi-thread (update caches before draw)
    public static Dictionary<string, OfficeGame> OfficeCache = [];
    public static string? Office { get; set; } = "office";
    public static OfficeGame? OfficeState { get; set; }
}

public static class GameState
{
    public static string SelectedButtonId = string.Empty;

    public static readonly TickManager Clock = new();

    public static bool DebugMode = false;

    public static readonly List<IScene> Scenes = [];
    public static IScene CurrentScene = new MenuHandler();

    public static GameJson.Game Project = new();
    public static readonly object ButtonsLock = new();

    public static string ProjectPath { get; set; } = "assets";
    public static float ScrollX { get; set; }
    public static float[] ScrollXCache { get; set; } = [0, 0, 0, 0, 0];
}