using FNaFStudio_Runtime.Data;
using FNaFStudio_Runtime.Data.Definitions;
using FNaFStudio_Runtime.Data.Definitions.GameObjects;
using FNaFStudio_Runtime.Util;
using Raylib_CsLo;
using System.Numerics;

namespace FNaFStudio_Runtime.Office;

public abstract class OfficeUtils
{
    private static void Toggle(ref bool state)
    {
        state = !state;
    }

    private static Button2D GetButtonWithCallback(string id, GameJson.OfficeObject obj, Action<Button2D>? setup = null)
    {
        if (GameCache.Buttons.TryGetValue(id, out var button) ||
            string.IsNullOrEmpty(obj.Sprite)) return button!;
        var tex = Cache.GetTexture(obj.Sprite);
        button = new Button2D(new Vector2(obj.Position[0] * Globals.XMagic, obj.Position[1] * Globals.YMagic), obj,
            texture: tex);
        setup?.Invoke(button);
        GameCache.Buttons[id] = button;

        return button;
    }

    public static Button2D GetDoorButton(string id, GameJson.OfficeObject obj)
    {
        return GetButtonWithCallback(id, obj, button =>
        {
            if (OfficeCore.OfficeState == null) return;
            var doorVars = OfficeCore.OfficeState.Office.Doors[obj.ID];
            button.OnClick(() =>
            {
                if (doorVars.Animation == null || doorVars.Animation.Current().HasFramesLeft()) return;
                Toggle(ref doorVars.IsClosed);
                Toggle(ref doorVars.Button.IsOn);

                if (doorVars.IsClosed)
                {
                    SoundPlayer.PlayOnChannel(doorVars.CloseSound, false, 13);
                    OfficeCore.OfficeState.Power.Usage += 1;
                }
                else
                {
                    SoundPlayer.PlayOnChannel(doorVars.OpenSound, false, 13);
                    OfficeCore.OfficeState.Power.Usage -= 1;
                }
                doorVars.Animation.Reverse();
            });
        });
    }

    public static Button2D GetLightButton(string id, GameJson.OfficeObject obj)
    {
        if (OfficeCore.OfficeState == null)
            return GetButtonWithCallback(id, obj, _ => { });

        return GetButtonWithCallback(id, obj, button =>
        {
            button.OnClick(HandleToggle);

            if (obj.Clickstyle)
                button.OnRelease(HandleToggle);
            return;

            void HandleToggle()
            {
                if (OfficeCore.OfficeState == null) return;
                var stateParts = OfficeCore.OfficeState.Office.State.Split(':');
                var isLightOn = OfficeCore.OfficeState.Office.Lights[obj.ID].IsOn;
                if (stateParts.Length == 2 && !isLightOn)
                {
                    Toggle(ref OfficeCore.OfficeState.Office.Lights[stateParts[0]].IsOn);
                    OfficeCore.OfficeState.Power.Usage -= 1;

                    OfficeCore.OfficeState.Office.State = $"{obj.ID}:{(stateParts[1]
                            .Split(',')
                            .Skip(1)
                            .DefaultIfEmpty("Default")
                            .Aggregate((acc, next) => acc + "," + next)
                        )}";

                    PathFinder.OnLightTurnedOff(stateParts[1].Split(',').First());
                }
                else
                {
                    OfficeCore.OfficeState.Office.State = stateParts.Length == 2
                        ?
                        (stateParts[1]
                            .Split(',')
                            .Skip(1)
                            .DefaultIfEmpty("Default")
                            .Aggregate((acc, next) => acc + "," + next)
                        )
                        : $"{obj.ID}:{OfficeCore.OfficeState.Office.State}";
                }

                if (!isLightOn)
                {
                    SoundPlayer.PlayOnChannel(obj.Sound, true, 12);
                    OfficeCore.OfficeState.Power.Usage += 1;

                    PathFinder.OnLightTurnedOn(obj.ID);
                }
                else
                {
                    SoundPlayer.StopChannel(12);
                    OfficeCore.OfficeState.Power.Usage -= 1;

                    PathFinder.OnLightTurnedOff(stateParts[1].Split(',').First());

                }

                Toggle(ref OfficeCore.OfficeState.Office.Lights[obj.ID].IsOn);
            }
        });
    }

    private static void ToggleCams()
    {
        if (OfficeCore.OfficeState == null) return;

        SoundPlayer.SetChannelVolume(10, 0);
        var checks = GameState.CurrentScene.Name == "CameraHandler" ?
        (GameState.Project.Sounds.Camdown, SceneType.Office, -1) : (GameState.Project.Sounds.Camup, SceneType.Cameras, 1);
        SoundPlayer.PlayOnChannel(checks.Item1, false, 2);
        RuntimeUtils.Scene.SetScenePreserve(checks.Item2);
        OfficeCore.OfficeState.Power.Usage += checks.Item3;
    }

    private static void ToggleMask()
    {
        if (OfficeCore.OfficeState == null) return;

        OfficeCore.OfficeState.Player.IsMaskOn = GameCache.HudCache.MaskAnim.State == AnimationState.Normal;
        var maskSound = OfficeCore.OfficeState.Player.IsMaskOn ?
            GameState.Project.Sounds.Maskoff : GameState.Project.Sounds.Maskon;
        SoundPlayer.PlayOnChannel(maskSound, false, 3);
        GameCache.HudCache.MaskAnim.Resume();
        GameCache.HudCache.MaskAnim.Show();
    }

    public static void ResetHud()
    {
        GameCache.HudCache.Power = new Text("", 26, GameState.Project.Offices[OfficeCore.Office ?? "Office"].TextFont ?? "LCD Solid", Raylib.WHITE);
        GameCache.HudCache.Usage = new Text("", 26, GameState.Project.Offices[OfficeCore.Office ?? "Office"].TextFont ?? "LCD Solid", Raylib.WHITE);
        GameCache.HudCache.Time = new Text("", 26, GameState.Project.Offices[OfficeCore.Office ?? "Office"].TextFont ?? "LCD Solid", Raylib.WHITE);
        GameCache.HudCache.Night = new Text("", 22, GameState.Project.Offices[OfficeCore.Office ?? "Office"].TextFont ?? "LCD Solid", Raylib.WHITE);

        GameCache.HudCache.CameraAnim.Reset();
        GameCache.HudCache.MaskAnim.Reset();

        GameCache.HudCache.CameraAnim.OnPlay(ToggleCams, AnimationState.Reverse);
        GameCache.HudCache.CameraAnim.OnFinish(ToggleCams, AnimationState.Normal);
        GameCache.HudCache.CameraAnim.OnFinish(() =>
        {
            // this executes the first time we play an animation
            // for some reason, and it causes a single frame of
            // office to appear when opening the camera panel
            GameCache.HudCache.CameraAnim.Hide();
            GameCache.HudCache.CameraAnim.Reverse();
        });

        GameCache.HudCache.MaskAnim.OnPlay(() => SoundPlayer.StopChannel(8), AnimationState.Reverse);
        GameCache.HudCache.MaskAnim.OnFinish(GameCache.HudCache.MaskAnim.Hide, AnimationState.Reverse);
        GameCache.HudCache.MaskAnim.OnFinish(GameCache.HudCache.MaskAnim.Reverse);
        GameCache.HudCache.MaskAnim.OnFinish(() =>
        {
            GameCache.HudCache.MaskAnim.Pause();
            SoundPlayer.PlayOnChannel(GameState.Project.Sounds.MaskBreathing, true, 8);
        }, AnimationState.Normal);

        GameCache.HudCache.CameraAnim.Hide();
        GameCache.HudCache.MaskAnim.Hide();
    }

    public static void DrawHud()
    {
        if (OfficeCore.Office == null || OfficeCore.OfficeState == null)
        {
            Logger.LogWarnAsync("OfficeUtils: DrawHUD", "OfficeCore.Office/OfficeState is null!");
            return;
        }


        GameCache.HudCache.CameraAnim.AdvanceDraw(Vector2.Zero);
        GameCache.HudCache.MaskAnim.AdvanceDraw(Vector2.Zero);


        GameCache.HudCache.Power.Content = $"Power Left: {OfficeCore.OfficeState.Power.Level}%";
        GameCache.HudCache.Power.Draw(new Vector2(38, 601));

        GameCache.HudCache.Usage.Content = $"Usage: ";
        GameCache.HudCache.Usage.Draw(new Vector2(38, 637));
        Raylib.DrawTexture(Cache.GetTexture($"e.usage_{Math.Clamp(OfficeCore.OfficeState.Power.Usage, 0, 4) + 1}"), 136, 634, Raylib.WHITE);


        var minutes = TimeManager.GetTime().hours;
        GameCache.HudCache.Time.Content = $"{(minutes == 0 ? " 12" : minutes)} AM";
        GameCache.HudCache.Time.Draw(new Vector2(minutes == 0 ? 1160 : 1165, 10));

        GameCache.HudCache.Night.Content = $"Night {OfficeCore.OfficeState.Night}";
        GameCache.HudCache.Night.Draw(new Vector2(1160, 45));

        DrawUiButtons();

        if (OfficeCore.OfficeState.Settings.Toxic)
        {
            var player = OfficeCore.OfficeState.Player;
            player.ToxicLevel = Math.Clamp(player.ToxicLevel + (player.IsMaskOn ? 50 : -50) * Raylib.GetFrameTime(), 0, 280);

            if (player is { IsMaskOn: true, ToxicLevel: >= 280, MaskEnabled: true })
            {
                player.MaskEnabled = false;
                ToggleMask();
                SoundPlayer.PlayOnChannel(GameState.Project.Sounds.MaskToxic, false, 3);
            }

            if (player.IsMaskOn || player.ToxicLevel > 0)
            {
                var toxicLevel = player.ToxicLevel / 280f;
                Color color = new((int)(toxicLevel * 255), (int)((1 - toxicLevel) * 255), 0, 255);
                Raylib.DrawTexture(Cache.GetTexture("e.toxic"), 25, 24, Raylib.WHITE);
                Raylib.DrawRectangle(30, 47, (int)Math.Clamp(toxicLevel * 114, 0, 114), 20, color);
            }

            player.MaskEnabled |= player.ToxicLevel <= 0;
        }

        if (!GameState.DebugMode) return;
        Raylib.DrawText("Time", 44 + 950, 88 - 88, 22, Raylib.WHITE);
        Raylib.DrawText("Seconds: " + TimeManager.GetTime().seconds, 88 + 950, 110 - 88, 22, Raylib.WHITE);
        Raylib.DrawText("Minutes: " + TimeManager.GetTime().minutes, 88 + 950, 132 - 88, 22, Raylib.WHITE);

        Raylib.DrawText("Animatronics", 44 + 950, 176 - 88, 22, Raylib.WHITE);
        var i = 0;
        var posY = 0;
        foreach (var anim in OfficeCore.OfficeState.Animatronics)
        {
            i++;
            posY = 176 + 22 * i;
            Raylib.DrawText(anim.Value.Name, 88 + 950, posY - 88, 22, Raylib.WHITE);
        }

        Raylib.DrawText("Cameras", 44 + 950, posY + 44 - 88, 22, Raylib.WHITE);
        var i2 = 0;
        foreach (var cam in OfficeCore.OfficeState.Cameras)
        {
            i2++;
            var camPosY = posY + 44 + 22 * i2;
            Raylib.DrawText(cam.Key, 88 + 950, camPosY - 88, 22, Raylib.WHITE);
        }
    }

    private static void DrawUiButtons()
    {
        if (OfficeCore.OfficeState == null) return;

        foreach (var (id, value) in OfficeCore.OfficeState.UIButtons)
        {
            if (value.Input?.Position == null) continue;

            Vector2 position = new(
                (int)(value.Input.Position[0] * Globals.XMagic),
                (int)(value.Input.Position[1] * Globals.YMagic)
            );

            if (!GameCache.Buttons.TryGetValue(id, out var button))
            {
                lock (GameState.ButtonsLock)
                {
                    var texture = Cache.GetTexture(value.Input.Image);

                    button = new Button2D(position, id: id, isMovable: false, texture: texture);

                    button.OnUnHover(() => HandleUnHover(id));

                    GameCache.Buttons[id] = button;
                }
            }

            button.IsVisible = (id == "camera")
                ? (!OfficeCore.OfficeState.Player.IsMaskOn && OfficeCore.OfficeState.Player.CameraEnabled)
                : id != "mask" || (!OfficeCore.OfficeState.Player.IsCameraUp && OfficeCore.OfficeState.Player.MaskEnabled);

            button.Draw(position);
        }
    }

    private static void HandleUnHover(string id)
    {
        switch (id)
        {
            case "camera":
                if (OfficeCore.OfficeState != null)
                    OfficeCore.OfficeState.Player.IsCameraUp =
                        GameCache.HudCache.CameraAnim.State == AnimationState.Normal;
                GameCache.HudCache.CameraAnim.Show();
                break;
            case "mask":
                ToggleMask();
                break;
        }
    }
}