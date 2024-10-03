using Editor.IO;
using ImGuiNET;

namespace Editor.Controls;

public class TopBar
{
    private readonly ProjectManager projectManager;

    public TopBar(ProjectManager manager)
    {
        projectManager = manager;
    }

    public void Render()
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("New Project")) projectManager.CreateNewProject();
                if (ImGui.MenuItem("Open Project")) projectManager.OpenProject();
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Edit"))
            {
                if (ImGui.MenuItem("Undo"))
                {
                    /* TODO */
                }

                if (ImGui.MenuItem("Redo"))
                {
                    /* TODO */
                }

                ImGui.Separator();

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Options"))
            {
                if (ImGui.MenuItem("Settings"))
                {
                    /* TODO */
                }

                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }
    }
}