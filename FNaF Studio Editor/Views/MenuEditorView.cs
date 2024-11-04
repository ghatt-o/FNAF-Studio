using System.Data;
using System.Numerics;
using Editor.Controls;
using Editor.IO;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;

namespace Editor.Views;

public class MenuEditorView : IContent
{
    private static readonly RenderTexture2D RenderTexture2D = Raylib.LoadRenderTexture(600, 337);
    private static readonly PropertiesControl Properties = new(new LockRecursionPolicy());

    private static Dictionary<string, GameJson.Menu> MenuItems = [];
    private string newMenuName = string.Empty;

    private bool showCreatePopup;
    public string CurrentMenu { get; private set; } = string.Empty;
    public string SelectedElementID { get; private set; } = string.Empty;

    public void Initialize()
    {
        if (ProjectManager.Project == null)
            return;

        MenuItems = ProjectManager.Project.Menus;
    }

    public void Render()
    {
        ImGui.Text("Menus");
        ImGui.Separator();
        DrawViewportAndProperties();
        DrawTabsAndContent();
    }

    private void DrawViewportAndProperties()
    {
        ImGui.BeginChild("ViewportAndProperties", new Vector2(600, 545), ImGuiChildFlags.None);

        DrawViewport();

        ImGui.BeginChild("Properties", new Vector2(800, 200), ImGuiChildFlags.None);
        if (!string.IsNullOrEmpty(CurrentMenu) && MenuItems.TryGetValue(CurrentMenu, out var value))
            Properties.SetObj(value
                .Properties); // TODO: use a custom MenuProperties class so we can use FileSelector and Color instead of strings
        Properties.Render();
        ImGui.EndChild();

        ImGui.EndChild();
    }

    private void DrawViewport()
    {
        if (ProjectManager.Project == null)
            throw new NoNullAllowedException("ProjectManager.Project is null.");

        ImGui.BeginChild("Viewport", new Vector2(600, 337), ImGuiChildFlags.None);

        Raylib.BeginTextureMode(RenderTexture2D);
        Raylib.ClearBackground(new Color(0, 0, 0, 255));

        if (!string.IsNullOrEmpty(CurrentMenu))
        {
            var reference = ProjectManager.Project.Menus[CurrentMenu];
            foreach (var element in reference.Elements)
                if (element.Type is "StaticText" or "Button")
                    Raylib.DrawText(element.Text, element.X, element.Y, (int)(element.Fontsize / 2.13f), Color.White);
        }

        Raylib.EndTextureMode();

        rlImGui.ImageRenderTexture(RenderTexture2D);

        ImGui.EndChild();
    }

    private void DrawTabsAndContent()
    {
        ImGui.SameLine();
        ImGui.BeginChild("TabsAndContent", new Vector2(205, 520), ImGuiChildFlags.None);

        if (ImGui.BeginTabBar("Tabs"))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(23, 5));
            DrawMenusTab();
            DrawElementsTab();
            ImGui.PopStyleVar();
            ImGui.EndTabBar();
        }

        ImGui.EndChild();
    }

    private void DrawMenusTab()
    {
        if (ProjectManager.Project == null)
            throw new NoNullAllowedException("ProjectManager.Project is null.");

        if (ImGui.BeginTabItem("Menus"))
        {
            ImGui.BeginChild("MenuList", new Vector2(200, 300), ImGuiChildFlags.None);

            foreach (var menu in MenuItems)
                if (ImGui.Selectable(menu.Key, CurrentMenu == menu.Key))
                    CurrentMenu = menu.Key;

            if (ImGui.Selectable("* Add Menu", false))
                showCreatePopup = true;

            if (showCreatePopup)
            {
                ImGui.OpenPopup("Create Menu");
                if (ImGui.BeginPopup("Create Menu"))
                {
                    ImGui.InputText("Name", ref newMenuName, 128);
                    if (ImGui.Button("Create"))
                        if (!string.IsNullOrEmpty(newMenuName) &&
                            !ProjectManager.Project.Menus.ContainsKey(newMenuName) && newMenuName != "* Add Menu")
                        {
                            ProjectManager.Project.Menus[newMenuName] = new GameJson.Menu();
                            newMenuName = string.Empty;
                            showCreatePopup = false;
                        }

                    ImGui.SameLine();
                    if (ImGui.Button("Cancel"))
                    {
                        showCreatePopup = false;
                        newMenuName = string.Empty;
                    }

                    ImGui.EndPopup();
                }
            }

            ImGui.EndChild();
            ImGui.EndTabItem();
        }
    }

    private void DrawElementsTab()
    {
        if (ImGui.BeginTabItem("Elements"))
        {
            ImGui.BeginChild("ElementList", new Vector2(200, 300), ImGuiChildFlags.None);

            if (!string.IsNullOrEmpty(CurrentMenu))
                foreach (var item in MenuItems[CurrentMenu].Elements)
                    if (ImGui.Selectable(item.ID, SelectedElementID == item.ID))
                        SelectedElementID = item.ID;

            ImGui.EndChild();
            ImGui.EndTabItem();
        }
    }
}