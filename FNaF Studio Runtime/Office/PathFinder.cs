using FNaFStudio_Runtime.Data;
using FNaFStudio_Runtime.Util;
using System.Threading.Tasks;
using static FNaFStudio_Runtime.Data.Definitions.GameJson;

namespace FNaFStudio_Runtime.Office;

public class PathFinder
{
    public static bool Enabled = true;
    private static readonly Random Rng = new();
    private static readonly List<PathTask> ActiveTasks = [];

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

            if (pathIndex == 0)
            {
                HandleMovement(animObj, newPath, anim);
                TakePath(anim, pathIndex + 1);
            }
            else
                ActiveTasks.Add(new(animObj, newPath, anim));
        }
    }

    public static void Update()
    {
        foreach (var task in ActiveTasks.ToList())
        {
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

            // TODO: Animatronic moving sound
            HandleMovement(task.AnimObj, task.NewPath, task.Anim);
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
                HandleDoorMovement(animObj, newPath);
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

    private static void HandleDoorMovement(Animatronic animObj, PathNode newPath)
    {
        if (OfficeCore.OfficeState != null)
        {
            foreach (var door in OfficeCore.OfficeState.Office.Doors)
            {
                if (door.Key == newPath.ID)
                {
                    if (door.Value.IsClosed)
                        animObj.PathIndex = 0;

                    // TODO: Jumpscare

                    break;
                }
            }
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
        if (OfficeCore.OfficeState == null) return;
        if (curPath.ID == null || !OfficeCore.OfficeState.Cameras.TryGetValue(curPath.ID, out var curCamera)) return;

        var anims = new List<string>(curCamera.State.Split(',').Where(a => a != anim))
        {
            anim.Contains(':') ? anim.Split(':')[0] : anim
        };
        if (anims.Count > 1 && anims.Contains("Default")) anims.Remove("Default");

        var findAnimState = curCamera.States.FirstOrDefault(stateEntry =>
            anims.All(a => stateEntry.Key.Contains(a)) &&
            stateEntry.Key.Split(',').Length <= anims.Count).Key;

        curCamera.State = findAnimState ?? "Default";

        if (animObj.curCam != null && curPath.Type == "camera" &&
            OfficeCore.OfficeState.Cameras.TryGetValue(animObj.curCam, out var prevCamera))
            prevCamera.State = string.Join(",",
                prevCamera.State.Split(',').Where(s => s != anim).DefaultIfEmpty("Default"));
    }

    public static void StartAnimatronicPath(string animatronic)
    {
        TakePath(animatronic, 0); 
        Console.WriteLine($"Starting path of {animatronic}");
    }

    private class PathTask(Animatronic animObj, PathNode newPath, string anim)
    {
        public Animatronic AnimObj { get; } = animObj;
        public PathNode NewPath { get; } = newPath;
        public string Anim { get; } = anim;
    }
}
