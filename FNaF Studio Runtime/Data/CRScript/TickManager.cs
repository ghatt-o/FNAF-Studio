using FNAFStudio_Runtime_RCS.Util;

namespace FNAFStudio_Runtime_RCS.Data.CRScript;

public class TickManager
{
    private readonly List<Action> callbacks = [];
    private readonly Dictionary<int, List<Action>> intervalCallbacks = [];
    private readonly object lockObject = new();
    private int currentTick;
    private bool started;
    private CancellationTokenSource stopSignal;

    public TickManager()
    {
        stopSignal = new CancellationTokenSource();
    }

    public void Reset()
    {
        lock (lockObject)
        {
            currentTick = 0;
        }
    }

    public int GetCurrentTick()
    {
        lock (lockObject)
        {
            return currentTick;
        }
    }

    public void Start()
    {
        if (!started)
        {
            callbacks.Clear();
            intervalCallbacks.Clear();
            started = true;
            OnTick(() =>
            {
                EventManager.TriggerEvent("on_game_loop", []);
                EventManager.TriggerEvent("current_tick_equals", [GetCurrentTick().ToString()]);
            });

            var token = stopSignal.Token;
            Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        lock (lockObject)
                        {
                            currentTick++;
                        }

                        TriggerCallbacks();
                        TriggerIntervalCallbacks();

                        await Task.Delay(50, token);
                        // 50ms = 1 tick;
                    }
                }
                catch (OperationCanceledException)
                {
                    await Logger.LogAsync("TickManager", "Thread stopped");
                }
                catch (Exception ex)
                {
                    await Logger.LogErrorAsync("TickManager", $"Error in Start: {ex.Message}");
                }
            }, token);
        }
    }

    public void Stop()
    {
        started = false;
        stopSignal.Cancel();
        stopSignal.Dispose();
    }

    public void Restart()
    {
        Stop();
        Reset();
        stopSignal = new CancellationTokenSource();
        Start();
    }

    public void OnTick(Action callback)
    {
        lock (lockObject)
        {
            callbacks.Add(callback);
        }
    }

    public void OnEveryNumTicks(int interval, Action callback)
    {
        lock (lockObject)
        {
            if (!intervalCallbacks.ContainsKey(interval))
                intervalCallbacks[interval] = [];

            intervalCallbacks[interval].Add(callback);
        }
    }

    private void TriggerCallbacks()
    {
        List<Action> callbacksCopy;
        lock (lockObject)
        {
            callbacksCopy = new List<Action>(callbacks);
        }

        foreach (var callback in callbacksCopy) callback();
    }

    private void TriggerIntervalCallbacks()
    {
        Dictionary<int, List<Action>> intervalCallbacksCopy;
        lock (lockObject)
        {
            intervalCallbacksCopy = new Dictionary<int, List<Action>>(intervalCallbacks);
        }

        foreach (var (interval, actions) in intervalCallbacksCopy)
            if (currentTick % interval == 0)
                foreach (var action in actions)
                    action();
    }
}