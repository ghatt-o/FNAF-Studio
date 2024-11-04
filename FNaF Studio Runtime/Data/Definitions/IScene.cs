namespace FNAFStudio_Runtime_RCS.Data.Definitions;

public interface IScene
{
    public string Name { get; }

    // Adding the {} makes this func optional
    void Init()
    {
    }

    public Task UpdateAsync();
    public void Draw();

    void Exit()
    {
    }
}