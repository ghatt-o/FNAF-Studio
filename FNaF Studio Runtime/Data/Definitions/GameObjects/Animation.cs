using System.Numerics;
using FNaFStudio_Runtime.Util;
using Newtonsoft.Json;
using Raylib_CsLo;

namespace FNaFStudio_Runtime.Data.Definitions.GameObjects
{
    public class BaseAnimation
    {
        private readonly List<string> frames;
        private readonly List<int> frameSpeeds;
        private int currentFrame;
        private int frameCount;
        private readonly bool looping;
        private float timer;
        private bool isPaused;
        private bool isHidden;
        private readonly List<Action>? onFinishActions;
        private readonly List<Action>? onPlayActions;
        private bool playTriggered;

        public BaseAnimation(string path, bool reversed = false, bool loop = true)
        {
            frames = [];
            frameSpeeds = [];
            LoadFrames(path);
            if (reversed)
            {
                frames.Reverse();
                frameSpeeds.Reverse();
            }

            currentFrame = 0;
            looping = loop;
            onFinishActions = [];
            onPlayActions = [];
        }

        private void LoadFrames(string file)
        {
            if (!File.Exists(file)) throw new FileNotFoundException("The specified JSON file was not found.", file);
            var jsonFrames = JsonConvert.DeserializeObject<AJson.Frame[]>(File.ReadAllText(file));
            if (jsonFrames == null)
            {
                Logger.LogFatalAsync("BaseAnimation", "JsonFrames is null.");
                return;
            }

            foreach (var frame in jsonFrames)
            {
                frames.Add(frame.Sprite);
                frameSpeeds.Add(frame.Duration);
            }

            frameCount = frames.Count;
        }

        public bool HasFramesLeft()
        {
            return currentFrame + 1 < frameCount;
        }

        public void Update()
        {
            // WARNING: PLEASE DO NOT TRY TO OPTIMIZE THIS CODE,
            // IT IS VERY SENSITIVE TO CHANGES AND VERY FRAGILE.
            if (isPaused || isHidden) return;

            if (!playTriggered && onPlayActions?.Count > 0)
            {
                onPlayActions.ForEach(action => action());
                playTriggered = true;
            }

            timer += Raylib.GetFrameTime();
            var frameLength = frameSpeeds[currentFrame] / 30f;

            if (HasFramesLeft() || looping)
            {
                while (timer >= frameLength)
                {
                    currentFrame = (currentFrame + 1) % frameCount;
                    timer -= frameLength;
                    frameLength = frameSpeeds[currentFrame];
                }
            }
            else if (timer > frameLength)
            {
                timer = frameLength;

                if (!looping && !HasFramesLeft() && onFinishActions?.Count > 0)
                {
                    onFinishActions.ForEach(action => action());
                }
            }
        }

        public void Draw(Vector2 position)
        {
            if (frameCount > 0 && !isHidden)
                Raylib.DrawTexture(Cache.GetTexture(frames[currentFrame]), (int)position.X, (int)position.Y, Raylib.WHITE);
        }

        public void Reset()
        {
            currentFrame = 0;
            timer = 0;
            playTriggered = false;
        }

        public void End()
        {
            if (frameCount != 0)
                currentFrame = frameCount - 1;
        }

        public void OnFinish(Action onFinishAction)
        {
            onFinishActions?.Add(onFinishAction);
        }

        public void OnPlay(Action onPlayAction)
        {
            onPlayActions?.Add(onPlayAction);
        }

        public void Pause()
        {
            isPaused = true;
        }

        public void Resume()
        {
            if (isPaused)
            {
                isPaused = false;
                playTriggered = false;
            }
        }

        public void Hide()
        {
            isHidden = true;
        }

        public void Show()
        {
            if (isHidden)
            {
                isHidden = false;
                playTriggered = false;
            }
        }
    }
}
