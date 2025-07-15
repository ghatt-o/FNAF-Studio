using FNaFStudio_Runtime.Data;

namespace FNaFStudio_Runtime.Office.Definitions;
public class OfficeCamera
{
    public bool Panorama;
    public int Scroll;
    public string State = "Default";
    public Dictionary<string, string> States = [];
    public bool Static;
    public bool Interrupted = false;
    public float InterruptTimer = 0f;

    public void SetState(string state)
    {
        State = state;
    }

    public void SetInterrupted(bool interrupted)
    {
        Interrupted = interrupted;
        if (interrupted)
        {
            InterruptTimer = 3.0f;
        }
        else
        {
            InterruptTimer = 0f;
        }
        SoundPlayer.PlayOnChannel(GameState.Project.Sounds.SignalInterrupted, false, 10);
    }

    public void Update(float deltaTime)
    {
        if (Interrupted && InterruptTimer > 0)
        {
            InterruptTimer -= deltaTime;
            if (InterruptTimer <= 0)
            {
                Interrupted = false;
                InterruptTimer = 0f;
                SoundPlayer.StopChannel(10);
            }
        }
    }
}