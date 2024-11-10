using System.Numerics;
using Raylib_CsLo;

namespace FNAFStudio_Runtime_RCS.Data.Definitions.GameObjects;

public class Text
{
    public Text(string content, int fontSize, string fontName, Color color)
    {
        Content = content.Replace(@"\n", "\n");
        FontSize = fontSize;
        Color = color;
        Font = Cache.GetFont(fontName, FontSize);
    }

    public string Content { get; set; }
    public int FontSize { get; set; }
    public Font Font { get; set; }
    public Color Color { get; set; }

    public Rectangle GetBounds(Vector2 position)
    {
        var textSize = Raylib.MeasureTextEx(Font, Content, FontSize, 1);
        return new Rectangle(position.X, position.Y, textSize.X, textSize.Y);
    }

    public void Draw(Vector2 position)
    {
        Raylib.DrawTextEx(Font, Content, position, FontSize, 1, Color);
    }
}