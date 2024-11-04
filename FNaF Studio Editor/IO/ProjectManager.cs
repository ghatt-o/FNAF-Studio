using System.Numerics;
using ImGuiNET;
using static Editor.IO.GameJson.Game;

namespace Editor.IO;

public class ProjectManager
{
    private static string title = "";
    private static string name = "";
    private static string id = "";

    public static string projectSpecialNameSelected = string.Empty;
    public static GameJson.Game? Project;
    private readonly string[] options = ["Classic FNAF"];
    private int selectedOption;

    public ProjectManager()
    {
        IsProjectOpen = false;
    }

    public bool IsProjectOpen { get; private set; }

    public void CreateNewProject()
    {
        Studio.renderCallbacks.Add(RenderProjectCreationDialog);
    }

    public void OpenProject()
    {
        Studio.renderCallbacks.Add(RenderProjectImportingDialog);
    }

    public void MakeProjectAndLoad(string name, string title, string id, int style)
    {
        if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "data/projects"))
            Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "data/projects");

        projectSpecialNameSelected = name;
        string[] directories =
        [
            // Resources
            "sprites",
            "sounds",
            "animations",
            "fonts",
            "special_sprites",
            // Functionality
            "scripts"
        ];

        Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "data/projects/" + name);
        foreach (var dir in directories)
            Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "data/projects/" + name + "/" + dir);

        Project = new GameJson.Game
        {
            Name = name
        };
        Project.GameInfo.Title = title;
        Project.GameInfo.ID = id;
        Project.GameInfo.Style = style;

        Project.Save();

        IsProjectOpen = true;
    }

    private void RenderProjectCreationDialog()
    {
        ImGui.OpenPopup("Create New Project");
        if (ImGui.BeginPopupModal("Create New Project", ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.InputText("Project Name", ref name, 100);
            ImGui.Separator();
            ImGui.InputText("Project Title", ref title, 100);
            ImGui.InputText("Project ID", ref id, 100);
            ImGui.Combo("Game Style", ref selectedOption, options, options.Length);

            if (ImGui.Button("Create"))
            {
                MakeProjectAndLoad(name, title, id, selectedOption);
                ImGui.CloseCurrentPopup();
                Studio.renderCallbacks.Remove(RenderProjectCreationDialog);
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
                Studio.renderCallbacks.Remove(RenderProjectCreationDialog);
            }

            ImGui.EndPopup();
        }
    }

    private void RenderProjectImportingDialog()
    {
        ImGui.OpenPopup("Import a Project");
        if (ImGui.BeginPopupModal("Import a Project", ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (ImGui.BeginTabBar("Lists"))
            {
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(24, 5));
                if (ImGui.BeginTabItem(" Projects "))
                {
                    ImGui.BeginChild("ProjectList", new Vector2(200, 300), ImGuiChildFlags.None);

                    if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "data/projects"))
                        Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "data/projects");

                    foreach (var dir in Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory +
                                                                 "data/projects/"))
                    {
                        var directoryInfo = new DirectoryInfo(dir);
                        var selectedDir = false;
                        if (ImGui.Selectable(directoryInfo.Name, ref selectedDir))
                            projectSpecialNameSelected = directoryInfo.Name;
                    }

                    ImGui.Separator();
                    ImGui.Text("Selecting " + projectSpecialNameSelected);

                    ImGui.EndChild();
                    ImGui.EndTabItem();
                }

                ImGui.PopStyleVar();
                ImGui.EndTabBar();
            }

            if (ImGui.Button("Import"))
            {
                if (string.IsNullOrEmpty(projectSpecialNameSelected))
                    return;

                ImGui.CloseCurrentPopup();
                Project = Load(AppDomain.CurrentDomain.BaseDirectory + "data/projects/" + projectSpecialNameSelected +
                               "/game.json");
                IsProjectOpen = true;
                Studio.ContentView.UpdateContent("Project Info");
                Studio.renderCallbacks.Remove(RenderProjectImportingDialog);
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
                Studio.renderCallbacks.Remove(RenderProjectImportingDialog);
            }

            ImGui.EndPopup();
        }
    }
}