using FNaFStudio_Runtime.Data;
using FNaFStudio_Runtime.Data.CRScript;
using FNaFStudio_Runtime.Data.Definitions;
using FNaFStudio_Runtime.Menus.Definitions;
using FNaFStudio_Runtime.Util;

namespace FNaFStudio_Runtime.Menus;

public class MenuHandler : IScene
{
    public static Menu menuReference = new();
    public string Name => "Menus";
    public SceneType Type => SceneType.Menu;

    public void Draw()
    {
        MenuUtils.DrawMenuBackgrounds();
        MenuUtils.Element.DrawMenuElements();
    }

    public void Exit()
    {
        menuReference = new Menu();
        MenusCore.Menu = string.Empty;
    }
}