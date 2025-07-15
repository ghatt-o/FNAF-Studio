using FNaFStudio_Runtime.Data;

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

    public bool SignalInterrupted; // DEPRECATED

    public void SetCamera(string cam)
    {
        CurrentCamera = cam;
        SoundPlayer.SetChannelVolume(10, (OfficeCore.OfficeState.Cameras[cam].Interrupted && IsCameraUp) ? 100 : 0);
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