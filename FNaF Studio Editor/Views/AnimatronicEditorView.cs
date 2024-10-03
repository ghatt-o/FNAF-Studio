using Editor.Controls;
using Editor.IO;
using ImGuiNET;
using System.Data;
using System.Numerics;
using static Editor.IO.GameJson;

namespace Editor.Views;

public class AnimatronicEditorView : IContent
{
    private int animatronicIndex = -1;
    private int currentNight = 1;
    private string newAnimatronicName = string.Empty;
    private Animatronic? selectedAnimatronic;
    private bool showCreatePopup;

    public void Render()
    {
        ImGui.SeparatorText("Animatronics");

        if (ProjectManager.Project == null)
            throw new NoNullAllowedException("ProjectManager.Project is null.");

        var animatronics = ProjectManager.Project.Animatronics;
        string[] keyArray = [.. animatronics.Keys];

        ImGui.PushItemWidth(205); // fixes the cutoff on the "Remove" button
        if (ImGui.Combo("##SelectAnimatronic", ref animatronicIndex, keyArray, keyArray.Length) && animatronicIndex >= 0)
        {
            selectedAnimatronic = animatronics[keyArray[animatronicIndex]];
        }
        ImGui.PopItemWidth();

        if (ImGui.Button("Add", new Vector2(100, 0)))
            showCreatePopup = true;

        ImGui.SameLine();
        if (ImGui.Button("Remove", new Vector2(100, 0)) && keyArray.Length > 0 && selectedAnimatronic != null)
        {
            animatronics.Remove(keyArray[animatronicIndex]);
            ResetEditorState();
        }

        ImGui.Spacing();

        if (selectedAnimatronic != null)
        {
            RenderAnimatronicDetails();
        }
        else
        {
            ImGui.Text("No animatronic available.");
        }

        HandleCreatePopup();
    }

    private void RenderAnimatronicDetails()
    {
        if (selectedAnimatronic == null)
            return;

        ImGui.SeparatorText("AI Levels");

        // Night Selection
        ImGui.Text($"Night {currentNight}");
        ImGui.SameLine();
        if (ImGui.ArrowButton("##prevNight", ImGuiDir.Left) && currentNight > 1) currentNight--;
        ImGui.SameLine();
        if (ImGui.ArrowButton("##nextNight", ImGuiDir.Right) && currentNight < 6) currentNight++;

        int aiLevel = selectedAnimatronic.AI.Count >= currentNight ? selectedAnimatronic.AI[currentNight - 1] : 0;

        ImGui.Text("AI Level");
        ImGui.SameLine();
        ImGui.PushItemWidth(130);
        ImGui.SliderInt($"##AILevel{currentNight}", ref aiLevel, 0, 21);
        ImGui.PopItemWidth();

        if (selectedAnimatronic.AI.Count < currentNight)
        {
            selectedAnimatronic.AI.Add(aiLevel);
        }
        else
        {
            selectedAnimatronic.AI[currentNight - 1] = aiLevel;
        }

        ImGui.Spacing();

        ImGui.SeparatorText("Jumpscare Settings");

        // Jumpscare animation
        RenderJumpscareSettings();

        // Phantom and Mask Settings
        bool phantom = selectedAnimatronic.Phantom;
        ImGui.Checkbox("Phantom", ref phantom);
        selectedAnimatronic.Phantom = phantom;

        bool ignoreMask = selectedAnimatronic.IgnoreMask;
        ImGui.Checkbox("Ignore Mask", ref ignoreMask);
        selectedAnimatronic.IgnoreMask = ignoreMask;

        ImGui.Spacing();
        ImGui.SeparatorText("Other");
    }

    private void RenderJumpscareSettings()
    {
        if (selectedAnimatronic == null)
            return;

        if (ImGui.Button("Set Jumpscare Animation", new Vector2(220, 0)))
        {
            selectedAnimatronic.Jumpscare[0] = "animation here";
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear", new Vector2(80, 0)))
        {
            selectedAnimatronic.Jumpscare[0] = string.Empty;
        }

        ImGui.SameLine();
        ImGui.Text(selectedAnimatronic.Jumpscare[0] ?? string.Empty);

        if (ImGui.Button("Set Jumpscare Sound", new Vector2(220, 0)))
        {
            selectedAnimatronic.Jumpscare[1] = "sound here";
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear ", new Vector2(80, 0)))
        {
            selectedAnimatronic.Jumpscare[1] = string.Empty;
        }

        ImGui.SameLine();
        ImGui.Text(selectedAnimatronic.Jumpscare[1] ?? string.Empty);
    }

    private void HandleCreatePopup()
    {
        if (ProjectManager.Project == null)
            throw new NoNullAllowedException("ProjectManager.Project is null.");

        if (showCreatePopup)
        {
            ImGui.OpenPopup("Create Animatronic");
            if (ImGui.BeginPopup("Create Animatronic"))
            {
                ImGui.InputText("Name", ref newAnimatronicName, 128);
                if (ImGui.Button("Create") && !string.IsNullOrEmpty(newAnimatronicName))
                {
                    if (!ProjectManager.Project.Animatronics.ContainsKey(newAnimatronicName))
                    {
                        CreateNewAnimatronic(newAnimatronicName);
                    }
                }

                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ResetCreatePopup();
                }

                ImGui.EndPopup();
            }
        }
    }

    private void CreateNewAnimatronic(string name)
    {
        if (ProjectManager.Project == null)
            throw new NoNullAllowedException("ProjectManager.Project is null.");

        var newAnimatronic = new Animatronic
        {
            IgnoreMask = true,
            Jumpscare = ["", ""]
        };
        ProjectManager.Project.Animatronics[name] = newAnimatronic;
        ResetCreatePopup();
    }

    private void ResetCreatePopup()
    {
        showCreatePopup = false;
        newAnimatronicName = string.Empty;
    }

    private void ResetEditorState()
    {
        selectedAnimatronic = null;
        animatronicIndex = -1;
        currentNight = 1;
    }
}
