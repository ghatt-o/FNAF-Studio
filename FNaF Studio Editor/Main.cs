using Editor.Controls;
using Editor.IO;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Editor;

public class Studio
{
    private readonly ProjectManager projectManager;
    private readonly SideBar sideBar;
    private readonly TopBar topBar;

    public static ContentView ContentView = new();
    public static List<Action> renderCallbacks = [];

    public Studio()
    {
        projectManager = new ProjectManager();
        topBar = new TopBar(projectManager);
        ContentView = new ContentView();
        sideBar = new SideBar(projectManager, ContentView);
    }

    static void Main()
    {
        const int screenWidth = 1024;
        const int screenHeight = 576;

        Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
        Raylib.InitWindow(screenWidth, screenHeight, "FNAF Studio");
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint);
        Raylib.SetTargetFPS(240);

        rlImGui.Setup(true);
        var io = ImGui.GetIO();
        unsafe
        {
            io.NativePtr->IniFilename = null;
        }

        // ---- FONT ----
        byte[] fontData = Assets.Assets.Arial;
        ImFontPtr fontPtr;

        GCHandle handle = GCHandle.Alloc(fontData, GCHandleType.Pinned);
        try
        {
            nint fontDataPtr = handle.AddrOfPinnedObject();
            fontPtr = io.Fonts.AddFontFromMemoryTTF(fontDataPtr, fontData.Length, 18);
        }
        finally
        {
            handle.Free(); // free the pinned handle to avoid memory leaks
        }
        rlImGui.ReloadFonts();
        // ---- FONT ----

        var Studio = new Studio();

        while (!Raylib.WindowShouldClose())
        {
            rlImGui.Begin();

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.DarkGray);
            ImGui.PushFont(fontPtr);
            Studio.Render();
            ImGui.PopFont();
            rlImGui.End();
            Raylib.EndDrawing();
        }

        //if (ProjectManager.Project != null)
        //ProjectManager.Project.Save();
        rlImGui.Shutdown();
        Raylib.CloseWindow();
    }

    public void Render()
    {
        SetupImGuiStyle();

        ImGui.SetNextWindowPos(new Vector2(0, 0));
        ImGui.SetNextWindowSize(new Vector2(9999, 25));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.Begin("TopBar", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);
        topBar.Render();
        ImGui.End();

        ImGui.SetNextWindowPos(new Vector2(0, 25));
        ImGui.SetNextWindowSize(new Vector2(200, 9999));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, ImGui.GetStyle().Colors[(int)ImGuiCol.MenuBarBg]);
        ImGui.Begin("Sidebar", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);
        sideBar.Render();
        ImGui.End();
        ImGui.PopStyleVar();
        ImGui.PopStyleColor();

        ImGui.SetNextWindowPos(new Vector2(202, 25));
        ImGui.SetNextWindowSize(new Vector2(9999, 9999));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.Begin("ContentView", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);
        ContentView.Render();
        ImGui.End();
        ImGui.PopStyleVar();

        foreach (var callback in new List<Action>(renderCallbacks)) callback.Invoke();
    }

    private static void SetupImGuiStyle()
    {
        var style = ImGui.GetStyle();

        style.Alpha = 1.0f;
        style.DisabledAlpha = 1.0f;
        style.WindowPadding = new Vector2(12.0f, 12.0f);
        style.WindowRounding = 5f;
        style.WindowBorderSize = 0.0f;
        style.WindowMinSize = new Vector2(20.0f, 20.0f);
        style.WindowTitleAlign = new Vector2(0.5f, 0.5f);
        style.WindowMenuButtonPosition = ImGuiDir.Right;
        style.ChildRounding = 0.0f;
        style.ChildBorderSize = 1.0f;
        style.PopupRounding = 0.0f;
        style.PopupBorderSize = 1.0f;
        style.FramePadding = new Vector2(20.0f, 5f);
        style.ItemSpacing = new Vector2(4f, 7f);
        style.FrameRounding = 5f;
        style.FrameBorderSize = 0.0f;
        style.ItemSpacing = new Vector2(4.3f, 5.5f);
        style.ItemInnerSpacing = new Vector2(7.1f, 1.8f);
        style.CellPadding = new Vector2(12.1f, 9.2f);
        style.IndentSpacing = 0.0f;
        style.ColumnsMinSpacing = 4.9f;
        style.ScrollbarSize = 11.6f;
        style.ScrollbarRounding = 15.9f;
        style.GrabRounding = 5.0f;
        style.TabRounding = 5f;
        style.TabBorderSize = 0.0f;
        style.IndentSpacing = 15f;
        style.GrabMinSize = 10f;
        style.TabMinWidthForCloseButton = 0.0f;
        style.ColorButtonPosition = ImGuiDir.Right;
        style.ButtonTextAlign = new Vector2(0.5f, 0.5f);
        style.SelectableTextAlign = new Vector2(0.0f, 0.0f);

        style.Colors[(int)ImGuiCol.Text] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.27f, 0.32f, 0.45f, 1.0f);
        style.Colors[(int)ImGuiCol.ChildBg] = new Vector4(0.09f, 0.10f, 0.12f, 1.0f);
        style.Colors[(int)ImGuiCol.PopupBg] = new Vector4(0.08f, 0.09f, 0.10f, 1.0f);
        style.Colors[(int)ImGuiCol.Border] = new Vector4(0.16f, 0.17f, 0.19f, 1.0f);
        style.Colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.08f, 0.09f, 0.10f, 1.0f);
        style.Colors[(int)ImGuiCol.FrameBg] = new Vector4(0.11f, 0.13f, 0.15f, 1.0f);
        style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.16f, 0.17f, 0.19f, 1.0f);
        style.Colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.16f, 0.17f, 0.19f, 1.0f);
        style.Colors[(int)ImGuiCol.TitleBg] = new Vector4(0.05f, 0.05f, 0.07f, 1.0f);
        style.Colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.05f, 0.05f, 0.07f, 1.0f);
        style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.08f, 0.09f, 0.10f, 1.0f);
        style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.05f, 0.05f, 0.07f, 1.0f);
        style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.12f, 0.13f, 0.15f, 1.0f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.16f, 0.17f, 0.19f, 1.0f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.12f, 0.13f, 0.15f, 1.0f);
        style.Colors[(int)ImGuiCol.CheckMark] = new Vector4(0.97f, 1.0f, 0.50f, 1.0f);
        style.Colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.97f, 1.0f, 0.50f, 1.0f);
        style.Colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(1.0f, 0.80f, 0.50f, 1.0f);
        style.Colors[(int)ImGuiCol.Button] = new Vector4(0.12f, 0.13f, 0.15f, 1.0f);
        style.Colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.18f, 0.19f, 0.20f, 1.0f);
        style.Colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.15f, 0.15f, 0.15f, 1.0f);
        style.Colors[(int)ImGuiCol.Header] = new Vector4(0.14f, 0.16f, 0.21f, 1.0f);
        style.Colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.11f, 0.11f, 0.11f, 1.0f);
        style.Colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.08f, 0.09f, 0.10f, 1.0f);
        style.Colors[(int)ImGuiCol.Separator] = new Vector4(0.13f, 0.15f, 0.19f, 1.0f);
        style.Colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.16f, 0.18f, 0.25f, 1.0f);
        style.Colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.16f, 0.18f, 0.25f, 1.0f);
        style.Colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.15f, 0.15f, 0.15f, 1.0f);
        style.Colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.97f, 1.0f, 0.50f, 1.0f);
        style.Colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.Tab] = new Vector4(0.14f, 0.16f, 0.21f, 1.0f);
        style.Colors[(int)ImGuiCol.TabDimmed] = new Vector4(0.13f, 0.15f, 0.20f, 1.0f);
        style.Colors[(int)ImGuiCol.TabHovered] = new Vector4(0.11f, 0.11f, 0.11f, 1.0f);
        style.Colors[(int)ImGuiCol.TabSelected] = new Vector4(0.14f, 0.16f, 0.21f, 1.0f);
        style.Colors[(int)ImGuiCol.TabSelectedOverline] = new Vector4(0.97f, 1.0f, 0.50f, 1.0f);
        style.Colors[(int)ImGuiCol.TabDimmedSelectedOverline] = new Vector4(0.96f, 0.9f, 0.49f, 1.0f);
        style.Colors[(int)ImGuiCol.DockingPreview] = new Vector4(0.97f, 1.0f, 0.50f, 1.0f);
        style.Colors[(int)ImGuiCol.DockingEmptyBg] = new Vector4(0.08f, 0.09f, 0.10f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotLines] = new Vector4(0.61f, 0.61f, 0.61f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4(0.97f, 1.0f, 0.50f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotHistogram] = new Vector4(0.97f, 1.0f, 0.50f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(0.97f, 1.0f, 0.50f, 1.0f);
        style.Colors[(int)ImGuiCol.TableHeaderBg] = new Vector4(0.09f, 0.10f, 0.12f, 1.0f);
        style.Colors[(int)ImGuiCol.TableBorderStrong] = new Vector4(0.09f, 0.10f, 0.12f, 1.0f);
        style.Colors[(int)ImGuiCol.TableBorderLight] = new Vector4(0.09f, 0.10f, 0.12f, 1.0f);
        style.Colors[(int)ImGuiCol.TableRowBg] = new Vector4(0.08f, 0.09f, 0.10f, 1.0f);
        style.Colors[(int)ImGuiCol.TableRowBgAlt] = new Vector4(0.09f, 0.10f, 0.12f, 1.0f);
        style.Colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.12f, 0.13f, 0.15f, 1.0f);
        style.Colors[(int)ImGuiCol.DragDropTarget] = new Vector4(0.97f, 1.0f, 0.50f, 1.0f);
        style.Colors[(int)ImGuiCol.NavHighlight] = new Vector4(0.97f, 1.0f, 0.50f, 1.0f);
        style.Colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0.0f, 0.0f, 0.0f, 0.3f);
        style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.0f, 0.0f, 0.0f, 0.3f);
        style.Colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.10f, 0.11f, 0.12f, 1.0f);
        style.Colors[(int)ImGuiCol.WindowBg] = new Vector4(0.08f, 0.09f, 0.10f, 1.0f);
    }
}