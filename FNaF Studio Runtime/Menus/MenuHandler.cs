using FNAFStudio_Runtime_RCS.Data;
using FNAFStudio_Runtime_RCS.Data.CRScript;
using FNAFStudio_Runtime_RCS.Data.Definitions;
using FNAFStudio_Runtime_RCS.Util;

namespace FNAFStudio_Runtime_RCS.Menus
{
    public class MenuHandler : IScene
    {
        public string Name => "Menus";

        public static Definitions.Menu menuReference = new();

        public static void Startup()
        {
            // cant put this in init otherwise exiting office
            // would reset you back to warning menu
            GameState.Project.Menus.ToList().ForEach(menu =>
            {
                if (MenusCore.ConvertMenuToAPI(menu.Value) is { } newMenu)
                    MenusCore.Menus.Add(menu.Key, newMenu);
            });

            MenuUtils.GotoMenu(GameState.Project.Menus.ContainsKey("Warning") ? "Warning" : "Main");
        }

        public async Task UpdateAsync()
        {
            MenusCore.CurrentStaticImageIndex = (MenusCore.CurrentStaticImageIndex >= 7) ? 1 : (++MenusCore.CurrentStaticImageIndex);

            foreach (var btn in GameCache.Buttons.Values)
                await btn.UpdateAsync(0);
        }

        public void Draw()
        {
            MenuUtils.DrawMenuBackgrounds();
            RuntimeUtils.DrawStaticEffect();
            MenuUtils.Element.DrawMenuElements();
        }

        public void Exit()
        {
            // We are going to office
            menuReference = new();
            EventManager.KillAllListeners();
            SoundPlayer.KillAllAsync().Wait();
            MenusCore.Menu = string.Empty;
            GameCache.Texts.Clear();
            GameCache.Buttons.Clear();
            GameState.Clock.Restart();
        }
    }
}
