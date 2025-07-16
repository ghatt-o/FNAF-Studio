using FNaFStudio_Runtime.Util;
using Raylib_CsLo;

namespace FNaFStudio_Runtime.Data;

public abstract class SoundPlayer
{
    private static readonly Dictionary<string, bool> LoopingSounds = [];
    private static readonly List<string> Channels = [..new string[48]];
    private static readonly float MasterVolume = 70;

    public static void LoadAudioAssets(string assetsPath)
    {
        var soundsDir = Path.Combine(assetsPath, "sounds");
        if (Directory.Exists(soundsDir))
            foreach (var file in Directory.GetFiles(soundsDir))
            {
                var fileName = Path.GetFileName(file);
                Cache.LoadSoundToSounds(fileName, file);
                Logger.LogAsync("SoundPlayer", $"Loaded sound: {fileName}");
            }
        else
            Logger.LogWarnAsync("SoundPlayer", $"Sounds directory not found: {soundsDir}");
    }

    public static void Play(string id, bool loopAudio)
    {
        if (string.IsNullOrEmpty(id)) return;

        var sound = Cache.GetSound(id);
        {
            var channel = GetAvailableChannel();
            if (channel != -1)
            {
                Channels[channel] = id;
                Raylib.PlaySound(sound);
                Raylib.SetSoundVolume(sound, (100 / MasterVolume));
                if (loopAudio) SetSoundLooping(id, true);
            }
            else
            {
                Logger.LogWarnAsync("SoundPlayer", $"No available channel for sound: {id}");
            }
        }
    }

    public static void PlayOnChannel(string id, bool loopAudio, int channelIdx)
    {
        if (string.IsNullOrEmpty(id)) return;

        var sound = Cache.GetSound(id);
        channelIdx--;
        if (channelIdx < Channels.Count && channelIdx >= 0)
        {
            Raylib.StopSound(sound);
            Channels[channelIdx] = id;
            Raylib.PlaySound(sound);
            Raylib.SetSoundVolume(sound, (100 / MasterVolume));
            if (loopAudio) SetSoundLooping(id, true);
        }
        else
        {
            Logger.LogWarnAsync("SoundPlayer", $"Invalid channel index: {channelIdx}");
        }
    }

    private static void Stop(string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        var sound = Cache.GetSound(id);
        LoopingSounds[id] = false;
        Raylib.StopSound(sound);
    }

    public static void StopChannel(int channelIdx)
    {
        channelIdx--;
        if (channelIdx < Channels.Count && channelIdx >= 0)
        {
            if (string.IsNullOrEmpty(Channels[channelIdx])) return;
            Stop(Channels[channelIdx]);
            Channels[channelIdx] = string.Empty;
        }
        else
        {
            Logger.LogWarnAsync("SoundPlayer", $"Invalid channel index: {channelIdx}");
        }
    }

    public static void KillAll()
    {
        for (var i = 0; i < Channels.Count; i++)
            if (!string.IsNullOrEmpty(Channels[i]))
            {
                Stop(Channels[i]);
                Channels[i] = string.Empty;
            }

        LoopingSounds.Clear();
        Logger.LogAsync("SoundPlayer", "Stopped all sounds");
    }

    public static void SetChannelVolume(int channelIdx, float volume)
    {
        channelIdx--;
        if (channelIdx < Channels.Count && channelIdx >= 0)
        {
            if (!string.IsNullOrEmpty(Channels[channelIdx]))
            {
                Raylib.SetSoundVolume(Cache.GetSound(Channels[channelIdx]), (volume / MasterVolume));
            }
        }
        else
            Logger.LogWarnAsync("SoundPlayer", $"Invalid channel index: {channelIdx}");
    }

    public static void SetAllVolumes(float volume)
    {
        foreach (var id in Channels.Where(id => !string.IsNullOrEmpty(id)))
            Raylib.SetSoundVolume(Cache.GetSound(id), (volume / MasterVolume));
    }

    private static int GetAvailableChannel()
    {
        for (var i = 0; i < Channels.Count; i++)
            if (string.IsNullOrEmpty(Channels[i]))
                return i;
        return -1;
    }

    private static void SetSoundLooping(string id, bool loop)
    {
        if (Cache.Sounds.ContainsKey(id)) LoopingSounds[id] = loop;
    }

    public static void Update()
    {
        foreach (var kvp in LoopingSounds.Where(kvp => kvp.Value && !Raylib.IsSoundPlaying(Cache.Sounds[kvp.Key])))
            Raylib.PlaySound(Cache.Sounds[kvp.Key]);
    }
}