namespace FNAFStudio_Runtime_RCS.Menus.Definitions;

public class MenuProperties
{
    public string BackgroundImage { get; set; } = "";
    public string BackgroundMusic { get; set; } = "";
    public bool ButtonArrows { get; set; } = true;
    public bool FadeIn { get; set; }
    public bool MenuScroll { get; set; }
    public bool FadeOut { get; set; }
    public int FadeSpeed { get; set; }
    public bool Panorama { get; set; }
    public string ButtonArrowStr { get; set; } = ">>";
    public string ButtonArrowColor { get; set; } = "255,255,255";
    public string ButtonArrowFont { get; set; } = "Consolas";
    public bool? StaticEffect { get; set; } = false;
    public string BackgroundColor { get; set; } = "0,0,0";
}