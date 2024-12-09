using System.Numerics;
using FNAFStudio_Runtime_RCS.Data;
using FNAFStudio_Runtime_RCS.Data.CRScript;
using FNAFStudio_Runtime_RCS.Data.Definitions;
using FNAFStudio_Runtime_RCS.Data.Definitions.GameObjects;
using FNAFStudio_Runtime_RCS.Menus;
using FNAFStudio_Runtime_RCS.Office.Definitions;
using FNAFStudio_Runtime_RCS.Office.Scenes;
using FNAFStudio_Runtime_RCS.Util;
using Raylib_CsLo;

namespace FNAFStudio_Runtime_RCS.Office;

public class OfficeUtils
{
    public static bool StartOffice(int Night)
    {
        if (!string.IsNullOrEmpty(OfficeCore.Office))
        {
            Logger.LogAsync("OfficeUtils", "Starting Office.");
            OfficeCore.LoadingLock = true;
            RuntimeUtils.Scene.SetScene(SceneType.Office);
            SoundPlayer.KillAllAsync().Wait();

            ReloadOfficeData(Night);

            foreach (var script in GameState.Project.OfficeScripts)
            {
                Logger.LogAsync("OfficeUtils", $"Starting Office Script: {script.Key}");
                EventManager.RunScript(script.Value);
            }

            GameState.Clock.Restart();
            OfficeCore.LoadingLock = false;
            EventManager.TriggerEvent("on_engine_start", []);
            TimeManager.Start();
            TimeManager.OnTimeUpdate(() =>
            {
                if (TimeManager.GetTime().hours >= 6)
                {
                    TimeManager.Stop();
                    EventManager.TriggerEvent("on_night_end", []);
                    //if (GameState.CurrentScene.Name != "Menus")
                    //  RuntimeUtils.Scene.SetScene(SceneType.Menu);
                    MenuUtils.GotoMenu("6AM");
                }
            });
            EventManager.TriggerEvent("on_night_start", []);
            SoundPlayer.PlayOnChannelAsync(GameState.Project.Sounds.Ambience, true, 1).Wait();
            if (GameState.Project.Sounds.PhoneCalls.Count >= Night && GameState.Project.Sounds.PhoneCalls[Night-1] != null)
                SoundPlayer.PlayOnChannelAsync(GameState.Project.Sounds.PhoneCalls[Night-1], false, 4).Wait();
            return true;
        }

        return false;
    }

    public static void ReloadOfficeData(int Night)
    {
        ResetHUD();
        if (OfficeCore.Office != null && OfficeCore.OfficeCache.TryGetValue(OfficeCore.Office, out var OfficeState))
        {
            GameCache.Buttons.Clear();
            Cache.Animations.Clear();
            OfficeCore.OfficeState = OfficeState;
            OfficeHandler.ScrollX = 0;
            return;
        }

        if (OfficeCore.Office != null && GameState.Project.Offices.TryGetValue(OfficeCore.Office, out var StaticOffice))
        {
            GameCache.Buttons.Clear();
            Cache.Animations.Clear();
            OfficeCore.OfficeState = new OfficeGame(Night);
            if (StaticOffice.Power != null)
                OfficeCore.OfficeState.Power = new OfficePower
                {
                    Enabled = StaticOffice.Power.Enabled,
                    Level = StaticOffice.Power.StartingLevel,
                    AnimatronicJumpscare = StaticOffice.Power.Animatronic ?? "",
                    Usage = 0, // 1 bar
                    UCN = StaticOffice.Power.Ucn
                };
            var Office = OfficeCore.OfficeState.Office;
            Office.States = StaticOffice.States;

            foreach (var obj in StaticOffice.Objects)
            {
                if (string.IsNullOrEmpty(obj.ID)) continue;
                Office.Objects.TryAdd(obj.ID,
                    new OfficeData.OfficeSprite { Visible = true, AbovePanorama = true, Hovered = false });
                switch (obj.Type)
                {
                    case "sprite":
                        Office.Sprites.TryAdd(obj.ID,
                            new OfficeData.OfficeSprite { Visible = true, AbovePanorama = true, Hovered = false });
                        break;
                    case "light_button":
                        Office.Lights.TryAdd(obj.ID, new OfficeData.OfficeLight { IsOn = false, Clickable = true });
                        break;
                    case "animation":
                        Office.Animations.TryAdd(obj.ID, new OfficeData.OfficeAnimation
                        {
                            Visible = true,
                            AbovePanorama = false,
                            Hovered = false,
                            Id = obj.Animation ?? "",
                            IsPlaying = true,
                            Rev = false
                        });
                        break;
                    case "door" when obj.Animation != null:
                        var doorAnim = Cache.GetAnimation(obj.Animation, false);
                        doorAnim.Reverse();
                        doorAnim.End();
                        Office.Doors.TryAdd(obj.ID, new OfficeData.OfficeDoor
                        {
                            Animation = doorAnim,
                            CloseSound = obj.Close_Sound,
                            OpenSound = obj.Open_Sound,
                            IsClosed = false,
                            Button = new OfficeData.OfficeButton { IsOn = false, Clickable = true }
                        });
                        break;
                }
            }

            OfficeHandler.ScrollX = 0;
            if (OfficeCore.Office != null)
                OfficeCore.OfficeCache.TryAdd(OfficeCore.Office, OfficeCore.OfficeState);
        }

        // Animatronics
        if (OfficeCore.OfficeState != null && OfficeCore.OfficeState.Animatronics.Count < 1)
            foreach (var anim in GameState.Project.Animatronics)
                OfficeCore.OfficeState.Animatronics.Add(anim.Key, new OfficeAnimatronic
                {
                    AI = anim.Value.AI ?? [],
                    Phantom = anim.Value.Phantom,
                    Script = anim.Value.Script,
                    Path = anim.Value.Path ?? [],
                    Scare = RuntimeUtils.ListToOfficeJumpscare(anim.Value.Jumpscare ?? []),
                    IgnoresMask = anim.Value.IgnoreMask,
                    LocationIndex = 0,
                    Location = (anim.Value.Path ?? []).FirstOrDefault() ?? new GameJson.PathNode(),
                    Name = anim.Key,
                    State = ""
                });
        // Cameras
        if (OfficeCore.OfficeState != null && OfficeCore.OfficeState.Cameras.Count < 1)
        {
            foreach (var cam in GameState.Project.Cameras)
            {
                if (cam.Key == "CamUI" || cam.Key == "UI" || cam.Key == "CameraUI") continue;
                OfficeCore.OfficeState.Cameras.Add(cam.Key, new OfficeCamera
                {
                    Panorama = cam.Value.Panorama,
                    State = "Default",
                    States = cam.Value.States,
                    Static = cam.Value.Static
                });
            }

            OfficeCore.OfficeState.CameraUI = new GameJson.CamUI
            {
                Sprites = GameState.Project.Cameras["CamUI"].Sprites,
                Buttons = GameState.Project.Cameras["CamUI"].Buttons,
                Blip = GameState.Project.Cameras["CamUI"].Blip,
                UI = GameState.Project.Cameras["CamUI"].UI,
                MusicBox = GameState.Project.Cameras["CamUI"].MusicBox
            };
        }

        // UIButtons
        if (OfficeCore.Office != null)
            if (GameState.Project.Offices[OfficeCore.Office].OldUIButtons !=
                null) // Convert from old UIBtn system to the new one
            {
                GameState.Project.Offices[OfficeCore.Office].UIButtons = new Dictionary<string, GameJson.UIButton>
                {
                    {
                        "camera",
                        new GameJson.UIButton
                        {
                            UI = new GameJson.UI
                            {
                                IsToxic = false,
                                Text = ""
                            },
                            Input = RuntimeUtils.ConvertOldCamera()
                        }
                    },
                    {
                        "mask",
                        new GameJson.UIButton
                        {
                            UI = new GameJson.UI
                            {
                                IsToxic = false,
                                Text = ""
                            },
                            Input = RuntimeUtils.ConvertOldMask()
                        }
                    }
                };

                GameState.Project.Offices[OfficeCore.Office].OldUIButtons = null;
            }

        if (OfficeCore.OfficeState != null && OfficeCore.OfficeState.UIButtons.Count < 1 && OfficeCore.Office != null)
        {
            OfficeCore.OfficeState.UIButtons.Add("camera", new GameJson.UIButton
            {
                Input = GameState.Project.Offices[OfficeCore.Office].UIButtons["camera"].Input,
                UI = GameState.Project.Offices[OfficeCore.Office].UIButtons["camera"].UI
            });
            OfficeCore.OfficeState.UIButtons.Add("mask", new GameJson.UIButton
            {
                Input = RuntimeUtils.DeepCopyInput(
                    GameState.Project.Offices[OfficeCore.Office].UIButtons["mask"].Input ?? new GameJson.Input()),
                UI = RuntimeUtils.DeepCopyUI(GameState.Project.Offices[OfficeCore.Office].UIButtons["mask"].UI ?? new GameJson.UI())
            });
        }
    }

    private static void ToggleCams()
    {
        if (OfficeCore.OfficeState == null) return;

        (string, SceneType) checks = GameState.CurrentScene.Name == "CameraHandler" ?
        (GameState.Project.Sounds.Camdown, SceneType.Office) : (GameState.Project.Sounds.Camup, SceneType.Cameras);
        SoundPlayer.PlayOnChannelAsync(checks.Item1, false, 2).Wait();
        RuntimeUtils.Scene.SetScenePreserve(checks.Item2);
    }

    private static void ToggleMask()
    {
        if (OfficeCore.OfficeState == null) return;

        string maskSound = OfficeCore.OfficeState.Player.IsMaskOn ? 
            GameState.Project.Sounds.Maskoff : GameState.Project.Sounds.Maskon;

        SoundPlayer.PlayOnChannelAsync(maskSound, false, 3).Wait();
        GameCache.HudCache.MaskAnim.Resume();
        GameCache.HudCache.MaskAnim.Show();
    }

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
                button.OnClickAsync(() =>
                {
                    if (doorVars.Animation != null && !doorVars.Animation.Current().HasFramesLeft())
                    {
                        Toggle(ref doorVars.IsClosed);
                        Toggle(ref doorVars.Button.IsOn);

                        if (doorVars.IsClosed) SoundPlayer.PlayOnChannelAsync(doorVars.CloseSound, false, 13).Wait();
                        else SoundPlayer.PlayOnChannelAsync(doorVars.OpenSound, false, 13).Wait();
                        doorVars.Animation.Reverse();
                    }

                    return Task.CompletedTask;
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
            async Task HandleToggle()
            {
                if (OfficeCore.OfficeState != null)
                {
                    var stateParts = OfficeCore.OfficeState.Office.State.Split(':');
                    var isLightOn = OfficeCore.OfficeState.Office.Lights[obj.ID].IsOn;
                    if (stateParts.Length == 2 && !isLightOn)
                    {
                        Toggle(ref OfficeCore.OfficeState.Office.Lights[stateParts[0]].IsOn);
                        OfficeCore.OfficeState.Office.State = $"{obj.ID}:{stateParts[1]}";
                    }
                    else
                    {
                        OfficeCore.OfficeState.Office.State = stateParts.Length == 2
                            ? stateParts[1]
                            : $"{obj.ID}:{OfficeCore.OfficeState.Office.State}";
                    }

                    if (!isLightOn) await SoundPlayer.PlayOnChannelAsync(obj.Sound, true, 12);
                    else await SoundPlayer.StopChannelAsync(12);

                    Toggle(ref OfficeCore.OfficeState.Office.Lights[obj.ID].IsOn);
                }

                await Task.CompletedTask;
            }

            button.OnClickAsync(HandleToggle);

            // FNaF 2 is the one where you need
            // to hold the button, not FNaF 1
            if (obj.Clickstyle)
                button.OnReleaseAsync(HandleToggle); // Works :thumbs_up~1:
        });
    }

    public static void ResetHUD()
    {
        GameCache.HudCache.Power = new("", 26, "Consolas", Raylib.WHITE);
        GameCache.HudCache.Usage = new("", 26, "Consolas", Raylib.WHITE);
        GameCache.HudCache.Time = new("", 26, GameState.Project.Offices[OfficeCore.Office ?? "Office"].TextFont ?? "LCD Solid", Raylib.WHITE);
        GameCache.HudCache.Night = new("", 22, GameState.Project.Offices[OfficeCore.Office ?? "Office"].TextFont ?? "LCD Solid", Raylib.WHITE);

        GameCache.HudCache.CameraAnim.Reset();
        GameCache.HudCache.MaskAnim.Reset();

        GameCache.HudCache.CameraAnim.OnPlay(ToggleCams, AnimationState.Reverse);
        GameCache.HudCache.CameraAnim.OnFinish(ToggleCams, AnimationState.Normal);
        GameCache.HudCache.CameraAnim.OnFinish(() => 
        {
            // this somehow executes before the toggle which causes
            // a singular frame of office to sneak into cams
            // TODO: prevent hiding the anim till we are fully loaded into cams
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

        GameCache.HudCache.Usage.Content = $"Usage: {OfficeCore.OfficeState.Power.Usage + 1}";
        GameCache.HudCache.Usage.Draw(new(38, 637));

        var minutes = TimeManager.GetTime().hours;
        GameCache.HudCache.Time.Content = $"{(minutes == 0 ? " 12" : minutes)} AM";
        GameCache.HudCache.Time.Draw(new(minutes != 0 ? 1160 : 1165, 10));

        GameCache.HudCache.Night.Content = $"Night {OfficeCore.OfficeState.Night}";
        GameCache.HudCache.Night.Draw(new(1160, 45));

        DrawUIButtons();

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
            bool visible = (UIButton.Key == "camera") ? 
                (!OfficeCore.OfficeState.Player.IsMaskOn) : UIButton.Key != "mask" || (!OfficeCore.OfficeState.Player.IsCameraUp);

            if (UIButton.Value.Input?.Position == null) continue;

            Vector2 position = new((int)(UIButton.Value.Input.Position[0] * Globals.xMagic),
                (int)(UIButton.Value.Input.Position[1] * Globals.yMagic));

            if (!GameCache.Buttons.TryGetValue(UIButton.Key, out var button))
            {
                button = new Button2D(position, id: UIButton.Key, IsMovable: false,
                    texture: Cache.GetTexture(UIButton.Value.Input.Image)
                );

                button.OnUnHoverAsync(async () =>
                {
                    if (button.ID == "camera")
                    {
                        OfficeCore.OfficeState.Player.IsCameraUp = GameCache.HudCache.CameraAnim.State == AnimationState.Normal;
                        GameCache.HudCache.CameraAnim.Show();
                    }
                    else if (button.ID == "mask")
                    {
                        OfficeCore.OfficeState.Player.IsMaskOn = GameCache.HudCache.MaskAnim.State == AnimationState.Normal;
                        ToggleMask();
                    }
                    await Task.CompletedTask;
                });

                GameCache.Buttons[UIButton.Key] = button;
            }

            button.IsVisible = visible;
            button.Draw(position);
        }
    }
}