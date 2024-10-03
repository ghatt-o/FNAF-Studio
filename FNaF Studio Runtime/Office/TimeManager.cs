using FNAFStudio_Runtime_RCS.Data;

namespace FNAFStudio_Runtime_RCS.Office
{
    public class TimeManager
    {
        public const float TicksPerSecond = 0.4444444444444444f;
        public const float TicksPerMinute = 26.6666666666666667f;
        public const float TicksPerHour = 1600; // 80 real seconds

        private static int currentTicks;
        private static int seconds;
        private static int minutes;
        private static int hours;
        private static readonly List<Action> timeCallbacks = [];
        private static readonly object lockObject = new();
        private static bool started = false;

        public static void Start()
        {
            if (!started)
            {
                started = true;

                // Register callback to update time on every tick in TickManager
                GameState.Clock.OnTick(() =>
                {
                    lock (lockObject)
                    {
                        currentTicks = GameState.Clock.GetCurrentTick();
                        seconds = (int)(currentTicks / TicksPerSecond % 60);
                        minutes = (int)(currentTicks / TicksPerMinute % 60);
                        hours = (int)(currentTicks / TicksPerHour % 24);
                    }

                    TriggerTimeCallbacks();
                });
            }
        }

        public static void Stop()
        {
            started = false;
        }

        public static void Reset()
        {
            lock (lockObject)
            {
                currentTicks = 0;
                hours = 0;
                minutes = 0;
                seconds = 0;
            }
        }

        public static (int hours, int minutes, int seconds) GetTime()
        {
            lock (lockObject)
            {
                return (hours, minutes, seconds);
            }
        }

        public static void SetHours(int FuncHours)
        {
            lock (lockObject) // not sure this is needed?
            {
                Reset();
                hours = FuncHours;
                currentTicks = (int)(TicksPerHour * FuncHours);
            }
        }

        public static void OnTimeUpdate(Action callback)
        {
            lock (lockObject)
            {
                timeCallbacks.Add(callback);
            }
        }

        private static void TriggerTimeCallbacks()
        {
            List<Action> callbacksCopy;
            lock (lockObject)
            {
                callbacksCopy = new List<Action>(timeCallbacks);
            }

            foreach (var callback in callbacksCopy)
            {
                callback();
            }
        }
    }
}