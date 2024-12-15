using static FNaFStudio_Runtime.Data.Definitions.GameJson;

namespace FNaFStudio_Runtime.Menus.Definitions;

public class Menu
{
    public List<Code> Code = [];
    public List<MenuElement> Elements = [];
    public Properties Properties = new();
}