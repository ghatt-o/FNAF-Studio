using System.Numerics;

namespace FNAFStudio_Runtime_RCS.Data.Definitions.GameObjects;

public enum AnimationState
{
    Normal,
    Reverse
}

public class RevAnimation(string path, bool loop = true)
{
    private readonly BaseAnimation normal = new(path, false, loop);
    private readonly BaseAnimation reverse = new(path, true, loop);
    public AnimationState State { get; private set; } = AnimationState.Normal;

    public void Draw(Vector2 position)
    {
        Current().Draw(position);
    }

    public void AdvanceDraw(Vector2 position)
    {
        Advance();
        Current().Draw(position);
    }

    public BaseAnimation Current()
    {
        return State == AnimationState.Normal ? normal : reverse;
    }

    public void Advance()
    {
        Current().Update();
    }

    public void SetState(AnimationState state)
    {
        if (State != state)
        {
            State = state;
            Current().Reset();
        }
    }

    public void Reverse()
    {
        SetState(State == AnimationState.Normal ? AnimationState.Reverse : AnimationState.Normal);
    }

    public void End()
    {
        Current().End();
    }
}