namespace FNAFStudio_Runtime_RCS.Office.Definitions;

public class Player
{
    public bool IsCameraUp;

    public bool CameraButtonToggle;

    public bool IsMaskOn;

    public bool IsFlashlightOn;

    public string CurrentCamera = "";

    public bool SignalInterrupted;

    public static void SetCamera(Player self, string cam) => self.CurrentCamera = cam;

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
