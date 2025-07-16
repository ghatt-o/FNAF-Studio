using FNaFStudio_Runtime.Data;
using FNaFStudio_Runtime.Data.Definitions;
using FNaFStudio_Runtime.Data.Definitions.GameObjects;
using Raylib_CsLo;
using System.Numerics;

namespace FNaFStudio_Runtime.Office.Scenes;
public class CameraHandler : IScene
{
    private static float _direction = -1;
    private static float _timeSinceSwitch;
    public string Name => "CameraHandler";
    public SceneType Type => SceneType.Cameras;

    public void Update()
    {
        var deltaTime = Raylib.GetFrameTime();

        foreach (var camera in OfficeCore.OfficeState?.Cameras.Values)
        {
            camera.Update(deltaTime);
        }
    }

    public void Draw()
    {
        if (OfficeCore.LoadingLock || OfficeCore.Office == null || OfficeCore.OfficeState == null) return;

        float deltaTime = Raylib.GetFrameTime();

        var curCam = OfficeCore.OfficeState.Cameras[OfficeCore.OfficeState.Player.CurrentCamera];
        if (curCam.States.TryGetValue(curCam.State, out var path))
            if (!string.IsNullOrEmpty(path))
            {
                if (!curCam.Interrupted)
                {
                    var curState = Cache.GetTexture(path);
                    float maxScroll = Math.Abs(curState.width - 1280),
                        scroll = curCam.Scroll * 30,
                        deltaTimeScaled = deltaTime * 100;
                    var velocity = Math.Clamp((int)Math.Abs(scroll - 640), 0, 640);
                    _timeSinceSwitch += deltaTimeScaled;
                    if (_timeSinceSwitch >= 500)
                    {
                        if (Math.Abs(GameState.ScrollX - maxScroll) >= maxScroll || GameState.ScrollX == 0)
                        {
                            _direction = -_direction;
                            _timeSinceSwitch = 0;
                        }
                        GameState.ScrollX += _direction * (velocity < 320 ? 0.1f : velocity < 640 ? 0.3f : 0.5f) * deltaTimeScaled;
                    }
                    GameState.ScrollX = Math.Clamp(GameState.ScrollX, 0, maxScroll);
                    if (curCam.Panorama)
                        Raylib.BeginShaderMode(GameCache.PanoramaShader);
                    Raylib.DrawTexture(curState, (int)-Math.Round(GameState.ScrollX), 0, Raylib.WHITE);
                    if (curCam.Panorama)
                        Raylib.EndShaderMode();
                    Raylib.DrawTexture(curState, (int)-Math.Round(GameState.ScrollX), 0, Raylib.WHITE);
                }
                else
                {
                    Raylib.DrawTexture(Cache.GetTexture("e.signalinterrupted"), 470, 130, Raylib.WHITE);
                }
            }
        foreach (var sprite in OfficeCore.OfficeState.CameraUI.Sprites.Values)
        {
            if (!sprite.Visible || string.IsNullOrEmpty(sprite.Sprite))
                continue;
            var position = new Vector2(sprite.X * Globals.XMagic, sprite.Y * Globals.YMagic);
            Raylib.DrawTextureEx(Cache.GetTexture(sprite.Sprite), position, 0, 1, Raylib.WHITE);
        }
        foreach (var (key, button) in OfficeCore.OfficeState.CameraUI.Buttons)
        {
            if (string.IsNullOrEmpty(button.Sprite))
                continue;

            string uid = string.Concat(key, button.Sprite);

            Vector2 position = new(button.X * Globals.XMagic, button.Y * Globals.YMagic);

            if (!GameCache.Buttons.TryGetValue(uid, out var cachedButton))
            {
                var texture = Cache.GetTexture(button.Sprite);
                cachedButton = new Button2D(position, texture: texture, isMovable: false, id: uid);

                if (!string.IsNullOrEmpty(key))
                {
                    cachedButton.OnClick(() => OfficeCore.OfficeState.Player.SetCamera(key));
                }
                GameCache.Buttons[uid] = cachedButton;
            }

            cachedButton.Draw(position);
        }

        OfficeUtils.DrawHud();
    }
}