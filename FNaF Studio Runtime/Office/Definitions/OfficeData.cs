using FNAFStudio_Runtime_RCS.Data.Definitions;
using FNAFStudio_Runtime_RCS.Data.Definitions.GameObjects;

namespace FNAFStudio_Runtime_RCS.Office.Definitions;

public class OfficeData
{
    public class OfficeSprite
    {
        public bool Visible = true;

        public bool AbovePanorama = true;

        public bool Hovered { get; internal set; }
    }

    public class OfficeAnimation : OfficeSprite
    {
        public string Id { get; internal set; } = string.Empty;

        public bool IsPlaying { get; internal set; } = true;

        public bool Rev { get; set; } = false;

        public List<AJson.Frame> Animation { get; internal set; } = [];
    }

    public class OfficeDoor
    {
        public bool IsClosed;

        public RevAnimation? Animation; // I'm keeping this just incase

        public OfficeButton Button = new();
    }

    public class OfficeButton
    {
        public bool IsOn;
        internal bool Clickable;
    }

    public class OfficeLight
    {
        public bool IsOn;
        internal bool Clickable;
    }

    public Dictionary<string, string> States = [];

    public string State = "Default";

    public bool BlinkingEffect;

    public bool DisableFlashlight;

    public Dictionary<string, OfficeSprite> Objects = [];

    public Dictionary<string, OfficeLight> Lights = [];

    public Dictionary<string, OfficeDoor> Doors = [];

    public Dictionary<string, OfficeAnimation> Animations = [];

    public Dictionary<string, OfficeSprite> Sprites = [];

    public void SetState(string state)
    {
        State = state;
    }
}
