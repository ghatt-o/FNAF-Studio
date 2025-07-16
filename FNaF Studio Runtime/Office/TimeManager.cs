using FNaFStudio_Runtime.Data;

namespace FNaFStudio_Runtime.Office;

public abstract class TimeManager
{
    private const float TicksPerSecond = 0.4444444444444444f;
    private const float TicksPerMinute = 26.6666666666666667f;
    private const float TicksPerHour = 1600; // 80 real seconds

    private static int _ticksSinceStart;
    private static int _seconds;
    private static int _minutes;
    private static int _hours;
    private static readonly List<Action> TimeCallbacks = [];
    private static readonly ReaderWriterLockSlim RwLock = new();
    private static bool _started;

    private static readonly SemaphoreSlim InstanceSemaphore = new(1, 1);
    private static readonly bool InstanceExists;

    static TimeManager()
    {
        InstanceSemaphore.Wait();
        try
        {
            if (InstanceExists)
            {
                throw new InvalidOperationException("Only one instance of TimeManager is allowed.");
            }

            InstanceExists = true;
        }
        finally
        {
            InstanceSemaphore.Release();
        }
    }

    public static void Start()
    {
        if (_started) return;
        _started = true;

        // Register callback to update time on every tick in TickManager
        GameState.Clock.OnTick(() =>
        {
            RwLock.EnterWriteLock();
            try
            {
                _ticksSinceStart++;
                _seconds = (int)(_ticksSinceStart / TicksPerSecond % 60);
                _minutes = (int)(_ticksSinceStart / TicksPerMinute % 60);
                _hours = (int)(_ticksSinceStart / TicksPerHour % 24);
            }
            finally
            {
                RwLock.ExitWriteLock();
            }

            TriggerTimeCallbacks();
        });
    }

    public static void Stop()
    {
        RwLock.EnterWriteLock();
        try
        {
            _started = false;
        }
        finally
        {
            RwLock.ExitWriteLock();
        }
    }

    public static void Reset()
    {
        RwLock.EnterWriteLock();
        try
        {
            _ticksSinceStart = 0;
            _hours = 0;
            _minutes = 0;
            _seconds = 0;
        }
        finally
        {
            RwLock.ExitWriteLock();
        }
    }

    public static (int hours, int minutes, int seconds) GetTime()
    {
        RwLock.EnterReadLock();
        try
        {
            return (_hours, _minutes, _seconds);
        }
        finally
        {
            RwLock.ExitReadLock();
        }
    }

    public static void SetTime(int newHours, int newMinutes, int newSeconds)
    {
        RwLock.EnterWriteLock();
        try
        {
            _hours = newHours % 24;
            _minutes = newMinutes % 60;
            _seconds = newSeconds % 60;
            _ticksSinceStart = (int)(_hours * TicksPerHour + _minutes * TicksPerMinute + _seconds * TicksPerSecond);
        }
        finally
        {
            RwLock.ExitWriteLock();
        }
    }

    public static void OnTimeUpdate(Action callback)
    {
        RwLock.EnterWriteLock();
        try
        {
            TimeCallbacks.Add(callback);
        }
        finally
        {
            RwLock.ExitWriteLock();
        }
    }

    private static void TriggerTimeCallbacks()
    {
        List<Action> callbacksCopy;
        RwLock.EnterReadLock();
        try
        {
            callbacksCopy = new List<Action>(TimeCallbacks);
        }
        finally
        {
            RwLock.ExitReadLock();
        }

        foreach (var callback in callbacksCopy) callback();
    }
}
