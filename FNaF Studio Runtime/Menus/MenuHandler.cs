using FNaFStudio_Runtime.Data.Definitions;
using FNaFStudio_Runtime.Menus.Definitions;

namespace FNaFStudio_Runtime.Menus;

public class MenuHandler : IScene
{
    public static Menu MenuReference = new();
    public string Name => "Menus";
    public SceneType Type => SceneType.Menu;

    public void Draw()
    {
        MenuUtils.DrawMenuBackgrounds();
        MenuUtils.Element.DrawMenuElements();
    }

    public void Exit()
    {
        MenuReference = new Menu();
    }
}