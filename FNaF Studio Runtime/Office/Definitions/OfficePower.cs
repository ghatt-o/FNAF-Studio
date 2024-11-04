using FNAFStudio_Runtime_RCS.Data.Definitions;

namespace FNAFStudio_Runtime_RCS.Office.Definitions;

public class OfficePower
{
    public string AnimatronicJumpscare = "";

    public bool Enabled;

    public int Level = 1;

    public PowerOutAnim PowerOutAnimation = new();

    public bool UCN;

    public int Usage;

    public class PowerOutAnim
    {
        public List<AJson.Frame> Frames = [];

        public int Offset;
    }
}