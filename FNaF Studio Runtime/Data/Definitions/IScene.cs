namespace FNaFStudio_Runtime.Data.Definitions;

public interface IScene
{
    public string Name { get; }
    public SceneType Type { get; }

    void Init()
    {
    }

    public void Update()
    {
    }

    public void Draw();

    void Exit()
    {
    }
}