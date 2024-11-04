using System.Data;
using Editor.Controls;
using Editor.IO;
using ImGuiNET;

namespace Editor.Views;

public class PluginsView : IContent
{
    public void Initialize()
    {
        if (ProjectManager.Project == null)
            throw new NoNullAllowedException("ProjectManager.Project is null.");
    }

    public void Render()
    {
        ImGui.Text("Plugins");
        ImGui.Separator();

        // Save
        if (ProjectManager.Project == null)
            throw new NoNullAllowedException("ProjectManager.Project is null.");
        // save plugin information
    }
}