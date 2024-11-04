using FNAFStudio_Runtime_RCS.Util;
using Raylib_CsLo;

namespace FNAFStudio_Runtime_RCS.Data;

public class SoundPlayer
{
    private static readonly Dictionary<string, bool> loopingSounds = [];
    private static readonly List<string> channels = new(new string[48]);

    public static async Task LoadAudioAssetsAsync(string assetsPath)
    {
        var soundsDir = Path.Combine(assetsPath, "sounds");
        if (Directory.Exists(soundsDir))
            foreach (var file in Directory.GetFiles(soundsDir))
            {
                var fileName = Path.GetFileName(file);
                Cache.LoadSoundToSounds(fileName, file);
                await Logger.LogAsync("SoundPlayer", $"Loaded sound: {fileName}");
            }
        else
            await Logger.LogWarnAsync("SoundPlayer", $"Sounds directory not found: {soundsDir}");
    }

    public static async Task PlayAsync(string id, bool loopAudio)
    {
        if (string.IsNullOrEmpty(id)) return;

        var sound = Cache.GetSound(id);
        //if (Raylib.IsSoundReady(sound))
        {
            var channel = GetAvailableChannel();
            if (channel != -1)
            {
                channels[channel] = id;
                Raylib.PlaySound(sound);
                if (loopAudio) SetSoundLooping(id, true);
            }
            else
            {
                await Logger.LogWarnAsync("SoundPlayer", $"No available channel for sound: {id}");
            }
        }
        /*else
        {
            await Logger.LogWarnAsync("SoundPlayer", $"Sound not ready: {id}");
        }*/
    }

    public static async Task PlayOnChannelAsync(string id, bool loopAudio, int channelIdx)
    {
        if (string.IsNullOrEmpty(id)) return;

        var sound = Cache.GetSound(id);
        if (channelIdx < channels.Count)
        {
            Raylib.StopSound(sound);
            channels[channelIdx] = id;
            Raylib.PlaySound(sound);
            if (loopAudio) SetSoundLooping(id, true);
        }
        else
        {
            await Logger.LogWarnAsync("SoundPlayer", $"Invalid channel index: {channelIdx}");
        }
    }

    public static Task StopAsync(string id)
    {
        if (string.IsNullOrEmpty(id)) return Task.CompletedTask;

        var sound = Cache.GetSound(id);
        Raylib.StopSound(sound);

        return Task.CompletedTask;
    }

    public static async Task StopChannelAsync(int channelIdx)
    {
        if (channelIdx < channels.Count && !string.IsNullOrEmpty(channels[channelIdx]))
        {
            await StopAsync(channels[channelIdx]);
            channels[channelIdx] = string.Empty;
        }
        else
        {
            await Logger.LogWarnAsync("SoundPlayer", $"Invalid channel index: {channelIdx}");
        }
    }

    public static async Task KillAllAsync()
    {
        // TODO: Add non-kill sounds to specific scenes (for example, continuous
        // menu background music)

        for (var i = 0; i < channels.Count; i++)
            if (!string.IsNullOrEmpty(channels[i]))
            {
                await StopAsync(channels[i]);
                channels[i] = string.Empty;
            }

        loopingSounds.Clear();
        await Logger.LogAsync("SoundPlayer", "Stopped all sounds");
    }

    public static async Task SetChannelVolumeAsync(int channelIdx, float volume)
    {
        if (channelIdx < channels.Count && !string.IsNullOrEmpty(channels[channelIdx]))
            Raylib.SetSoundVolume(Cache.GetSound(channels[channelIdx]), volume);
        else
            await Logger.LogWarnAsync("SoundPlayer", $"Invalid channel index: {channelIdx}");
    }

    public static Task SetAllVolumesAsync(float volume)
    {
        foreach (var id in channels)
            if (!string.IsNullOrEmpty(id))
                Raylib.SetSoundVolume(Cache.GetSound(id), volume);

        return Task.CompletedTask;
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

    public static Task UpdateAsync()
    {
        foreach (var kvp in loopingSounds)
            if (kvp.Value && !Raylib.IsSoundPlaying(Cache.Sounds[kvp.Key]))
                Raylib.PlaySound(Cache.Sounds[kvp.Key]);
        return Task.CompletedTask;
    }
}