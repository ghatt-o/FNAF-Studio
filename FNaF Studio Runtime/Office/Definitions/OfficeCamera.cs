namespace FNAFStudio_Runtime_RCS.Office.Definitions;

public class OfficeCamera
{
    public bool Panorama;

    public int Scroll;

    public string State = "Default";

    public Dictionary<string, string> States = [];

    public bool Static;

    public void SetState(string state)
    {
        State = state;
    }
}