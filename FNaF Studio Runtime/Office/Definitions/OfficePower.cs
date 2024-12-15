using FNaFStudio_Runtime.Data.Definitions;

namespace FNaFStudio_Runtime.Office.Definitions;

public class OfficePower
{
    public string AnimatronicJumpscare = "";
    public PowerOutAnim PowerOutAnimation = new();
    public bool Enabled;
    public int Level = 1;
    public float Accumulator = 0;
    public int Ticks = 9600;
    public bool UCN;
    public int Usage = 0;

    public class PowerOutAnim
    {
        public List<AJson.Frame> Frames = [];
        public int Offset;
    }
}