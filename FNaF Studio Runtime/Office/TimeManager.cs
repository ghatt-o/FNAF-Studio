using FNaFStudio_Runtime.Data;

namespace FNaFStudio_Runtime.Office;

public class TimeManager
{
    public const float TicksPerSecond = 0.4444444444444444f;
    public const float TicksPerMinute = 26.6666666666666667f;
    public const float TicksPerHour = 1600; // 80 real seconds

    private static int ticksSinceStart;
    private static int seconds;
    private static int minutes;
    private static int hours;
    private static readonly List<Action> timeCallbacks = [];
    private static readonly ReaderWriterLockSlim rwLock = new();
    private static bool started;

    private static readonly SemaphoreSlim instanceSemaphore = new(1, 1);
    private static readonly bool instanceExists = false;

    static TimeManager()
    {
        instanceSemaphore.Wait();
        try
        {
            if (instanceExists)
            {
                throw new InvalidOperationException("Only one instance of TimeManager is allowed.");
            }

            instanceExists = true;
        }
        finally
        {
            instanceSemaphore.Release();
        }
    }

    public static void Start()
    {
        if (!started)
        {
            started = true;

            // Register callback to update time on every tick in TickManager
            GameState.Clock.OnTick(() =>
            {
                rwLock.EnterWriteLock();
                try
                {
                    ticksSinceStart++;
                    seconds = (int)(ticksSinceStart / TicksPerSecond % 60);
                    minutes = (int)(ticksSinceStart / TicksPerMinute % 60);
                    hours = (int)(ticksSinceStart / TicksPerHour % 24);
                }
                finally
                {
                    rwLock.ExitWriteLock();
                }

                TriggerTimeCallbacks();
            });
        }
    }

    public static void Stop()
    {
        rwLock.EnterWriteLock();
        try
        {
            started = false;
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    public static void Reset()
    {
        rwLock.EnterWriteLock();
        try
        {
            ticksSinceStart = 0;
            hours = 0;
            minutes = 0;
            seconds = 0;
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    public static (int hours, int minutes, int seconds) GetTime()
    {
        rwLock.EnterReadLock();
        try
        {
            return (hours, minutes, seconds);
        }
        finally
        {
            rwLock.ExitReadLock();
        }
    }

    public static void SetTime(int newHours, int newMinutes, int newSeconds)
    {
        rwLock.EnterWriteLock();
        try
        {
            hours = newHours % 24;
            minutes = newMinutes % 60;
            seconds = newSeconds % 60;
            ticksSinceStart = (int)(hours * TicksPerHour + minutes * TicksPerMinute + seconds * TicksPerSecond);
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    public static void OnTimeUpdate(Action callback)
    {
        rwLock.EnterWriteLock();
        try
        {
            timeCallbacks.Add(callback);
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    private static void TriggerTimeCallbacks()
    {
        List<Action> callbacksCopy;
        rwLock.EnterReadLock();
        try
        {
            callbacksCopy = new List<Action>(timeCallbacks);
        }
        finally
        {
            rwLock.ExitReadLock();
        }

        foreach (var callback in callbacksCopy) callback();
    }
}
