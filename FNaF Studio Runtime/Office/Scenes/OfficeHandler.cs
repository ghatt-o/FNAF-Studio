using FNaFStudio_Runtime.Data;
using FNaFStudio_Runtime.Data.CRScript;
using FNaFStudio_Runtime.Data.Definitions;
using FNaFStudio_Runtime.Data.Definitions.GameObjects;
using FNaFStudio_Runtime.Menus;
using FNaFStudio_Runtime.Office.Definitions;
using FNaFStudio_Runtime.Util;
using Raylib_CsLo;
using System.Numerics;

namespace FNaFStudio_Runtime.Office.Scenes;

public class OfficeHandler : IScene
{
    public string Name => "OfficeHandler";
    public SceneType Type => SceneType.Office;

    public void Update()
    {
        if (OfficeCore.OfficeState == null || OfficeCore.LoadingLock) return;
        float viewportWidth = Raylib.GetScreenWidth();
        var mousePosition = Raylib.GetMousePosition();
        var mousePositionX = mousePosition.X;

        if (!(viewportWidth < OfficeCore.CurStateWidth)) return;
        var scrollSpeed = 0.0f;
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

        var newScrollX = GameState.ScrollX + scrollSpeed * Raylib.GetFrameTime() *
            Math.Sign(mousePositionX - viewportWidth * 0.5f);
        GameState.ScrollX = Math.Clamp(newScrollX, 0.0f, OfficeCore.CurStateWidth - viewportWidth);
    }

    private static void DrawSprite(GameJson.OfficeObject obj, Vector2 objPos)
    {
        if (GameCache.Buttons.TryGetValue(obj.ID, out var imgBtn))
        {
            imgBtn.Draw(objPos);
        }
        else
        {
            var tex = Cache.GetTexture(obj.Sprite);
            var newImgBtn = new Button2D(objPos, obj, texture: tex);
            GameCache.Buttons[obj.ID] = newImgBtn;
            newImgBtn.Draw(objPos);
        }
    }

    public void Draw()
    {
        if (OfficeCore.LoadingLock || OfficeCore.Office == null || OfficeCore.OfficeState == null) return;

        Raylib.BeginTextureMode(GameCache.PanoramaTex);
        Raylib.ClearBackground(Raylib.BLACK);

        if (OfficeCore.OfficeState.Office.States.TryGetValue(OfficeCore.OfficeState.Office.State, out var texPath) &&
            !string.IsNullOrEmpty(texPath))
        {
            Texture curState = Cache.GetTexture(texPath);
            OfficeCore.CurStateWidth = curState.width;
            Raylib.DrawTexture(curState, (int)-Math.Round(GameState.ScrollX), 0, Raylib.WHITE);
        }

        foreach (var obj in GameState.Project.Offices[OfficeCore.Office].Objects)
        {
            if (!OfficeCore.OfficeState.Office.Objects[obj.ID].Visible) continue;

            Vector2 objPos = new(obj.Position[0] * Globals.XMagic - GameState.ScrollX, obj.Position[1] * Globals.YMagic);

            switch (obj.Type)
            {
                case "door_button" when OfficeCore.OfficeState.Office.Doors.TryGetValue(obj.ID, out var doorVars):
                    OfficeUtils.GetDoorButton(obj.ID, obj).Draw(objPos, doorVars.Button.IsOn);
                    break;

                case "light_button" when OfficeCore.OfficeState.Office.Lights.TryGetValue(obj.ID, out var lightVars):
                    OfficeUtils.GetLightButton(obj.ID, obj).Draw(objPos, lightVars.IsOn);
                    break;

                case "sprite":
                    if (!OfficeCore.OfficeState.Office.Sprites[obj.ID].AbovePanorama)
                        DrawSprite(obj, objPos);
                    break;

                case "animation":
                    Cache.GetAnimation(obj.Animation).AdvanceDraw(objPos);
                    break;

                case "door" when OfficeCore.OfficeState.Office.Doors.TryGetValue(obj.ID, out var doorAnimVars) &&
                                 doorAnimVars.Animation != null:
                    doorAnimVars.Animation.AdvanceDraw(objPos);
                    break;

                case "text":
                    var uid = obj.Text;
                    if (GameCache.Buttons.TryGetValue(obj.ID, out var btn)) btn.Draw(objPos);
                    else
                    {
                        var tx = GameCache.Texts.TryGetValue(uid, out var value)
                            ? value
                            : GameCache.Texts[uid] = new Text(obj.Text, 36, "Arial", Raylib.WHITE);
                        GameCache.Buttons[obj.ID] = new Button2D(objPos, obj, text: tx);
                        GameCache.Buttons[obj.ID].Draw(objPos);
                    }
                    break;

                default:
                    Logger.LogFatalAsync("MenuHandler", $"Unimplemented Element Type: {obj.Type}");
                    break;
            }
        }

        GameCache.HudCache.JumpscareAnim?.AdvanceDraw(new Vector2(-GameState.ScrollX, 0));
        Raylib.EndTextureMode();

        if (OfficeCore.OfficeState.Settings.Panorama)
            Raylib.BeginShaderMode(GameCache.PanoramaShader);

        var renderTex = GameCache.PanoramaTex.texture;
        Raylib.DrawTexturePro(
            renderTex,
            new Rectangle(0, 0, renderTex.width, -renderTex.height),
            new Rectangle(0, 0, renderTex.width, renderTex.height),
            new Vector2(0, 0), 0f, Raylib.WHITE
        );

        if (OfficeCore.OfficeState.Settings.Panorama)
            Raylib.EndShaderMode();

        GameState.Project.Offices[OfficeCore.Office].Objects
            .Where(obj => obj.Type == "sprite" &&
                  OfficeCore.OfficeState.Office.Sprites[obj.ID].AbovePanorama &&
                  OfficeCore.OfficeState.Office.Objects[obj.ID].Visible)
            .ToList().ForEach(obj =>
                DrawSprite(obj, new Vector2(obj.Position[0] * Globals.XMagic - GameState.ScrollX, obj.Position[1] * Globals.YMagic)));

        OfficeUtils.DrawHud();
    }


    public static bool StartOffice(int night)
    {
        if (string.IsNullOrEmpty(OfficeCore.Office)) return false;
        Logger.LogAsync("OfficeUtils", "Starting Office.");
        OfficeCore.LoadingLock = true;
        RuntimeUtils.Scene.SetScene(SceneType.Office);
        EventManager.KillAllListeners();
        SoundPlayer.KillAll();

        ReloadOfficeData(night);

        foreach (var script in GameState.Project.OfficeScripts)
        {
            Logger.LogAsync("OfficeUtils", $"Starting Office Script: {script.Key}");
            EventManager.RunScript(script.Value);
        }

        if (OfficeCore.OfficeState != null)
            foreach (var animatronic in OfficeCore.OfficeState.Animatronics.Keys)
                PathFinder.StartAnimatronicPath(animatronic);

        GameState.Clock.Restart();
        OfficeCore.LoadingLock = false;
        EventManager.TriggerEvent("on_engine_start", []);
        TimeManager.Start();
        TimeManager.OnTimeUpdate(() =>
        {
            if (TimeManager.GetTime().hours < 6) return;
            TimeManager.Stop();
            EventManager.TriggerEvent("on_night_end", []); // This causes a game expressions error
            MenuUtils.GotoMenu("6AM");
        });
        GameState.Clock.OnTick(() => // TODO: PowerManager?
        {
            if (OfficeCore.OfficeState == null) return;

            if (OfficeCore.OfficeState.Power.Accumulator >= 1)
            {
                if (OfficeCore.OfficeState.Power.Level > 0)
                {
                    OfficeCore.OfficeState.Power.Level -= 1;
                    OfficeCore.OfficeState.Power.Accumulator = 0;
                }
                else
                    OfficeCore.OfficeState.Power.Level = -1;
            }
            else OfficeCore.OfficeState.Power.Accumulator += PowerPerTick(OfficeCore.OfficeState.Power.Usage);

            return;

            static float PowerPerTick(int usage)
            {
                return usage switch
                {
                    0 => 1f / 192f,
                    1 => 1f / 96f,
                    2 => 1f / 64f,
                    3 => 1f / 48f,
                    _ => 1f / 36f,
                };
            }
        });
        GameState.Clock.OnTick(PathFinder.Update);
        EventManager.TriggerEvent("on_night_start", []);
        SoundPlayer.PlayOnChannel(GameState.Project.Sounds.Ambience, true, 1);
        if (GameState.Project.Sounds.Phone_Calls.Count >= night)
            SoundPlayer.PlayOnChannel(GameState.Project.Sounds.Phone_Calls[night - 1], false, 4);
        return true;

    }

    public static void ReloadOfficeData(int night)
    {
        OfficeUtils.ResetHud(); // because position changes on every reload
        if (OfficeCore.Office != null && OfficeCore.OfficeCache.TryGetValue(OfficeCore.Office, out var officeState))
        {
            GameCache.Buttons.Clear();
            Cache.Animations.Clear();
            OfficeCore.OfficeState = officeState;
            GameState.ScrollX = 0;
            return;
        }

        if (OfficeCore.Office != null && GameState.Project.Offices.TryGetValue(OfficeCore.Office, out var staticOffice))
        {
            GameCache.Buttons.Clear();
            Cache.Animations.Clear();
            OfficeCore.OfficeState = new OfficeGame(night)
            {
                Power = new OfficePower
                {
                    Enabled = staticOffice.Power.Enabled,
                    Level = staticOffice.Power.Starting_Level,
                    AnimatronicJumpscare = staticOffice.Power.Animatronic,
                    Usage = 0, // 1 bar
                    Ucn = staticOffice.Power.Ucn,
                    Accumulator = 0
                }
            };
            var office = OfficeCore.OfficeState.Office;
            office.States = staticOffice.States;

            foreach (var obj in staticOffice.Objects.Where(obj => !string.IsNullOrEmpty(obj.ID)))
            {
                office.Objects.TryAdd(obj.ID,
                    new OfficeData.OfficeSprite { Visible = true, AbovePanorama = false, Hovered = false });
                switch (obj.Type)
                {
                    case "sprite":
                        office.Sprites.TryAdd(obj.ID,
                            new OfficeData.OfficeSprite { Visible = true, AbovePanorama = false, Hovered = false });
                        break;
                    case "light_button":
                        office.Lights.TryAdd(obj.ID, new OfficeData.OfficeLight { IsOn = false, Clickable = true });
                        break;
                    case "animation":
                        office.Animations.TryAdd(obj.ID, new OfficeData.OfficeAnimation
                        {
                            Visible = true,
                            AbovePanorama = false,
                            Hovered = false,
                            Id = obj.Animation,
                            IsPlaying = true,
                            Rev = false
                        });
                        break;
                    case "door":
                        var doorAnim = Cache.GetAnimation(obj.Animation, false);
                        doorAnim.Reverse();
                        doorAnim.End();
                        office.Doors.TryAdd(obj.ID, new OfficeData.OfficeDoor
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

            GameState.ScrollX = 0;
            OfficeCore.OfficeCache.TryAdd(OfficeCore.Office, OfficeCore.OfficeState);
        }

        // Animatronics
        if (OfficeCore.OfficeState != null && OfficeCore.OfficeState.Animatronics.Count < 1)
            foreach (var anim in GameState.Project.Animatronics)
                OfficeCore.OfficeState.Animatronics.Add(anim.Key, new OfficeAnimatronic
                {
                    Ai = anim.Value.AI,
                    Phantom = anim.Value.Phantom,
                    Script = anim.Value.Script,
                    Path = anim.Value.Path,
                    Scare = RuntimeUtils.ListToOfficeJumpscare(anim.Value.Jumpscare),
                    IgnoresMask = anim.Value.IgnoreMask,
                    LocationIndex = 0,
                    Location = (anim.Value.Path).FirstOrDefault() ?? new GameJson.PathNode(),
                    Name = anim.Key,
                    State = ""
                });
        // Cameras
        if (OfficeCore.OfficeState != null && OfficeCore.OfficeState.Cameras.Count < 1)
        {
            foreach (var cam in GameState.Project.Cameras.Where(cam => cam.Key != "CamUI" && cam.Key != "UI" && cam.Key != "CameraUI"))
            {
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

        if (OfficeCore.OfficeState == null || OfficeCore.OfficeState.UIButtons.Count >= 1 ||
            OfficeCore.Office == null) return;
        OfficeCore.OfficeState.UIButtons.Add("camera", new GameJson.UIButton
        {
            Input = GameState.Project.Offices[OfficeCore.Office].UIButtons["camera"].Input,
            UI = GameState.Project.Offices[OfficeCore.Office].UIButtons["camera"].UI
        });
        OfficeCore.OfficeState.UIButtons.Add("mask", new GameJson.UIButton
        {
            Input = RuntimeUtils.DeepCopyInput(
                GameState.Project.Offices[OfficeCore.Office].UIButtons["mask"].Input ?? new GameJson.Input()),
            UI = RuntimeUtils.DeepCopyUi(GameState.Project.Offices[OfficeCore.Office].UIButtons["mask"].UI ?? new GameJson.UI())
        });
    }

    public void Exit()
    {
        GameCache.HudCache.JumpscareAnim = null;
        SoundPlayer.KillAll();
        PathFinder.Reset();
        TimeManager.Stop();
        TimeManager.Reset();
        OfficeCore.OfficeCache = [];
    }
}
