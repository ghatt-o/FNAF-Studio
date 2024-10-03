using FNAFStudio_Runtime_RCS.Data.Definitions;

namespace FNAFStudio_Runtime_RCS.Office.Scenes
{
    public class CameraHandler : IScene
    {
        public string Name => "CameraHandler";
        public static float ScrollX = 0; // Needed for buttons

        public Task UpdateAsync()
        {
            return Task.CompletedTask;
        }

        public void Draw()
        {

        }
    }
}
