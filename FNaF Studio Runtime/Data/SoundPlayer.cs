using FNaFStudio_Runtime.Util;
using Raylib_CsLo;

namespace FNaFStudio_Runtime.Data;

public class SoundPlayer
{
    private static readonly Dictionary<string, bool> loopingSounds = [];
    private static readonly List<string> channels = new(new string[48]);
    public static float MasterVolume = 70;

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
                channels[channel] = id;
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
        if (channelIdx < channels.Count && channelIdx >= 0)
        {
            Raylib.StopSound(sound);
            channels[channelIdx] = id;
            Raylib.PlaySound(sound);
            Raylib.SetSoundVolume(sound, (100 / MasterVolume));
            if (loopAudio) SetSoundLooping(id, true);
        }
        else
        {
            Logger.LogWarnAsync("SoundPlayer", $"Invalid channel index: {channelIdx}");
        }
    }

    public static void Stop(string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        var sound = Cache.GetSound(id);
        loopingSounds[id] = false;
        Raylib.StopSound(sound);

        return;
    }

    public static void StopChannel(int channelIdx)
    {
        channelIdx--;
        if (channelIdx < channels.Count && channelIdx >= 0)
        {
            if (!string.IsNullOrEmpty(channels[channelIdx]))
            {
                Stop(channels[channelIdx]);
                channels[channelIdx] = string.Empty;
            }
        }
        else
        {
            Logger.LogWarnAsync("SoundPlayer", $"Invalid channel index: {channelIdx}");
        }
    }

    public static void KillAll()
    {
        // TODO: Add non-kill sounds to specific scenes (for example, continuous
        // menu background music)

        for (var i = 0; i < channels.Count; i++)
            if (!string.IsNullOrEmpty(channels[i]))
            {
                Stop(channels[i]);
                channels[i] = string.Empty;
            }

        loopingSounds.Clear();
        Logger.LogAsync("SoundPlayer", "Stopped all sounds");
    }

    public static void SetChannelVolume(int channelIdx, float volume)
    {
        channelIdx--;
        if (channelIdx < channels.Count && channelIdx >= 0)
        {
            if (!string.IsNullOrEmpty(channels[channelIdx]))
            {
                Raylib.SetSoundVolume(Cache.GetSound(channels[channelIdx]), (volume / MasterVolume));
            }
        }
        else
            Logger.LogWarnAsync("SoundPlayer", $"Invalid channel index: {channelIdx}");
    }

    public static void SetAllVolumes(float volume)
    {
        foreach (var id in channels)
            if (!string.IsNullOrEmpty(id))
                Raylib.SetSoundVolume(Cache.GetSound(id), (volume / MasterVolume));

        return;
    }

    private static int GetAvailableChannel()
    {
        for (var i = 0; i < channels.Count; i++)
            if (string.IsNullOrEmpty(channels[i]))
                return i;
        return -1;
    }

    private static void SetSoundLooping(string id, bool loop)
    {
        if (Cache.Sounds.ContainsKey(id)) loopingSounds[id] = loop;
    }

    public static void Update()
    {
        foreach (var kvp in loopingSounds)
            if (kvp.Value && !Raylib.IsSoundPlaying(Cache.Sounds[kvp.Key]))
                Raylib.PlaySound(Cache.Sounds[kvp.Key]);
        return;
    }
}