using Editor.Controls;
using ImGuiNET;

namespace Editor.Views;

public class OfficeEditorView : IContent
{

    public void Render()
    {
        ImGui.Text("Offices");
        ImGui.Separator();
    }
}