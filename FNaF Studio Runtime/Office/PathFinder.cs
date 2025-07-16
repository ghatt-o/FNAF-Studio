using FNaFStudio_Runtime.Data;
using FNaFStudio_Runtime.Menus;
using FNaFStudio_Runtime.Util;
using System.Collections.Concurrent;
using static FNaFStudio_Runtime.Data.Definitions.GameJson;

namespace FNaFStudio_Runtime.Office
{
    public abstract class PathFinder
    {
        private static readonly Random Rng = new();
        private static List<PathTask> _activeTasks = [];
        private static readonly Dictionary<string, List<PathNode>?> CurrentSubPaths = new();
        private static readonly Dictionary<string, List<PathNode>?> FullPaths = new();
        private static readonly Dictionary<string, Stack<PathContext>> PathStacks = new();
        private static ConcurrentQueue<(string Anim, string LightId)> _pendingAnimQueue = new();
        private record PathContext(List<PathNode>? Path, List<PathNode>? FullPath, int Index);

        private static void TakePath(string anim, int pathIndex, List<PathNode>? path = null, List<PathNode>? fullPath = null)
        {
            while (true)
            {
                if (OfficeCore.OfficeState == null) return;

                var animObj = GameState.Project.Animatronics[anim];
                if (animObj.AI.Count < OfficeCore.OfficeState.Night) animObj.CurAI = animObj.AI[OfficeCore.OfficeState.Night];

                path ??= animObj.Path;
                fullPath ??= path;

                if (!PathStacks.ContainsKey(anim)) PathStacks[anim] = new Stack<PathContext>();

                if (pathIndex >= path.Count)
                {
                    if (PathStacks[anim].Count > 0)
                    {
                        var parent = PathStacks[anim].Pop();
                        Logger.LogAsync("PathFinder", $"{anim} returning to parent path at index {parent.Index + 1}");
                        pathIndex = parent.Index + 1;
                        path = parent.Path;
                        fullPath = parent.FullPath;
                    }
                    else
                    {
                        Logger.LogAsync("PathFinder", $"{anim} path reset");
                        pathIndex = 0;
                        path = animObj.Path;
                        fullPath = animObj.Path;
                    }

                    continue;
                }

                var newPath = path[pathIndex];

                animObj.PathIndex = pathIndex;
                CurrentSubPaths[anim] = path;
                FullPaths[anim] = fullPath;

                Logger.LogAsync("PathFinder", $"{anim} moving to {newPath.Type} ({newPath.ID}) at index {pathIndex}");
                HandleMovement(animObj, newPath, anim);
                _activeTasks.Add(new PathTask(animObj, anim));
                break;
            }
        }

        public static void Update()
        {
            foreach (var task in _activeTasks.ToList())
            {
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
                _activeTasks.Remove(task);
                TakePath(task.Anim, task.AnimObj.PathIndex + 1, CurrentSubPaths[task.Anim], FullPaths[task.Anim]);
            }
        }

        private static void HandleMovement(Animatronic animObj, PathNode newPath, string anim)
        {
            if (animObj.PathIndex > 0 && CurrentSubPaths.TryGetValue(anim, out var path))
            {
                var prevPath = path?[animObj.PathIndex - 1];
                if (prevPath?.Type == "light" && newPath.Type != "light")
                {
                    Logger.LogAsync("PathFinder", $"{anim} moved out of light node");
                    var tempQueue = new ConcurrentQueue<(string Anim, string LightId)>();
                    while (_pendingAnimQueue.TryDequeue(out var queued))
                    {
                        if (queued.Anim != anim)
                            tempQueue.Enqueue(queued);
                    }
                    _pendingAnimQueue = tempQueue;
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
                    if (branch.Count > 0)
                    {
                        PathStacks[anim].Push(new PathContext(CurrentSubPaths[anim], FullPaths[anim], animObj.PathIndex));
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
            if (OfficeCore.OfficeState == null) return;
            foreach (var door in OfficeCore.OfficeState.Office.Doors.Where(door => door.Key == newPath.ID))
            {
                if (door.Value.Button.IsOn)
                    TakePath(anim, 0, FullPaths[anim], FullPaths[anim]);
                else if (animObj.IgnoreMask)
                    Jumpscare(animObj);
                break;
            }
        }

        private static void Jumpscare(Animatronic animObj)
        {
            if (OfficeCore.OfficeState == null) return;
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

        private static void UpdateOffice(string anim = "", bool remove = false)
        {
            if (OfficeCore.OfficeState == null) return;
            var office = OfficeCore.OfficeState.Office;
            var splits = office.State.Split(':');
            var curState = splits.Last();
            var special = splits.Length != 1 ? splits.First() : null;
            if (special != null) special += ':';
            var anims = new List<string>(curState.Split(',').Where(a => a != anim && a != "Default"));

            if (!remove && !string.IsNullOrEmpty(anim))
            {
                var lightId = CurrentSubPaths[anim]?[GameState.Project.Animatronics[anim].PathIndex].ID;
                if (lightId != null && (splits.Length == 1 || !splits[0].Contains(lightId)))
                {
                    if (_pendingAnimQueue.Any(q => q.Anim == anim)) return;
                    _pendingAnimQueue.Enqueue((anim, lightId));
                    Logger.LogAsync("PathFinder", $"Light off - queued anim {anim} for light {lightId} until light turns on");
                    return;
                }

                anims.Add($"{special}{anim}");
            }

            var newCurState = office.States.FirstOrDefault(stateEntry =>
                anims.All(a => stateEntry.Key.Contains(a)) &&
                stateEntry.Key.Split(',').Length <= anims.Count).Key;

            office.SetState(string.IsNullOrEmpty(newCurState) ? "Default" : newCurState);
        }

        public static void OnLightTurnedOn(string lightId)
        {
            if (OfficeCore.OfficeState == null) return;

            var tempQueue = new ConcurrentQueue<(string Anim, string LightId)>();
            while (_pendingAnimQueue.TryDequeue(out var queued))
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
            _pendingAnimQueue = tempQueue;
        }

        public static void OnLightTurnedOff(string anim)
        {
            if (OfficeCore.OfficeState == null || anim == "Default") return;

            UpdateOffice(anim);
        }

        private static void UpdateCam(string anim, Animatronic animObj, PathNode curPath)
        {
            if (OfficeCore.OfficeState == null) return;

            var currentCameraId = !string.IsNullOrEmpty(curPath.ID) ? curPath.ID : animObj.curCam ?? "";

            if (OfficeCore.OfficeState.Cameras.TryGetValue(currentCameraId, out var curCamera))
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

            if (animObj.PathIndex <= 0) return;
            var path = CurrentSubPaths[anim];
            for (int i = animObj.PathIndex - 1; i >= 0; i--)
            {
                var prevPath = path?[i];
                if (prevPath?.Type != "camera" ||
                    !OfficeCore.OfficeState.Cameras.TryGetValue(prevPath.ID, out var prevCamera)) continue;
                prevCamera.State = string.Join(",", prevCamera.State.Split(',').Where(s => s != anim).DefaultIfEmpty("Default"));
                prevCamera.SetInterrupted(true);
                break;
            }
        }

        public static void MoveAnimToNode(string anim, string nodeId)
        {
            if (OfficeCore.OfficeState == null || !GameState.Project.Animatronics.TryGetValue(anim, out var animObj)) return;

            if (!CurrentSubPaths.TryGetValue(anim, out var currentPath) || !FullPaths.TryGetValue(anim, out var fullPath)) return;
            int currentIndex = animObj.PathIndex;

            int closestIndex = -1;
            List<PathNode>? targetPath = null;
            int minDistance = int.MaxValue;

            if (currentPath != null)
                for (int i = 0; i < currentPath.Count; i++)
                {
                    if (currentPath[i].ID != nodeId) continue;
                    int distance = Math.Abs(i - currentIndex);
                    if (distance >= minDistance) continue;
                    minDistance = distance;
                    closestIndex = i;
                    targetPath = currentPath;
                }

            if (fullPath != null)
                for (int i = 0; i < fullPath.Count; i++)
                {
                    if (fullPath[i].ID != nodeId) continue;
                    int distance = Math.Abs(i - currentIndex);
                    if (distance >= minDistance) continue;
                    minDistance = distance;
                    closestIndex = i;
                    targetPath = fullPath;
                }

            if (closestIndex == -1 || targetPath == null)
            {
                Logger.LogAsync("PathFinder", $"Node ID {nodeId} not found in {anim}'s path");
                return;
            }

            if (PathStacks.TryGetValue(anim, out var stack))
            {
                stack.Clear();
            }

            Logger.LogAsync("PathFinder", $"{anim} moving to node {nodeId} at index {closestIndex} in {(targetPath == currentPath ? "current sub-path" : "full path")}");

            animObj.PathIndex = closestIndex;
            CurrentSubPaths[anim] = targetPath;
            HandleMovement(animObj, targetPath[closestIndex], anim);

            _activeTasks.Add(new PathTask(animObj, anim));
        }

        public static void StartAnimatronicPath(string animatronic)
        {
            TakePath(animatronic, 0);
            Logger.LogAsync("PathFinder", $"Starting path of {animatronic}");
        }

        public static void Reset()
        {
            _activeTasks = [];
            CurrentSubPaths.Clear();
            FullPaths.Clear();
            PathStacks.Clear();
            while (_pendingAnimQueue.TryDequeue(out _)) { }
        }

        private class PathTask(Animatronic animObj, string anim)
        {
            public Animatronic AnimObj { get; } = animObj;
            public string Anim { get; } = anim;
        }
    }
}