// SPDX-License-Identifier: CC-BY-SA-3.0

using System.Diagnostics;

namespace Holo.Media;

/// <summary>
/// High-Resolution Timer
/// </summary>
/// <author>Dmitry Romanov</author>
public class HighResolutionTimer
{
    /// <summary>
    /// Tick time length in [ms]
    /// </summary>
    public static readonly double TickLength = 1000f / Stopwatch.Frequency;

    /// <summary>
    /// Tick frequency
    /// </summary>
    public static readonly double Frequency = Stopwatch.Frequency;

    /// <summary>
    /// True if the system/operating system supports HighResolution timer
    /// </summary>
    public static bool IsHighResolution = Stopwatch.IsHighResolution;

    /// <summary>
    /// Invoked when the timer is elapsed
    /// </summary>
    public event EventHandler<HighResolutionTimerElapsedEventArgs>? Elapsed;

    /// <summary>
    /// The interval of timer ticks [ms]
    /// </summary>
    private volatile float _interval;

    /// <summary>
    /// The timer is running
    /// </summary>
    private volatile bool _isRunning;

    /// <summary>
    ///  Execution thread
    /// </summary>
    private Thread? _thread;

    private volatile bool _isMultiInterval;
    private volatile int _intervalIndex;
    private volatile long[] _intervals;

    /// <summary>
    /// Creates a timer with 1 [ms] interval
    /// </summary>
    public HighResolutionTimer()
        : this(1f) { }

    /// <summary>
    /// Creates timer with interval in [ms]
    /// </summary>
    /// <param name="interval">Interval time in [ms]</param>
    public HighResolutionTimer(float interval)
    {
        Interval = interval;
        _intervals = [];
        _isMultiInterval = false;
    }

    /// <summary>
    /// Creates timer with per-tick intervals in [ms]
    /// </summary>
    /// <remarks>
    /// When in use, the next interval will be selected each tick.
    /// It is the responsibility of the caller to prevent out-of-bounds exceptions.
    /// </remarks>
    /// <param name="intervals">Array of interval times in [ms]</param>
    public HighResolutionTimer(long[] intervals)
    {
        _intervals = intervals;
        _isMultiInterval = true;
        _intervalIndex = 0;
    }

    /// <summary>
    /// The interval of a timer in [ms]
    /// </summary>
    public float Interval
    {
        get => _interval;
        set
        {
            if (value is < 0f or float.NaN)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _interval = value;
        }
    }

    /// <summary>
    /// Set the intervals for this timer
    /// </summary>
    public long[] Intervals
    {
        get => _intervals;
        internal set
        {
            _intervals = value;
            _intervalIndex = 0;
            _isMultiInterval = true;
        }
    }

    /// <summary>
    /// Index in the array of intervals, if <see cref="IsMultiInterval"/> is enabled.
    /// </summary>
    public int IntervalIndex
    {
        get => _intervalIndex;
        set
        {
            if (!_isMultiInterval)
                return;
            if (value < 0 || value > _intervals.Length - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _intervalIndex = value;
        }
    }

    /// <summary>
    /// If this timer is using per-tick intervals
    /// </summary>
    public bool IsMultiInterval => _isMultiInterval;

    /// <summary>
    /// True when timer is running
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// If true, sets the execution thread to ThreadPriority.Highest
    /// (works after the next Start())
    /// </summary>
    /// <remarks>
    /// It might help in some cases and get things worse in others.
    /// It suggested that you do some studies if you apply
    /// </remarks>
    public bool UseHighPriorityThread { get; set; } = false;

    /// <summary>
    /// Starts the timer
    /// </summary>
    public void Start()
    {
        if (_isRunning)
            return;

        _isRunning = true;
        _thread = new Thread(ExecuteTimer) { IsBackground = true };

        if (UseHighPriorityThread)
        {
            _thread.Priority = ThreadPriority.Highest;
        }

        _thread.Start();
    }

    /// <summary>
    /// Stops the timer
    /// </summary>
    /// <remarks>
    /// This function is waiting an executing thread
    /// </remarks>
    public void Stop(bool joinThread = true)
    {
        _isRunning = false;

        // Even if _thread.Join may take time it is guaranteed that
        // Elapsed event is never called overlapped with different threads
        if (joinThread && Thread.CurrentThread != _thread)
        {
            _thread?.Join();
        }
    }

    private void ExecuteTimer()
    {
        var nextTrigger = 0f;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        while (_isRunning)
        {
            if (!_isMultiInterval)
                nextTrigger += _interval;
            else
            {
                nextTrigger += _intervals[_intervalIndex];
                Interlocked.Increment(ref _intervalIndex);
            }

            double elapsed;

            while (true)
            {
                elapsed = ElapsedHiRes(stopwatch);
                var diff = nextTrigger - elapsed;
                if (diff <= 0f)
                    break;

                switch (diff)
                {
                    case < 1f:
                        Thread.SpinWait(10);
                        break;
                    case < 5f:
                        Thread.SpinWait(100);
                        break;
                    case < 15f:
                        Thread.Sleep(1);
                        break;
                    default:
                        Thread.Sleep(10);
                        break;
                }

                if (!_isRunning)
                    return;
            }

            var delay = elapsed - nextTrigger;
            Elapsed?.Invoke(this, new HighResolutionTimerElapsedEventArgs(delay));

            if (!_isRunning)
                return;

            // restarting the timer in every hour to prevent precision problems
            if (!(stopwatch.Elapsed.TotalHours >= 1d))
                continue;
            stopwatch.Restart();
            nextTrigger = 0f;
        }

        stopwatch.Stop();
    }

    private static double ElapsedHiRes(Stopwatch stopwatch)
    {
        return stopwatch.ElapsedTicks * TickLength;
    }
}

/// <summary>
/// EventArgs for the <see cref="HighResolutionTimer"/> elapsed event
/// </summary>
public class HighResolutionTimerElapsedEventArgs : EventArgs
{
    /// <summary>Real timer delay in [ms]</summary>
    public double Delay { get; }

    internal HighResolutionTimerElapsedEventArgs(double delay)
    {
        Delay = delay;
    }
}
