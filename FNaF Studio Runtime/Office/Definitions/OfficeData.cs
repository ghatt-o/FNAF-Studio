using FNaFStudio_Runtime.Data.Definitions;
using FNaFStudio_Runtime.Data.Definitions.GameObjects;

namespace FNaFStudio_Runtime.Office.Definitions;

public class OfficeData
{
    public Dictionary<string, OfficeAnimation> Animations = [];

    public bool BlinkingEffect;

    public bool DisableFlashlight;

    public Dictionary<string, OfficeDoor> Doors = [];

    public Dictionary<string, OfficeLight> Lights = [];

    public Dictionary<string, OfficeSprite> Objects = [];

    public Dictionary<string, OfficeSprite> Sprites = [];

    public string State = "Default";

    public Dictionary<string, string> States = [];

    public void SetState(string state)
    {
        State = state;
    }

    public class OfficeSprite
    {
        public bool AbovePanorama = true;
        public bool Visible = true;

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
        public RevAnimation? Animation; // I'm keeping this just incase
        public string CloseSound = string.Empty;
        public string OpenSound = string.Empty;
        public OfficeButton Button = new();
        public bool IsClosed;
    }

    public class OfficeButton
    {
        internal bool Clickable;
        public bool IsOn;
    }

    public class OfficeLight
    {
        internal bool Clickable;
        public bool IsOn;
    }
}