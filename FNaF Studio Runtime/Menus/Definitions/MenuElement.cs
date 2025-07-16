using Raylib_CsLo;

namespace FNaFStudio_Runtime.Menus.Definitions;

public class MenuElement
{
    public Color Color { get; set; }
    public string FontName { get; set; } = string.Empty;
    public int FontSize { get; set; }
    public bool Hidden { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Sprite { get; set; } = string.Empty;
    public string Animatronic { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public string Animation { get; set; } = string.Empty;
}