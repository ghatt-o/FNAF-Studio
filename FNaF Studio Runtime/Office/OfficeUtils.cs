using System.Numerics;
using System.Runtime.CompilerServices;
using FNaFStudio_Runtime.Data;
using FNaFStudio_Runtime.Data.Definitions;
using FNaFStudio_Runtime.Data.Definitions.GameObjects;
using FNaFStudio_Runtime.Util;
using Raylib_CsLo;

namespace FNaFStudio_Runtime.Office;

public class OfficeUtils
{
    private static void Toggle(ref bool state)
    {
        state = !state;
    }

    public static Button2D GetButtonWithCallback(string id, GameJson.OfficeObject obj, Action<Button2D>? setup = null)
    {
        if (!GameCache.Buttons.TryGetValue(id, out var button) && obj.Position != null &&
            !string.IsNullOrEmpty(obj.Sprite))
        {
            var tex = Cache.GetTexture(obj.Sprite);
            button = new Button2D(new Vector2(obj.Position[0] * Globals.xMagic, obj.Position[1] * Globals.yMagic), obj,
                texture: tex);
            setup?.Invoke(button);
            GameCache.Buttons[id] = button;
        }

        return button!;
    }

    public static Button2D GetDoorButton(string id, GameJson.OfficeObject obj)
    {
        return GetButtonWithCallback(id, obj, button =>
        {
            if (OfficeCore.OfficeState != null && obj.ID != null)
            {
                var doorVars = OfficeCore.OfficeState.Office.Doors[obj.ID];
                button.OnClick(() =>
                {
                    if (doorVars.Animation != null && !doorVars.Animation.Current().HasFramesLeft())
                    {
                        Toggle(ref doorVars.IsClosed);
                        Toggle(ref doorVars.Button.IsOn);

                        if (doorVars.IsClosed)
                        {
                            SoundPlayer.PlayOnChannelAsync(doorVars.CloseSound, false, 13).Wait();
                            OfficeCore.OfficeState.Power.Usage += 1;
                        }
                        else
                        {
                            SoundPlayer.PlayOnChannelAsync(doorVars.OpenSound, false, 13).Wait();
                            OfficeCore.OfficeState.Power.Usage -= 1;
                        }
                        doorVars.Animation.Reverse();
                    }
                });
            }
        });
    }

    // TODO: new lights and flashlight format for office states:
    // ----------------------------------------------------------------------------------------------
    // Format: FlashlightID:[flashAnims], LightID:[lightAnims], OfficeAnims
    // Examples: 
    // - Flashlight:[Withered Foxy]
    // - Flashlight:[Toy Bonnie, Toy Chica], Toy Bonnie
    // - LeftLight:[Toy Chica], Flashlight:[Toy Freddy], RightLight:[Toy Bonnie], Withered Bonnie
    //
    // Note: Flashlight is treated as a built-in Light and the only reason it wasn't removed was for
    // compatibility with previous FNaF Engine and FNAF Studio versions, since the new Lights are
    // extremely featureful there is no reason to have Flashlight except for backwards compatability
    // with scripts that use DisableFlashlight etc, and the keybind would be used for HandleToggle
    // 
    // I'll write a converter that converts the old format into this new one, because they're not
    // that different and it's easy to write a converter for it.
    // This also would not be order dependant, I'm thinking of using some sort of system to
    // reduce the number of office states the user would have to render...
    // ----------------------------------------------------------------------------------------------
    public static Button2D GetLightButton(string id, GameJson.OfficeObject obj)
    {
        if (OfficeCore.OfficeState == null || obj.ID == null)
            return GetButtonWithCallback(id, obj, _ => { });

        return GetButtonWithCallback(id, obj, button =>
        {
            void HandleToggle()
            {
                if (OfficeCore.OfficeState != null)
                {
                    var stateParts = OfficeCore.OfficeState.Office.State.Split(':');
                    var isLightOn = OfficeCore.OfficeState.Office.Lights[obj.ID].IsOn;
                    if (stateParts.Length == 2 && !isLightOn)
                    {
                        Toggle(ref OfficeCore.OfficeState.Office.Lights[stateParts[0]].IsOn);
                        OfficeCore.OfficeState.Power.Usage -= 1;
                        OfficeCore.OfficeState.Office.State = $"{obj.ID}:{stateParts[1]}";
                    }
                    else
                    {
                        OfficeCore.OfficeState.Office.State = stateParts.Length == 2
                            ? stateParts[1]
                            : $"{obj.ID}:{OfficeCore.OfficeState.Office.State}";
                    }

                    if (!isLightOn)
                    {
                        SoundPlayer.PlayOnChannelAsync(obj.Sound, true, 12).Wait();
                        OfficeCore.OfficeState.Power.Usage += 1;
                    }
                    else 
                    {
                        SoundPlayer.StopChannelAsync(12).Wait();
                        OfficeCore.OfficeState.Power.Usage -= 1;
                    }

                    Toggle(ref OfficeCore.OfficeState.Office.Lights[obj.ID].IsOn);
                }
            }

            button.OnClick(HandleToggle);

            if (obj.Clickstyle)
                button.OnRelease(HandleToggle);
        });
    }

    private static void ToggleCams()
    {
        if (OfficeCore.OfficeState == null) return;

        (string, SceneType, int) checks = GameState.CurrentScene.Name == "CameraHandler" ?
        (GameState.Project.Sounds.Camdown, SceneType.Office, -1) : (GameState.Project.Sounds.Camup, SceneType.Cameras, 1);
        SoundPlayer.PlayOnChannelAsync(checks.Item1, false, 2).Wait();
        RuntimeUtils.Scene.SetScenePreserve(checks.Item2);
        OfficeCore.OfficeState.Power.Usage += checks.Item3;
    }

    private static void ToggleMask()
    {
        if (OfficeCore.OfficeState == null) return;

        OfficeCore.OfficeState.Player.IsMaskOn = GameCache.HudCache.MaskAnim.State == AnimationState.Normal;
        string maskSound = OfficeCore.OfficeState.Player.IsMaskOn ?
            GameState.Project.Sounds.Maskoff : GameState.Project.Sounds.Maskon;
        SoundPlayer.PlayOnChannelAsync(maskSound, false, 3).Wait();
        GameCache.HudCache.MaskAnim.Resume();
        GameCache.HudCache.MaskAnim.Show();
    }

    public static void ResetHUD()
    {
        GameCache.HudCache.Power = new("", 26, GameState.Project.Offices[OfficeCore.Office ?? "Office"].TextFont ?? "LCD Solid", Raylib.WHITE);
        GameCache.HudCache.Usage = new("", 26, GameState.Project.Offices[OfficeCore.Office ?? "Office"].TextFont ?? "LCD Solid", Raylib.WHITE);
        GameCache.HudCache.Time = new("", 26, GameState.Project.Offices[OfficeCore.Office ?? "Office"].TextFont ?? "LCD Solid", Raylib.WHITE);
        GameCache.HudCache.Night = new("", 22, GameState.Project.Offices[OfficeCore.Office ?? "Office"].TextFont ?? "LCD Solid", Raylib.WHITE);

        GameCache.HudCache.CameraAnim.Reset();
        GameCache.HudCache.MaskAnim.Reset();

        GameCache.HudCache.CameraAnim.OnPlay(ToggleCams, AnimationState.Reverse);
        GameCache.HudCache.CameraAnim.OnFinish(ToggleCams, AnimationState.Normal);
        GameCache.HudCache.CameraAnim.OnFinish(() => 
        {
            // this executes the first time we play an animation
            // for some reason and it causes a single frame of
            // office to appear when opening the camera panel
            GameCache.HudCache.CameraAnim.Hide();
            GameCache.HudCache.CameraAnim.Reverse();
        });

        GameCache.HudCache.MaskAnim.OnPlay(() => SoundPlayer.StopChannelAsync(8).Wait(), AnimationState.Reverse);
        GameCache.HudCache.MaskAnim.OnFinish(GameCache.HudCache.MaskAnim.Hide, AnimationState.Reverse);
        GameCache.HudCache.MaskAnim.OnFinish(GameCache.HudCache.MaskAnim.Reverse);
        GameCache.HudCache.MaskAnim.OnFinish(() =>
        {
            GameCache.HudCache.MaskAnim.Pause();
            SoundPlayer.PlayOnChannelAsync(GameState.Project.Sounds.MaskBreathing, true, 8).Wait();
        }, AnimationState.Normal);

        GameCache.HudCache.CameraAnim.Hide();
        GameCache.HudCache.MaskAnim.Hide();
    }

    public static void DrawHUD()
    {
        if (OfficeCore.Office == null || OfficeCore.OfficeState == null)
        {
            Logger.LogWarnAsync("OfficeUtils: DrawHUD", "OfficeCore.Office/OfficeState is null!");
            return;
        }

        GameCache.HudCache.CameraAnim.AdvanceDraw(Vector2.Zero);
        GameCache.HudCache.MaskAnim.AdvanceDraw(Vector2.Zero);

        GameCache.HudCache.Power.Content = $"Power Left: {OfficeCore.OfficeState.Power.Level}%";
        GameCache.HudCache.Power.Draw(new(38, 601));

        GameCache.HudCache.Usage.Content = $"Usage: ";
        GameCache.HudCache.Usage.Draw(new(38, 637));
        Raylib.DrawTexture(Cache.GetTexture($"e.usage_{Math.Clamp(OfficeCore.OfficeState.Power.Usage, 0, 4) + 1}"), 136, 634, Raylib.WHITE);
        

        var minutes = TimeManager.GetTime().hours;
        GameCache.HudCache.Time.Content = $"{(minutes == 0 ? " 12" : minutes)} AM";
        GameCache.HudCache.Time.Draw(new(minutes == 0 ? 1160 : 1165, 10));

        GameCache.HudCache.Night.Content = $"Night {OfficeCore.OfficeState.Night}";
        GameCache.HudCache.Night.Draw(new(1160, 45));

        DrawUIButtons();

        if (OfficeCore.OfficeState.Settings.Toxic)
        {
            var player = OfficeCore.OfficeState.Player;
            player.ToxicLevel = Math.Clamp(player.ToxicLevel + (player.IsMaskOn ? 50 : -50) * Raylib.GetFrameTime(), 0, 280);

            if (player.IsMaskOn && player.ToxicLevel >= 280 && player.MaskEnabled)
            {
                player.MaskEnabled = false;
                ToggleMask();
                SoundPlayer.PlayOnChannelAsync(GameState.Project.Sounds.MaskToxic, false, 3).Wait();
            }

            if (player.IsMaskOn || player.ToxicLevel > 0)
            {
                float toxicLevel = player.ToxicLevel / 280f;
                Color color = new((int)(toxicLevel * 255), (int)((1 - toxicLevel) * 255), 0, 255);
                Raylib.DrawTexture(Cache.GetTexture("e.toxic"), 25, 24, Raylib.WHITE);
                Raylib.DrawRectangle(30, 47, (int)Math.Clamp(toxicLevel * 114, 0, 114), 20, color);
            }

            player.MaskEnabled |= player.ToxicLevel <= 0;
        }

        if (GameState.DebugMode)
        {
            var offsetX = 950;
            var offsetY = 88;
            Raylib.DrawText("Time", 44 + offsetX, 88 - offsetY, 22, Raylib.WHITE);
            Raylib.DrawText("Seconds: " + TimeManager.GetTime().seconds, 88 + offsetX, 110 - offsetY, 22, Raylib.WHITE);
            Raylib.DrawText("Minutes: " + TimeManager.GetTime().minutes, 88 + offsetX, 132 - offsetY, 22, Raylib.WHITE);

            Raylib.DrawText("Animatronics", 44 + offsetX, 176 - offsetY, 22, Raylib.WHITE);
            var i = 0;
            var posY = 0;
            foreach (var anim in OfficeCore.OfficeState.Animatronics)
            {
                i++;
                posY = 176 + 22 * i;
                Raylib.DrawText(anim.Value.Name, 88 + offsetX, posY - offsetY, 22, Raylib.WHITE);
            }

            Raylib.DrawText("Cameras", 44 + offsetX, posY + 44 - offsetY, 22, Raylib.WHITE);
            var i2 = 0;
            foreach (var cam in OfficeCore.OfficeState.Cameras)
            {
                i2++;
                var camPosY = posY + 44 + 22 * i2;
                Raylib.DrawText(cam.Key, 88 + offsetX, camPosY - offsetY, 22, Raylib.WHITE);
            }
        }
    }

    public static void DrawUIButtons()
    {
        if (OfficeCore.OfficeState == null) return;

        foreach (var UIButton in OfficeCore.OfficeState.UIButtons)
        {
            if (UIButton.Value.Input?.Position == null) continue;

            Vector2 position = new((int)(UIButton.Value.Input.Position[0] * Globals.xMagic),
                (int)(UIButton.Value.Input.Position[1] * Globals.yMagic));

            if (!GameCache.Buttons.TryGetValue(UIButton.Key, out var button))
            {
                lock (GameState.buttonsLock)
                {
                    button = new Button2D(position, id: UIButton.Key, IsMovable: false,
                    texture: Cache.GetTexture(UIButton.Value.Input.Image)
                );

                    button.OnUnHover(() =>
                    {
                        if (button.ID == "camera")
                        {
                            OfficeCore.OfficeState.Player.IsCameraUp = GameCache.HudCache.CameraAnim.State == AnimationState.Normal;
                            GameCache.HudCache.CameraAnim.Show();
                        }
                        else if (button.ID == "mask")
                        {
                            ToggleMask();
                        }
                    });

                    GameCache.Buttons[UIButton.Key] = button;
                }
            }

            // This single expression reduced the total CPU time
            // by 1 ms (huge performance, atleast 100 FPS more)
            button.IsVisible = (UIButton.Key == "camera") ?
                (!OfficeCore.OfficeState.Player.IsMaskOn && OfficeCore.OfficeState.Player.CameraEnabled) :
                UIButton.Key != "mask" || (!OfficeCore.OfficeState.Player.IsCameraUp && OfficeCore.OfficeState.Player.MaskEnabled); ;
            button.Draw(position);
        }
    }
}