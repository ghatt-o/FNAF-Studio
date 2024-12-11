using FNAFStudio_Runtime_RCS.Util;

namespace FNAFStudio_Runtime_RCS.Data.CRScript;

public class TickManager
{
    private readonly List<Action> callbacks = [];
    private readonly Dictionary<int, List<Action>> intervalCallbacks = [];
    private readonly SemaphoreSlim semaphore = new(1, 1); // Replaces the lockObject
    private int currentTick;
    private bool started;
    private CancellationTokenSource stopSignal;

    private static readonly SemaphoreSlim instanceSemaphore = new(1, 1);
    private static bool instanceExists = false;

    public TickManager()
    {
        instanceSemaphore.Wait();
        try
        {
            if (instanceExists)
            {
                throw new InvalidOperationException("Only one instance of TickManager is allowed.");
            }

            instanceExists = true;
        }
        finally
        {
            instanceSemaphore.Release();
        }

        stopSignal = new CancellationTokenSource();
    }

    public void Reset()
    {
        semaphore.Wait();
        try
        {
            currentTick = 0;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public int GetCurrentTick()
    {
        semaphore.Wait();
        try
        {
            return currentTick;
        }
        finally
        {
            semaphore.Release();
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
            Task.Run(() =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        semaphore.Wait();
                        try
                        {
                            currentTick++;
                        }
                        finally
                        {
                            semaphore.Release();
                        }

                        TriggerCallbacks();
                        TriggerIntervalCallbacks();

                        Task.Delay(50, token).Wait();
                        // 50ms = 1 tick;
                    }
                }
                catch (OperationCanceledException)
                {
                    Logger.LogAsync("TickManager", "Thread stopped");
                }
                catch (Exception ex)
                {
                    Logger.LogErrorAsync("TickManager", $"Error in Start: {ex.Message}");
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
        semaphore.Wait();
        try
        {
            callbacks.Add(callback);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public void OnEveryNumTicks(int interval, Action callback)
    {
        semaphore.Wait();
        try
        {
            if (!intervalCallbacks.ContainsKey(interval))
                intervalCallbacks[interval] = [];

            intervalCallbacks[interval].Add(callback);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private void TriggerCallbacks()
    {
        List<Action> callbacksCopy;
        semaphore.Wait();
        try
        {
            callbacksCopy = new List<Action>(callbacks);
        }
        finally
        {
            semaphore.Release();
        }

        foreach (var callback in callbacksCopy) callback();
    }

    private void TriggerIntervalCallbacks()
    {
        Dictionary<int, List<Action>> intervalCallbacksCopy;
        semaphore.Wait();
        try
        {
            intervalCallbacksCopy = new Dictionary<int, List<Action>>(intervalCallbacks);
        }
        finally
        {
            semaphore.Release();
        }

        foreach (var (interval, actions) in intervalCallbacksCopy)
            if (currentTick % interval == 0)
                foreach (var action in actions)
                    action();
    }
}
