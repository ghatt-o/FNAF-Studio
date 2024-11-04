using System.Numerics;
using FNAFStudio_Runtime_RCS.Util;
using Newtonsoft.Json;
using Raylib_CsLo;

namespace FNAFStudio_Runtime_RCS.Data.Definitions.GameObjects;

public class BaseAnimation
{
    private readonly List<string> frames;
    private readonly List<int> frameSpeeds;
    private int currentFrame;
    private int frameCount;
    public bool looping = true;
    private float timer;

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
    }

    private void LoadFrames(string file)
    {
        if (!File.Exists(file)) throw new FileNotFoundException("The specified JSON file was not found.", file);
        var JsonFrames = JsonConvert.DeserializeObject<AJson.Frame[]>(File.ReadAllText(file));
        if (JsonFrames == null)
        {
            Logger.LogFatalAsync("BaseAnimation", "JsonFrames is null.");
            return;
        }

        foreach (var frame in JsonFrames)
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
        timer += Raylib.GetFrameTime();

        var frameLength = frameSpeeds[currentFrame] / 30f; // FPS Number

        if (HasFramesLeft() || looping)
            while (timer >= frameLength)
            {
                currentFrame = (currentFrame + 1) % frameCount;
                timer -= frameLength;
                frameLength = frameSpeeds[currentFrame];
            }
        else if (timer > frameLength) timer = frameLength;
    }

    public void Draw(Vector2 position)
    {
        if (frameCount > 0)
            Raylib.DrawTexture(Cache.GetTexture(frames[currentFrame]), (int)position.X, (int)position.Y, Raylib.WHITE);
    }

    public void Reset()
    {
        currentFrame = 0;
    }

    public void End()
    {
        if (frameCount != 0)
            currentFrame = frameCount - 1;
    }
}