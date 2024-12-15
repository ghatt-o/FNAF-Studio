using FNaFStudio_Runtime.Data.Definitions;
using FNaFStudio_Runtime.Menus;
using static FNaFStudio_Runtime.Data.Definitions.GameJson;

namespace FNaFStudio_Runtime.Office.Definitions;

public class OfficeGame(int Night)
{
    public Dictionary<string, List<AJson.Frame>> Animations = [];
    public Dictionary<string, OfficeAnimatronic> Animatronics = [];
    public Dictionary<string, OfficeCamera> Cameras = [];
    public CamUI CameraUI = new();
    public int Night = Night;
    public OfficeData Office = new();
    public Player Player = new();
    public OfficePower Power = new();
    public OfficeSettings Settings = new();
    public Dictionary<string, UIButton> UIButtons = [];
    public int Time { get; internal set; }
    public bool Able { get; set; } = true;
    public int EndingTime { get; set; } = 6;

    public void SetTime(int v)
    {
        Time = v;
    }

    public static void Quit()
    {
        Environment.Exit(0);
    }

    public void DisableTime(object b)
    {
        Able = false;
    }

    public static void GotoMenu(string name)
    {
        MenuUtils.GotoMenu(name);
    }
}