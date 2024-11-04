using FNAFStudio_Runtime_RCS.Data.Definitions;

namespace FNAFStudio_Runtime_RCS.Office.Definitions;

public class OfficeAnimatronic
{
    public List<int> AI = [];

    public bool IgnoresMask;

    public GameJson.PathNode Location = new();

    public int LocationIndex;

    public int MoveTime;

    public string Name = "";

    public List<GameJson.PathNode> Path = [];

    public Jumpscare Scare = new();

    public string State = "";

    public bool Phantom { get; set; }
    public bool Script { get; set; }

    public class Jumpscare
    {
        public string Animation = "";

        public int Offset;
        public string Sound = "";
    }
}