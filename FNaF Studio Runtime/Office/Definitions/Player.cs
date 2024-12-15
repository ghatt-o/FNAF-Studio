namespace FNaFStudio_Runtime.Office.Definitions;

public class Player
{
    public bool CameraEnabled = true;
    public bool MaskEnabled = true;
    public float ToxicLevel;

    public string CurrentCamera = "Default";
    public bool IsCameraUp;

    public bool IsFlashlightOn;

    public bool IsMaskOn;

    public bool SignalInterrupted;

    public static void SetCamera(Player self, string cam)
    {
        self.CurrentCamera = cam;
    }

    public void Putdown()
    {
        IsCameraUp = false;
    }

    public void Pullup()
    {
        IsCameraUp = true;
    }

    public void MaskOff()
    {
        IsMaskOn = false;
    }

    public void MaskOn()
    {
        IsMaskOn = true;
    }
}