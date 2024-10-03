using FNAFStudio_Runtime_RCS.Data.Definitions;

namespace FNAFStudio_Runtime_RCS.Office.Definitions;

public class OfficeAnimatronic
{
    public class Jumpscare
    {
        public string Sound = "";

        public string Animation = "";

        public int Offset;
    }

    public bool IgnoresMask;

    public List<GameJson.PathNode> Path = [];

    public List<int> AI = [];

    public Jumpscare Scare = new();

    public GameJson.PathNode Location = new();

    public string Name = "";

    public int MoveTime;

    public string State = "";

    public int LocationIndex;

    public bool Phantom { get; set; }
    public bool Script { get; set; }
}
