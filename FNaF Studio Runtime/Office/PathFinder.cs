using FNaFStudio_Runtime.Data;
using FNaFStudio_Runtime.Menus;
using FNaFStudio_Runtime.Util;
using System.Collections.Concurrent;
using static FNaFStudio_Runtime.Data.Definitions.GameJson;

namespace FNaFStudio_Runtime.Office
{
    public class PathFinder
    {
        private static readonly Random Rng = new();
        private static List<PathTask> ActiveTasks = new();
        private static readonly Dictionary<string, List<PathNode>> CurrentSubPaths = new();
        private static readonly Dictionary<string, List<PathNode>> FullPaths = new();
        private static readonly Dictionary<string, Stack<PathContext>> PathStacks = new();
        private static ConcurrentQueue<(string Anim, string LightId)> PendingAnimQueue = new();
        private record PathContext(List<PathNode> Path, List<PathNode> FullPath, int Index);

        public static void TakePath(string anim, int pathIndex, List<PathNode>? path = null, List<PathNode>? fullPath = null)
        {
            if (OfficeCore.OfficeState == null) return;

            var animObj = GameState.Project.Animatronics[anim];
            if (animObj.AI.Count < OfficeCore.OfficeState.Night)
                animObj.CurAI = animObj.AI[OfficeCore.OfficeState.Night];

            path ??= animObj.Path;
            fullPath ??= path;

            if (!PathStacks.ContainsKey(anim))
                PathStacks[anim] = new Stack<PathContext>();

            if (pathIndex >= path.Count)
            {
                if (PathStacks[anim].Count > 0)
                {
                    var parent = PathStacks[anim].Pop();
                    Logger.LogAsync("PathFinder", $"{anim} returning to parent path at index {parent.Index + 1}");
                    TakePath(anim, parent.Index + 1, parent.Path, parent.FullPath);
                }
                else
                {
                    Logger.LogAsync("PathFinder", $"{anim} path reset");
                    TakePath(anim, 0, animObj.Path, animObj.Path);
                }
                return;
            }

            var newPath = path[pathIndex];
            if (newPath == null)
            {
                Logger.LogAsync("PathFinder", "Path ID not found");
                return;
            }

            animObj.PathIndex = pathIndex;
            CurrentSubPaths[anim] = path;
            FullPaths[anim] = fullPath;

            Logger.LogAsync("PathFinder", $"{anim} moving to {newPath.Type} ({newPath.ID}) at index {pathIndex}");
            HandleMovement(animObj, newPath, anim);
            ActiveTasks.Add(new(animObj, newPath, anim));
        }

        public static void Update()
        {
            foreach (var task in ActiveTasks.ToList())
            {
                var path = CurrentSubPaths[task.Anim];
                if (task.AnimObj.Paused || !task.AnimObj.Moving)
                {
                    if (task.AnimObj.MoveTime > 0)
                        task.AnimObj.MoveTime--;
                    else if (task.AnimObj.AI[OfficeCore.OfficeState?.Night - 1 ?? 0] >= Rng.Next(1, 20))
                        task.AnimObj.Moving = true;
                    else
                        task.AnimObj.MoveTime = Rng.Next(150, 500);
                    continue;
                }

                if (GameState.Project.Sounds.AnimatronicMove.Count > 0)
                    SoundPlayer.PlayOnChannel(GameState.Project.Sounds.AnimatronicMove[Rng.Next(GameState.Project.Sounds.AnimatronicMove.Count)], false, 11);
                task.AnimObj.Moving = false;
                ActiveTasks.Remove(task);
                TakePath(task.Anim, task.AnimObj.PathIndex + 1, CurrentSubPaths[task.Anim], FullPaths[task.Anim]);
            }
        }

        private static void HandleMovement(Animatronic animObj, PathNode newPath, string anim)
        {
            if (animObj.PathIndex > 0 && CurrentSubPaths.ContainsKey(anim))
            {
                var path = CurrentSubPaths[anim];
                var prevPath = path[animObj.PathIndex - 1];
                if (prevPath.Type == "light" && newPath.Type != "light")
                {
                    Logger.LogAsync("PathFinder", $"{anim} moved out of light node");
                    var tempQueue = new ConcurrentQueue<(string Anim, string LightId)>();
                    while (PendingAnimQueue.TryDequeue(out var queued))
                    {
                        if (queued.Anim != anim)
                            tempQueue.Enqueue(queued);
                    }
                    PendingAnimQueue = tempQueue;
                }
            }

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
                    bool success = Rng.Next(1, newPath.Chance + 1) == 1;
                    var branch = success ? newPath.Path : newPath.AltPath;
                    if (branch?.Count > 0)
                    {
                        PathStacks[anim].Push(new(CurrentSubPaths[anim], FullPaths[anim], animObj.PathIndex));
                        TakePath(anim, 0, branch, FullPaths[anim]);
                    }
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
                            TakePath(anim, 0, FullPaths[anim], FullPaths[anim]);
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
                var splits = office.State.Split(':');
                var curState = splits.Last();
                special = splits.Length != 1 ? splits.First() : null;
                if (special != null) special += ':';
                var anims = new List<string>(curState.Split(',').Where(a => a != anim && a != "Default"));

                if (!remove && !string.IsNullOrEmpty(anim))
                {
                    var lightId = CurrentSubPaths[anim][GameState.Project.Animatronics[anim].PathIndex].ID;
                    if (splits.Length == 1 || !splits[0].Contains(lightId))
                    {
                        if (!PendingAnimQueue.Any(q => q.Anim == anim))
                        {
                            PendingAnimQueue.Enqueue((anim, lightId));
                            Logger.LogAsync("PathFinder", $"Light off - queued anim {anim} for light {lightId} until light turns on");
                        }
                        return;
                    }
                    else
                    {
                        anims.Add($"{special}{anim}");
                    }
                }

                var newCurState = office.States.FirstOrDefault(stateEntry =>
                    anims.All(a => stateEntry.Key.Contains(a)) &&
                    stateEntry.Key.Split(',').Length <= anims.Count).Key;

                office.SetState(string.IsNullOrEmpty(newCurState) ? "Default" : newCurState);
            }
        }

        public static void OnLightTurnedOn(string lightId)
        {
            if (OfficeCore.OfficeState == null) return;

            var tempQueue = new ConcurrentQueue<(string Anim, string LightId)>();
            while (PendingAnimQueue.TryDequeue(out var queued))
            {
                if (queued.LightId == lightId)
                {
                    Logger.LogAsync("PathFinder", $"Light {lightId} turned on - adding queued anim {queued.Anim}");
                    UpdateOffice(queued.Anim, remove: false);
                }
                else
                {
                    tempQueue.Enqueue(queued);
                }
            }
            PendingAnimQueue = tempQueue;
        }

        public static void OnLightTurnedOff(string anim)
        {
            if (OfficeCore.OfficeState == null || anim == "Default") return;

            UpdateOffice(anim);
        }

        private static void UpdateCam(string anim, Animatronic animObj, PathNode curPath)
        {
            if (OfficeCore.OfficeState == null) return;

            string currentCameraID = !string.IsNullOrEmpty(curPath.ID) ? curPath.ID : animObj.curCam ?? "";

            if (OfficeCore.OfficeState.Cameras.TryGetValue(currentCameraID, out var curCamera))
            {
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
                    if (animObj.PathIndex > 0) curCamera.SetInterrupted(true);
                }
            }

            if (animObj.PathIndex > 0)
            {
                var path = CurrentSubPaths[anim];
                for (int i = animObj.PathIndex - 1; i >= 0; i--)
                {
                    var prevPath = path[i];
                    if (prevPath.Type == "camera" && OfficeCore.OfficeState.Cameras.TryGetValue(prevPath.ID, out var prevCamera))
                    {
                        prevCamera.State = string.Join(",", prevCamera.State.Split(',').Where(s => s != anim).DefaultIfEmpty("Default"));
                        prevCamera.SetInterrupted(true);
                        break;
                    }
                }
            }
        }

        public static void MoveAnimToNode(string anim, string nodeId)
        {
            if (OfficeCore.OfficeState == null || !GameState.Project.Animatronics.ContainsKey(anim)) return;

            var animObj = GameState.Project.Animatronics[anim];
            if (!CurrentSubPaths.ContainsKey(anim) || !FullPaths.ContainsKey(anim)) return;

            var currentPath = CurrentSubPaths[anim];
            var fullPath = FullPaths[anim];
            int currentIndex = animObj.PathIndex;

            int closestIndex = -1;
            List<PathNode> targetPath = null;
            int minDistance = int.MaxValue;

            for (int i = 0; i < currentPath.Count; i++)
            {
                if (currentPath[i].ID == nodeId)
                {
                    int distance = Math.Abs(i - currentIndex);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestIndex = i;
                        targetPath = currentPath;
                    }
                }
            }

            for (int i = 0; i < fullPath.Count; i++)
            {
                if (fullPath[i].ID == nodeId)
                {
                    int distance = Math.Abs(i - currentIndex);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestIndex = i;
                        targetPath = fullPath;
                    }
                }
            }

            if (closestIndex == -1 || targetPath == null)
            {
                Logger.LogAsync("PathFinder", $"Node ID {nodeId} not found in {anim}'s path");
                return;
            }

            if (PathStacks.ContainsKey(anim))
            {
                PathStacks[anim].Clear();
            }

            Logger.LogAsync("PathFinder", $"{anim} moving to node {nodeId} at index {closestIndex} in {(targetPath == currentPath ? "current sub-path" : "full path")}");

            animObj.PathIndex = closestIndex;
            CurrentSubPaths[anim] = targetPath;
            HandleMovement(animObj, targetPath[closestIndex], anim);

            ActiveTasks.Add(new PathTask(animObj, targetPath[closestIndex], anim));
        }

        public static void StartAnimatronicPath(string animatronic)
        {
            TakePath(animatronic, 0);
            Logger.LogAsync("PathFinder", $"Starting path of {animatronic}");
        }

        public static void Reset()
        {
            ActiveTasks = new();
            CurrentSubPaths.Clear();
            FullPaths.Clear();
            PathStacks.Clear();
            while (PendingAnimQueue.TryDequeue(out _)) { }
        }

        private class PathTask
        {
            public Animatronic AnimObj { get; }
            public PathNode NewPath { get; }
            public string Anim { get; }

            public PathTask(Animatronic animObj, PathNode newPath, string anim)
            {
                AnimObj = animObj;
                NewPath = newPath;
                Anim = anim;
            }
        }
    }
}