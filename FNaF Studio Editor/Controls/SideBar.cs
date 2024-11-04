using System.Numerics;
using Editor.IO;
using Editor.Views;
using ImGuiNET;
using Raylib_cs;

namespace Editor.Controls;

public class SideBar
{
    private readonly ContentView contentView;
    private readonly ProjectManager projectManager;
    private readonly List<nint> Texture2DIDs = [];

    public SideBar(ProjectManager manager, ContentView contentViewInstance)
    {
        projectManager = manager;
        contentView = contentViewInstance;

        // -- projects
        contentView.RegisterContent("Project Info", new ProjectInfoView());
        // -- game
        contentView.RegisterContent("Menus Editor", new MenuEditorView());
        contentView.RegisterContent("Office Editor", new OfficeEditorView());
        // "camera editor" here
        contentView.RegisterContent("Animatronics", new AnimatronicEditorView());
        // -- resources
        // "animations" here
        contentView.RegisterContent("Sounds", new SoundsView());
        // -- scripting
        // "script editor" here
        contentView.RegisterContent("Plugins", new PluginsView());
        LoadTexture2Ds();
    }

    private void LoadTexture2Ds()
    {
        byte[][] textureResources =
        {
            Assets.Assets.createproject,
            Assets.Assets.openproject,
            Assets.Assets.templates,
            Assets.Assets.extensions,
            Assets.Assets.gameinfo,
            Assets.Assets.menuseditor,
            Assets.Assets.officeeditor,
            Assets.Assets.cameraeditor,
            Assets.Assets.animatronics,
            Assets.Assets.animations,
            Assets.Assets.sounds,
            Assets.Assets.scripteditor
        };

        foreach (var textureData in textureResources)
            unsafe
            {
                fixed (byte* rawData = textureData)
                {
                    fixed (sbyte* Filetype = &Array.ConvertAll(".png".GetUTF8Bytes(), q => Convert.ToSByte(q))[0])
                    {
                        var image = Raylib.LoadImageFromMemory(Filetype, rawData, textureData.Length);
                        var texture = Raylib.LoadTextureFromImage(image);

                        Raylib.SetTextureFilter(texture, TextureFilter.Bilinear);
                        Raylib.SetTextureFilter(texture, TextureFilter.Anisotropic16X);

                        Texture2DIDs.Add((nint)texture.Id);

                        Raylib.UnloadImage(image);
                    }
                }
            }
    }

    private static void RenderButtonWithImage(string label, nint Texture2DID, Action onClick)
    {
        ImGui.BeginGroup();
        var imageSize = new Vector2(32, 32);
        ImGui.Image(Texture2DID, imageSize);
        ImGui.SameLine();
        if (ImGui.Button(label, new Vector2(ImGui.GetContentRegionAvail().X, 32))) onClick.Invoke();
        ImGui.EndGroup();
    }

    public void Render()
    {
        ImGui.Begin("Sidebar");

        if (!projectManager.IsProjectOpen)
        {
            RenderButtonWithImage("Create Project", Texture2DIDs[0], () => projectManager.CreateNewProject());
            ImGui.Spacing();
            RenderButtonWithImage("Open Project", Texture2DIDs[1], () => projectManager.OpenProject());
            ImGui.Separator();
            ImGui.Spacing();
            RenderButtonWithImage("Templates", Texture2DIDs[2], () => { });
            ImGui.Spacing();
            RenderButtonWithImage("Plugins", Texture2DIDs[3], () => { });
        }
        else
        {
            ImGui.SeparatorText("Project");
            RenderButtonWithImage("Project Info", Texture2DIDs[4], () => contentView.UpdateContent("Project Info"));
            ImGui.Spacing();
            ImGui.SeparatorText("Game");
            RenderButtonWithImage("Menus Editor", Texture2DIDs[5], () => contentView.UpdateContent("Menus Editor"));
            ImGui.Spacing();
            RenderButtonWithImage("Office Editor", Texture2DIDs[6], () => contentView.UpdateContent("Office Editor"));
            ImGui.Spacing();
            RenderButtonWithImage("Camera Editor", Texture2DIDs[7], () => contentView.UpdateContent("Camera Editor"));
            ImGui.Spacing();
            RenderButtonWithImage("Animatronics", Texture2DIDs[8], () => contentView.UpdateContent("Animatronics"));
            ImGui.SeparatorText("Resources");
            ImGui.Spacing();
            RenderButtonWithImage("Animations", Texture2DIDs[9], () => contentView.UpdateContent("Animations"));
            ImGui.Spacing();
            RenderButtonWithImage("Sounds", Texture2DIDs[10], () => contentView.UpdateContent("Sounds"));
            ImGui.SeparatorText("Scripting");
            ImGui.Spacing();
            RenderButtonWithImage("Script Editor", Texture2DIDs[11], () => contentView.UpdateContent("Script Editor"));
            ImGui.Spacing();
            RenderButtonWithImage("Plugins", Texture2DIDs[3], () => contentView.UpdateContent("Plugins"));
        }

        ImGui.End();
    }
}