namespace FNAFStudio_Runtime_RCS.Office.Definitions;

public class OfficeCamera
{
    public bool Panorama;

    public bool Static;

    public Dictionary<string, string> States = [];

    public string State = "Default";

    public int Scroll;

    public void SetState(string state)
    {
        State = state;
    }
}
