using Editor.IO;
using Editor.Views;
using ImGuiNET;

namespace Editor.Controls;

public interface IContent
{
    void Initialize()
    {
    }

    void Render();
}

public class ContentView
{
    public Dictionary<string, IContent> ContentDictionary;
    private string currentContentKey;

    public ContentView()
    {
        ContentDictionary = [];
        currentContentKey = "default";
    }

    public void RegisterContent(string key, IContent content)
    {
        ContentDictionary.TryAdd(key, content);
    }

    public void UpdateContent(string newContentKey)
    {
        if (ContentDictionary.TryGetValue(newContentKey, out var value))
        {
            value.Initialize();
            currentContentKey = newContentKey;
        }
        else
        {
            currentContentKey = "default";
        }
    }

    public void Render()
    {
        if (ContentDictionary.TryGetValue(currentContentKey, out var value))
            value.Render();
        else
            ImGui.Text("Could not render ContentView.");
    }

    public static implicit operator ContentView(GameJson.GameInfo v)
    {
        throw new NotImplementedException();
    }

    public static implicit operator ContentView(ProjectInfoView v)
    {
        throw new NotImplementedException();
    }
}