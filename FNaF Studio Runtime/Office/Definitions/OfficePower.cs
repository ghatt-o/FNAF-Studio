using FNAFStudio_Runtime_RCS.Data.Definitions;

namespace FNAFStudio_Runtime_RCS.Office.Definitions;

public class OfficePower
{
    public class PowerOutAnim
    {
        public List<AJson.Frame> Frames = [];

        public int Offset;
    }

    public int Level = 1;

    public int Usage;

    public bool Enabled;

    public bool UCN;

    public PowerOutAnim PowerOutAnimation = new();

    public string AnimatronicJumpscare = "";
}
