using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using FNAFStudio_Runtime_RCS.Data.Definitions.GameObjects;
using Microsoft.Win32;
using Raylib_CsLo;

namespace FNAFStudio_Runtime_RCS.Data;

public static partial class Cache
{
    public static Dictionary<string, Font> Fonts = [];
    public static Dictionary<string, Texture> Sprites = [];
    public static Dictionary<string, RevAnimation> Animations = [];
    public static Dictionary<string, Sound> Sounds = [];

    private static Dictionary<string, string>? linuxFontCache;

    public static Sound GetSound(string soundName)
    {
        if (Sounds.TryGetValue(soundName, out var value))
            return value;

        var soundPath = soundName.StartsWith("e.")
            ? AppDomain.CurrentDomain.BaseDirectory + "res/" + soundName
            : $"{GameState.ProjectPath}/sounds/{soundName}".Replace("\\", "/");

        return LoadSoundToSounds(soundName, soundPath);
    }

    public static Sound LoadSoundToSounds(string soundName, string fullSoundPath)
    {
        var loadedSound = soundName.EndsWith(".wav")
            ? Raylib.LoadSoundFromWave(Raylib.LoadWave(fullSoundPath.Replace("\\", "/")))
            : Raylib.LoadSound(fullSoundPath.Replace("\\", "/"));
        Sounds[soundName] = loadedSound;

        return loadedSound;
    }

    public static Font GetFont(string fontName, int fontSize)
    {
        var uid = $"{fontName}-{fontSize}";
        if (Fonts.TryGetValue(uid, out var font))
            return font;

        var fontPath = GetSystemFontPath(fontName) ?? string.Empty;
        unsafe
        {
            font = string.IsNullOrEmpty(fontPath)
                ? Raylib.GetFontDefault()
                : Raylib.LoadFontEx(fontPath, fontSize, null, 0);
        }

        Raylib.SetTextureFilter(font.texture, TextureFilter.TEXTURE_FILTER_TRILINEAR);
        Fonts[uid] = font;

        return font;
    }

    public static string GetFontUID(string fontName, int fontSize)
    {
        return $"{fontName}-{fontSize}";
    }

    private static string? GetSystemFontPath(string fontName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var fontsKey =
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts");
            if (fontsKey != null)
                foreach (var font in fontsKey.GetValueNames())
                    if (font.StartsWith(fontName, StringComparison.OrdinalIgnoreCase))
                    {
                        var fontPathVal = fontsKey.GetValue(font);
                        if (fontPathVal != null)
                            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
                                fontPathVal.ToString() ?? "");
                    }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            linuxFontCache ??= InitializeLinuxFontCache();
            if (linuxFontCache.TryGetValue(fontName, out var fontPath)) return fontPath;
        }

        return null;
    }

    private static Dictionary<string, string> InitializeLinuxFontCache()
    {
        var fontCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        ProcessStartInfo psi = new()
        {
            FileName = "fc-list", Arguments = "", RedirectStandardOutput = true, UseShellExecute = false,
            CreateNoWindow = true
        };
        using (var process = Process.Start(psi))
        {
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                var regex = FontRegex();
                foreach (Match match in regex.Matches(output))
                {
                    var filePath = match.Groups["path"].Value.Trim();
                    var fontName = match.Groups["name"].Value.Trim();
                    if (!fontCache.ContainsKey(fontName)) fontCache[fontName] = filePath;
                }
            }
        }

        return fontCache;
    }

    private static Texture LoadImageToSprite(string textureName, string fullTexturePath)
    {
        var loadedImage = Raylib.LoadTexture(fullTexturePath.Replace("\\", "/")); // can return empty textures
        Sprites[textureName] = loadedImage;

        return loadedImage;
    }

    public static Texture GetTexture(string? texName)
    {
        if (!string.IsNullOrEmpty(texName))
        {
            if (Sprites.TryGetValue(texName, out var value))
                return value;

            string texturePath;

            void SetTexturePath(string textureName, string baseFolder) =>
                texturePath = $"{GameState.ProjectPath}/{baseFolder}/{(textureName.StartsWith("e.") ? $"{textureName}.png" : textureName )}";

            if (texName.StartsWith("e."))
                SetTexturePath(texName, "special_sprites");
            else
                SetTexturePath(texName.Replace("\\", "/"), "sprites");

            return LoadImageToSprite(texName, texturePath);
        }

        return new Texture();
    }

    public static RevAnimation GetAnimation(string animName, bool loop = true)
    {
        if (Animations.TryGetValue(animName, out var value))
            return value;

        RevAnimation anim = new($"{GameState.ProjectPath}/animations/{animName}.json", loop);
        Animations[animName] = anim;
        return anim;
    }

    public static bool CacheFonts()
    {
        foreach (var fontPath in Directory.EnumerateFiles($"{GameState.ProjectPath}/fonts"))
        {
            Font loadedFont;
            unsafe
            {
                loadedFont = Raylib.LoadFontEx(fontPath, 72, null, 0);
            }

            Fonts[GetFontUID(Path.GetFileName(fontPath), 72)] = loadedFont;
        }

        return true;
    }

    [GeneratedRegex(@"^(?<path>.+?):\s*(?<name>.+?):style=",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled, "en-US")]
    private static partial Regex FontRegex();
}

public static class GameCache
{
    public static Dictionary<string, Text> Texts = [];
    public static Dictionary<string, Button2D> Buttons = [];

    public static class HudCache
    {
        public static Text Power = new("", 26, "Consolas", Raylib.WHITE);
        public static Text Usage = new("", 26, "Consolas", Raylib.WHITE);
        public static Text Time = new("", 26, GameState.Project.Offices[OfficeCore.Office ?? "Office"].TextFont ?? "LCD Solid", Raylib.WHITE);
        public static Text Night = new("", 22, GameState.Project.Offices[OfficeCore.Office ?? "Office"].TextFont ?? "LCD Solid", Raylib.WHITE);
        public static RevAnimation CameraAnim = Cache.GetAnimation(GameState.Project.Office.Animations.Camera, false);
        public static RevAnimation MaskAnim = Cache.GetAnimation(GameState.Project.Office.Animations.Mask, false);
    }
}