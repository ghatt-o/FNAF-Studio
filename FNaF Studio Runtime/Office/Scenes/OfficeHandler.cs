using FNAFStudio_Runtime_RCS.Data;
using FNAFStudio_Runtime_RCS.Data.Definitions;
using FNAFStudio_Runtime_RCS.Data.Definitions.GameObjects;
using FNAFStudio_Runtime_RCS.Util;
using Raylib_CsLo;
using System.Numerics;

namespace FNAFStudio_Runtime_RCS.Office.Scenes
{
    public class OfficeHandler : IScene
    {
        public string Name => "OfficeHandler";
        public static int TempNight = -1;
        public static float ScrollX = 0;

        public async Task UpdateAsync()
        {
            // TODO: use Camera2D instead of manual X scrolling? - Mike 
            // Yes, but after I consider adding Projection Shaders,
            // so we can use Camera2D for 2 different scroll types
            // otherwise, we are keeping this because its smaller - HexDev
            if (OfficeCore.OfficeState != null && !OfficeCore.LoadingLock)
            {
                Texture curState = Cache.GetTexture(OfficeCore.OfficeState.Office.States[OfficeCore.OfficeState.Office.State]);
                float viewportWidth = Raylib.GetScreenWidth();
                Vector2 mousePosition = Raylib.GetMousePosition();
                float mousePositionX = mousePosition.X;

                if (viewportWidth < curState.width)
                {
                    float scrollSpeed = 0.0f;
                    if (mousePositionX < viewportWidth * 0.1f)
                        scrollSpeed = 550.0f;
                    else if (mousePositionX < viewportWidth * 0.225f)
                        scrollSpeed = 350.0f;
                    else if (mousePositionX < viewportWidth * 0.35f)
                        scrollSpeed = 150.0f;
                    else if (mousePositionX > viewportWidth * 0.9f)
                        scrollSpeed = 550.0f;
                    else if (mousePositionX > viewportWidth * 0.775f)
                        scrollSpeed = 350.0f;
                    else if (mousePositionX > viewportWidth * 0.65f)
                        scrollSpeed = 150.0f;

                    float newScrollX = ScrollX + scrollSpeed * Raylib.GetFrameTime() * Math.Sign(mousePositionX - viewportWidth * 0.5f);
                    ScrollX = Math.Clamp(newScrollX, 0.0f, curState.width - viewportWidth);
                }

                // Update all buttons
                foreach (var button in GameCache.Buttons.Values)
                {
                    await button.UpdateAsync(ScrollX);
                }
            }
        }

        public void Draw()
        {
            if (OfficeCore.LoadingLock || OfficeCore.Office == null || OfficeCore.OfficeState == null) return;

            string texPath = OfficeCore.OfficeState.Office.States[OfficeCore.OfficeState.Office.State];
            if (OfficeCore.OfficeState.Office.States.TryGetValue(OfficeCore.OfficeState.Office.State, out var path))
                if (!string.IsNullOrEmpty(path))
                    texPath = path;

            Texture curState = Cache.GetTexture(texPath);
            Raylib.DrawTexture(curState, (int)(ScrollX * -1), 0, Raylib.WHITE);

            foreach (var obj in GameState.Project.Offices[OfficeCore.Office].Objects)
            {
                if (obj.Position == null || obj.ID == null || !OfficeCore.OfficeState.Office.Objects[obj.ID].Visible) continue;

                Vector2 objPos = new(obj.Position[0] * Globals.xMagic - ScrollX, obj.Position[1] * Globals.yMagic);

                switch (obj.Type)
                {
                    case "door_button" when OfficeCore.OfficeState.Office.Doors.TryGetValue(obj.ID, out var doorVars):
                        OfficeUtils.GetDoorButton(obj.ID, obj).Draw(objPos, doorVars.Button.IsOn);
                        break;
                    case "light_button" when OfficeCore.OfficeState.Office.Lights.TryGetValue(obj.ID, out var lightVars):
                        OfficeUtils.GetLightButton(obj.ID, obj).Draw(objPos, lightVars.IsOn);
                        break;
                    case "sprite" when obj.Sprite != null:
                        if (GameCache.Buttons.TryGetValue(obj.ID, out var imgBtn)) imgBtn.Draw(objPos);
                        else
                        {
                            var tex = Cache.GetTexture(obj.Sprite);
                            var newImgBtn = new Button2D(objPos, obj, texture: tex);
                            GameCache.Buttons[obj.ID] = newImgBtn;
                            newImgBtn.Draw(objPos);
                        }
                        break;
                    case "animation" when obj.Animation != null:
                        Cache.GetAnimation(obj.Animation).AdvanceDraw(objPos);
                        break;
                    case "door" when OfficeCore.OfficeState.Office.Doors.TryGetValue(obj.ID, out var doorAnimVars) && doorAnimVars.Animation != null:
                        doorAnimVars.Animation.AdvanceDraw(objPos);
                        break;

                    // new
                    case "text":
                        string uid = obj.Text ?? "";
                        if (GameCache.Buttons.TryGetValue(obj.ID, out var btn)) btn.Draw(objPos);
                        else
                        {
                            Text tx = GameCache.Texts.TryGetValue(uid, out Text? value) ? value : GameCache.Texts[uid] = new Text(obj.Text ?? "", 36, "Arial", Raylib.WHITE);
                            var newBtn = new Button2D(objPos, obj, text: tx);
                            GameCache.Buttons[obj.ID] = newBtn;
                            newBtn.Draw(objPos);
                        }
                        break;
                    default:
                        Logger.LogFatalAsync("MenuHandler", $"Unimplemented Element Type: {obj.Type}");
                        break;
                }
            }

            OfficeUtils.DrawHUD();
        }

        public void Exit()
        {
            SoundPlayer.KillAllAsync().Wait();
            TimeManager.Stop();
            TimeManager.Reset();
        }
    }
}
