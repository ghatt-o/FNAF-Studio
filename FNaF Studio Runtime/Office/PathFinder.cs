using FNaFStudio_Runtime.Data;
using FNaFStudio_Runtime.Data.CRScript;
using FNaFStudio_Runtime.Menus;
using FNaFStudio_Runtime.Office.Scenes;
using FNaFStudio_Runtime.Util;
using System.Threading.Tasks;
using static FNaFStudio_Runtime.Data.Definitions.GameJson;

namespace FNaFStudio_Runtime.Office;

public class PathFinder
{
    private static readonly Random Rng = new();
    private static List<PathTask> ActiveTasks = [];

    public static void TakePath(string anim, int pathIndex, List<PathNode>? path = null)
    {
        if (OfficeCore.OfficeState == null) return;

        var animObj = GameState.Project.Animatronics[anim];
        if (animObj.AI.Count < OfficeCore.OfficeState.Night)
            animObj.CurAI = animObj.AI[OfficeCore.OfficeState.Night];
        path ??= animObj.Path;

        if (path != null)
        {
            if (pathIndex >= path.Count)
            {
                Logger.LogAsync("PathFinder", $"{anim} PATH ENDED");
                return;
            }

            var newPath = path[pathIndex];
            if (newPath == null)
            {
                Logger.LogAsync("PathFinder", "Path ID not found");
                return;
            }

            animObj.PathIndex = pathIndex;
            HandleMovement(animObj, newPath, anim);
            ActiveTasks.Add(new(animObj, newPath, anim));
        }
    }

    public static void Update()
    {
        foreach (var task in ActiveTasks.ToList())
        {
            if (task.AnimObj.Path[task.AnimObj.PathIndex].Type == "office") ; // TODO: stare
            if (task.AnimObj.Paused || !task.AnimObj.Moving)
            {
                if (task.AnimObj.MoveTime > 0)
                    task.AnimObj.MoveTime--;
                else if (task.AnimObj.AI[OfficeCore.OfficeState?.Night ?? 0] >= Rng.Next(1, 20))
                    task.AnimObj.Moving = true;
                else
                    task.AnimObj.MoveTime = Rng.Next(150, 500);
                continue;
            }

            if (GameState.Project.Sounds.AnimatronicMove.Count > 0)
                SoundPlayer.PlayOnChannel(GameState.Project.Sounds.AnimatronicMove[Rng.Next(GameState.Project.Sounds.AnimatronicMove.Count)], false, 11);
            task.AnimObj.Moving = false;
            ActiveTasks.Remove(task);
            TakePath(task.Anim, task.AnimObj.PathIndex + 1); 
        }
    }

    private static void HandleMovement(Animatronic animObj, PathNode newPath, string anim)
    {
        switch (newPath.Type)
        {
            case "camera":
                UpdateCam(anim, animObj, newPath);
                break;
            case "music_box":
                animObj.MoveTime = 1000;
                break;
            case "light":
                UpdateCam(anim, animObj, newPath);
                UpdateOffice(anim);
                break;
            case "door":
                HandleDoorMovement(anim, animObj, newPath);
                break;
            case "office":
                UpdateOffice(anim);
                break;
            case "chance":
                if (Rng.Next(1, newPath.Chance) == 1)
                    TakePath(anim, 0, newPath.Path);
                break;
            case "change_state":
                animObj.State = newPath.State;
                UpdateCam(anim, animObj, newPath);
                break;
        }
    }

    private static void HandleDoorMovement(string anim, Animatronic animObj, PathNode newPath)
    {
        if (OfficeCore.OfficeState != null)
        {
            foreach (var door in OfficeCore.OfficeState.Office.Doors)
            {
                if (door.Key == newPath.ID)
                {
                    if (door.Value.Button.IsOn)
                        TakePath(anim, 0);
                    else if (animObj.IgnoreMask)
                        Jumpscare(animObj);
                    break;
                }
            }
        }
    }

    private static void Jumpscare(Animatronic animObj)
    {
        if (OfficeCore.OfficeState != null)
        {
            GameState.Clock.Stop();
            SoundPlayer.PlayOnChannel(animObj.Jumpscare[0], false, 48);

            var player = OfficeCore.OfficeState.Player;

            if (player.IsMaskOn || player.IsCameraUp)
            {
                var anim = player.IsMaskOn ? GameCache.HudCache.MaskAnim : GameCache.HudCache.CameraAnim;
                anim.Show();
                anim.OnFinish(() =>
                {
                    GameCache.HudCache.JumpscareAnim = Cache.GetAnimation(animObj.Jumpscare[1], false);
                    GameCache.HudCache.JumpscareAnim.OnFinish(() => MenuUtils.GotoMenu("GameOver"));
                });
                return;
            }

            GameCache.HudCache.JumpscareAnim = Cache.GetAnimation(animObj.Jumpscare[1], false);
            GameCache.HudCache.JumpscareAnim.OnFinish(() => MenuUtils.GotoMenu("GameOver"));
        }
    }


    private static void UpdateOffice(string anim = "", bool remove = false, string? special = null)
    {
        if (OfficeCore.OfficeState != null)
        {
            var office = OfficeCore.OfficeState.Office;
            var curState = office.State.Split(':').Last();
            var anims = new List<string>(curState.Split(',').Where(a => a != anim));

            if (!remove && !string.IsNullOrEmpty(anim)) anims.Add(anim);

            curState = office.States.FirstOrDefault(stateEntry =>
                anims.All(anim => stateEntry.Key.Contains(anim)) &&
                stateEntry.Key.Split(',').Length <= anims.Count).Key;
            if (special != null) special += ':';
            office.SetState(string.IsNullOrEmpty(curState) ? $"{special}Default" : curState);
        }
    }

    private static void UpdateCam(string anim, Animatronic animObj, PathNode curPath)
    {
        if (OfficeCore.OfficeState == null ||
            !OfficeCore.OfficeState.Cameras.TryGetValue(
                !string.IsNullOrEmpty(curPath.ID) ? curPath.ID : animObj.curCam ?? "",
                out var curCamera)) return;

        var anims = new List<string>(curCamera.State.Split(',')
            .Where(a => a != "Default" && (a.Contains(':') ? a.Split(':')[0] != anim : a != anim)))
        {
            $"{anim}{(!string.IsNullOrEmpty(animObj.State) ? $":{animObj.State}" : "")}"
        };

        var findAnimState = curCamera.States.FirstOrDefault(stateEntry =>
            anims.All(a => stateEntry.Key.Contains(a)) &&
            stateEntry.Key.Split(',').Length <= anims.Count).Key ?? "Default";

        curCamera.State = (findAnimState.Contains($"{anim}:") && string.IsNullOrEmpty(animObj.State)) ? "Default" : findAnimState;
        if (curPath.Type == "camera")
        {
            animObj.curCam = curPath.ID;
            if (animObj.PathIndex > 0)
            {
                var prevPath = animObj.Path[animObj.PathIndex - 1];
                if (prevPath.Type == "camera" &&
                    OfficeCore.OfficeState.Cameras.TryGetValue(prevPath.ID, out var prevCamera))
                    prevCamera.State = string.Join(",",
                        prevCamera.State.Split(',').Where(s => s != anim).DefaultIfEmpty("Default"));
            }
        }
    }

    public static void StartAnimatronicPath(string animatronic)
    {
        TakePath(animatronic, 0); 
        Console.WriteLine($"Starting path of {animatronic}");
    }

    public static void Reset()
    {
        ActiveTasks = [];
    }

    private class PathTask(Animatronic animObj, PathNode newPath, string anim)
    {
        public Animatronic AnimObj { get; } = animObj;
        public PathNode NewPath { get; } = newPath;
        public string Anim { get; } = anim;
    }
}
