using System.Numerics;

namespace FNaFStudio_Runtime.Data.Definitions.GameObjects;

public enum AnimationState
{
    Normal,
    Reverse
}

public class RevAnimation(string path, bool loop = true)
{
    private BaseAnimation normal = new(path, false, loop);
    private BaseAnimation reverse = new(path, true, loop);

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

    public void OnFinish(Action onFinishAction)
    {
        normal.OnFinish(onFinishAction);
        reverse.OnFinish(onFinishAction);
    }

    public void OnPlay(Action onPlayAction)
    {
        normal.OnPlay(onPlayAction);
        reverse.OnPlay(onPlayAction);
    }

    public void OnFinish(Action onFinishAction, AnimationState state)
    {
        (state == AnimationState.Normal ? normal : reverse).OnFinish(onFinishAction);
    }

    public void OnPlay(Action onPlayAction, AnimationState state)
    {
        (state == AnimationState.Normal ? normal : reverse).OnPlay(onPlayAction);
    }

    public void Pause()
    {
        normal.Pause();
        reverse.Pause();
    }

    public void Resume()
    {
        normal.Resume();
        reverse.Resume();
    }

    public void Hide()
    {
        normal.Hide();
        reverse.Hide();
    }

    public void Show()
    {
        normal.Show();
        reverse.Show();
    }

    public void Reset()
    {
        normal = new(path, false, loop);
        reverse = new(path, true, loop);
    }
}