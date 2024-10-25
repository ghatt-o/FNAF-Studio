using FNAFStudio_Runtime_RCS.Data;
using FNAFStudio_Runtime_RCS.Data.Definitions;
using Raylib_CsLo;
using System;
using System.Numerics;

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
            if (OfficeCore.LoadingLock || OfficeCore.Office == null || OfficeCore.OfficeState == null) return;

            var curCam = OfficeCore.OfficeState.Cameras[OfficeCore.OfficeState.Player.CurrentCamera];
            if (OfficeCore.OfficeState.Office.States.TryGetValue(curCam.States[OfficeCore.OfficeState.Player.CurrentCamera], out var path))
                if (!string.IsNullOrEmpty(path))
                {
                    Texture texture = Cache.GetTexture(path);
                    float scrollFactor = curCam.Scroll * 30;
                    float frameTime = (float)Raylib.GetFrameTime();

                    var halfWidth = texture.width / 2;
                    float distance = Math.Abs(scrollFactor - halfWidth) / halfWidth;
                    float velocity = Math.Clamp(distance, 0, 1) * (texture.width / 8);
                    ScrollX -= velocity * float.Lerp(0.3f, 0.5f, distance) * Math.Sign(halfWidth - scrollFactor) * frameTime;
                    ScrollX %= texture.width;

                    Raylib.DrawTexture(texture, (int)-ScrollX, 0, Raylib.WHITE);
                }

            foreach (var sprite in OfficeCore.OfficeState.CameraUI.Sprites.Values)
            {
                if (sprite.Visible && !string.IsNullOrEmpty(sprite.Sprite))
                {
                    var position = new Vector2(sprite.X * Globals.xMagic, sprite.Y * Globals.yMagic);
                    Raylib.DrawTextureEx(Cache.GetTexture(sprite.Sprite), position, 0, 1, Raylib.WHITE);
                }
            }

            foreach (var button in OfficeCore.OfficeState.CameraUI.Buttons)
            {
                if (string.IsNullOrEmpty(button.Value.ID))
                    continue;

                Vector2 position = new(button.Value.X * Globals.xMagic, button.Value.Y * Globals.yMagic);
                if (!GameCache.Buttons.TryGetValue(button.Value.ID, out var cachedButton))
                {
                    cachedButton = new(position, texture: Cache.GetTexture(button.Value.Sprite));
                    GameCache.Buttons[button.Value.ID] = cachedButton;
                }
                cachedButton.Draw(position);
            }

        }
    }
}
