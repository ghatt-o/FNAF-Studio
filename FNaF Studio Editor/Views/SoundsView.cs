using System.Data;
using Editor.Controls;
using Editor.IO;
using ImGuiNET;

namespace Editor.Views;

public class SoundsView : IContent
{
    private string[]? availableSoundTypes;
    private string currentField = string.Empty;
    private string selectedMoveSoundIndex = "None";
    private string selectedPhoneCallIndex = "None";
    private string selectedSoundType = string.Empty;
    private bool showFilePickerPopup;
    private Dictionary<string, string> soundFileMap = [];

    public void Initialize()
    {
        if (ProjectManager.Project == null)
            throw new NoNullAllowedException("ProjectManager.Project is null.");

        var sounds = ProjectManager.Project.Sounds;
        soundFileMap = new Dictionary<string, string>
        {
            { "Ambience", sounds.Ambience },
            { "Blip", sounds.Blip },
            { "Cam Down", sounds.Camdown },
            { "Cam Up", sounds.Camup },
            { "Flashlight", sounds.Flashlight },
            { "Mask Breathing", sounds.MaskBreathing },
            { "Mask Off", sounds.Maskoff },
            { "Mask On", sounds.Maskon },
            { "Mask Toxic", sounds.MaskToxic },
            { "Music Box Run Out", sounds.MusicBoxRunOut },
            { "Power Out", sounds.Powerout },
            { "Signal Interrupted", sounds.SignalInterrupted },
            { "Stare", sounds.Stare }
        };

        availableSoundTypes = new string[soundFileMap.Keys.Count];
        soundFileMap.Keys.CopyTo(availableSoundTypes, 0);
    }

    public void Render()
    {
        if (ProjectManager.Project == null)
            throw new NoNullAllowedException("ProjectManager.Project is null.");

        ImGui.Text("Sounds");
        ImGui.Separator();

        RenderSoundTypeDropdown();
        HandleSelectedSound();

        ImGui.Spacing();
        ImGui.Spacing();
        RenderIndexedDropdown("Animatronic Movement", "Move Sound", ProjectManager.Project.Sounds.AnimatronicMove,
            ref selectedMoveSoundIndex);
        RenderIndexedDropdown("Phone Calls", "Night", ProjectManager.Project.Sounds.PhoneCalls,
            ref selectedPhoneCallIndex);

        HandleFilePickerPopup();
    }

    private void RenderSoundTypeDropdown()
    {
        ImGui.SetNextItemWidth(170);
        if (ImGui.BeginCombo("##soundTypeCombo", selectedSoundType))
        {
            if (availableSoundTypes != null)
                foreach (var soundType in availableSoundTypes)
                {
                    var isSelected = soundType == selectedSoundType;
                    if (ImGui.Selectable(soundType, isSelected))
                    {
                        selectedSoundType = soundType;
                        currentField = selectedSoundType;
                    }

                    if (isSelected) ImGui.SetItemDefaultFocus();
                }

            ImGui.EndCombo();
        }
    }

    private void HandleSelectedSound()
    {
        if (!string.IsNullOrEmpty(selectedSoundType))
        {
            var soundFile = soundFileMap[selectedSoundType];
            ImGui.SameLine();
            ImGui.Text($"{(string.IsNullOrEmpty(soundFile) ? "None" : soundFile)}");

            if (ImGui.Button($"Set File##{selectedSoundType}"))
            {
                showFilePickerPopup = true;
                currentField = selectedSoundType;
            }

            ImGui.SameLine();
            if (ImGui.Button($"Clear File##{selectedSoundType}")) soundFileMap[selectedSoundType] = string.Empty;
        }
    }

    private void RenderIndexedDropdown(string label, string prefix, List<string> list, ref string selectedIndex)
    {
        ImGui.SeparatorText(label);
        ImGui.SetNextItemWidth(170);

        var currentSelection = selectedIndex;

        if (ImGui.BeginCombo($"##{label}_Dropdown", currentSelection))
        {
            for (var i = 0; i < list.Count; i++)
            {
                var indexLabel = $"{prefix} {i + 1}";
                var isSelected = indexLabel == currentSelection;
                if (ImGui.Selectable(indexLabel, isSelected))
                {
                    selectedIndex = indexLabel;
                    currentField = selectedIndex;
                }

                if (isSelected) ImGui.SetItemDefaultFocus();
            }

            if (ImGui.Selectable($"* Add New {prefix}"))
            {
                list.Add(string.Empty);
                selectedIndex = $"{prefix} {list.Count}";
                currentField = selectedIndex;
            }

            ImGui.EndCombo();
        }

        if (currentSelection != "None")
        {
            var index = int.Parse(string.Concat(selectedIndex.Where(char.IsDigit))) - 1;
            if (index >= 0 && index < list.Count)
            {
                ImGui.SameLine();
                ImGui.Text($"{(string.IsNullOrEmpty(list[index]) ? "None" : list[index])}");

                if (ImGui.Button($"Set File##{prefix}_{index}"))
                {
                    showFilePickerPopup = true;
                    currentField = selectedIndex;
                }

                ImGui.SameLine();

                if (ImGui.Button($"Clear File##{prefix}_{index}")) list[index] = string.Empty;

                ImGui.SameLine();

                if (ImGui.Button($"Remove##{prefix}_{index}"))
                {
                    if (index == list.Count - 1)
                        list.RemoveAt(index);
                    else
                        list[index] = string.Empty; // cant delete a night that isn't at the end, so make it empty

                    if (currentField.StartsWith("Move Sound"))
                        selectedMoveSoundIndex = "None";
                    else if (currentField.StartsWith("Night"))
                        selectedPhoneCallIndex = "None";
                }
            }
        }

        ImGui.Spacing();
        ImGui.Spacing();
    }

    private void HandleFilePickerPopup()
    {
        if (showFilePickerPopup)
        {
            ImGui.OpenPopup("File Picker");
            if (ImGui.BeginPopup("File Picker"))
            {
                ImGui.Text($"Select a file for: {currentField}");

                ImGui.Separator();
                ImGui.Text("pretend that this file picker works");

                if (ImGui.Button("Confirm"))
                {
                    var selectedFilePath = "TotallyARealFile.wav";
                    AssignSelectedFilePath(selectedFilePath);
                    showFilePickerPopup = false;
                }

                ImGui.SameLine();

                if (ImGui.Button("Cancel")) showFilePickerPopup = false;

                ImGui.EndPopup();
            }
        }
    }

    private void AssignSelectedFilePath(string filePath)
    {
        if (ProjectManager.Project == null)
            throw new NoNullAllowedException("ProjectManager.Project is null.");

        if (currentField == selectedSoundType)
        {
            soundFileMap[selectedSoundType] = filePath;
        }
        else
        {
            var index = int.Parse(string.Concat(currentField.Where(char.IsDigit))) - 1;
            if (currentField.StartsWith("Move Sound"))
                ProjectManager.Project.Sounds.AnimatronicMove[index] = filePath;
            else if (currentField.StartsWith("Night"))
                ProjectManager.Project.Sounds.PhoneCalls[index] = filePath;
        }
    }
}