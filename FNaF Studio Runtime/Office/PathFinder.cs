using FNAFStudio_Runtime_RCS;
using FNAFStudio_Runtime_RCS.Data;
using FNAFStudio_Runtime_RCS.Util;
using static FNAFStudio_Runtime_RCS.Data.Definitions.GameJson;

namespace FNAFStudio_Runtime_RCS.Office
{
    public class PathFinder
    {
        public static bool Enabled = true;
        private static readonly Random Rng = new();

        public static async Task TakePathAsync(string anim, int pathIndex, List<PathNode>? path = null)
        {
            var animObj = GameState.Project.Animatronics[anim];
            path ??= animObj.Path;

            if (path != null)
            {
                if (pathIndex >= path.Count)
                {
                    await Logger.LogAsync("PathFinder", $"{anim} PATH ENDED");
                    return;
                }

                var newPath = path[pathIndex];
                if (newPath == null)
                {
                    await Logger.LogErrorAsync("PathFinder", "Path ID not found");
                    return;
                }

                animObj.PathIndex = pathIndex;

                await WaitForMovementAsync(animObj);
                animObj.Moving = true;

                await HandleMovementAsync(animObj, newPath, anim);

                animObj.Moving = false;
                GameState.Project.Animatronics[anim] = animObj;
                await TakePathAsync(anim, pathIndex + 1);
            }
        }

        private static async Task WaitForMovementAsync(Animatronic animObj)
        {
            animObj.MoveTime = Rng.Next(150, 497);
            while (animObj.MoveTime > 0 || animObj.Paused || animObj.Moving)
            {
                await Task.Delay(5);
                animObj.MoveTime--;
            }
            // TODO: Animatronic moving sound
        }

        private static async Task HandleMovementAsync(Animatronic animObj, PathNode newPath, string anim)
        {
            switch (newPath.Type)
            {
                case "camera":
                    animObj.curCam = newPath.ID;
                    UpdateCam(anim, animObj, newPath);
                    break;

                case "music_box":
                    await Task.Delay(1000);
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
                        await TakePathAsync(anim, 0, newPath.Path);
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
                        {
                            animObj.CurPath = null;
                            animObj.PathIndex = 0;
                        }
                        else
                        {
                            // TODO: Jumpscare
                        }
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

                office.SetState(string.IsNullOrEmpty(curState) ? $"{special}:Default" : curState);
            }
        }

        private static void UpdateCam(string anim, Animatronic animObj, PathNode curPath)
        {
            if (animObj.curCam == null || !GameState.Project.Cameras.TryGetValue(animObj.curCam, out Camera? value)) return;

            var curCamera = value;
            var anims = new List<string>(curCamera.CurState.Split(',').Where(a => a != anim))
        {
            anim.Contains(':') ? anim.Split(':')[0] : anim
        };

            var findAnimState = curCamera.States.FirstOrDefault(stateEntry =>
                anims.All(a => stateEntry.Key.Contains(a)) &&
                stateEntry.Key.Split(',').Length <= anims.Count).Key;

            curCamera.CurState = findAnimState ?? "Default";

            if (curPath.ID != null && curPath.Type == "camera" && GameState.Project.Cameras.TryGetValue(curPath.ID, out var prevCamera))
                prevCamera.CurState = string.Join(",", prevCamera.CurState.Split(',').Where(s => s != anim).DefaultIfEmpty("Default"));
        }

        public static void StartAnimatronicPath(string animatronic, Runtime runtime)
        {
            TakePathAsync(animatronic, 0).Wait();
            Console.WriteLine($"Starting path of {animatronic} at {runtime}");
        }
    }
}