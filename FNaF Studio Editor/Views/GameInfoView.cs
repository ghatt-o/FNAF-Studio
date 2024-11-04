using System.Data;
using Editor.Controls;
using Editor.IO;
using ImGuiNET;

namespace Editor.Views;

public class ProjectInfoView : IContent
{
    private bool fullscreen;
    private string id = string.Empty;
    private string title = string.Empty;

    public void Initialize()
    {
        if (ProjectManager.Project == null)
            throw new NoNullAllowedException("ProjectManager.Project is null.");

        title = ProjectManager.Project.GameInfo.Title;
        id = ProjectManager.Project.GameInfo.ID;
        fullscreen = ProjectManager.Project.GameInfo.Fullscreen;
    }

    public void Render()
    {
        ImGui.Text("Project Information");
        ImGui.Separator();

        // Project Info
        ImGui.PushItemWidth(232);
        ImGui.Text("Project Title");
        ImGui.InputText("##0", ref title, 72);
        ImGui.Text("Project ID");
        ImGui.InputText("##1", ref id, 72);
        ImGui.PopItemWidth();
        ImGui.Checkbox("Fullscreen", ref fullscreen);

        // Save
        if (ProjectManager.Project == null)
            throw new NoNullAllowedException("ProjectManager.Project is null.");

        ProjectManager.Project.GameInfo.Title = title;
        ProjectManager.Project.GameInfo.ID = id;
        ProjectManager.Project.GameInfo.Fullscreen = fullscreen;
    }
}