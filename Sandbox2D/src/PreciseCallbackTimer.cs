using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Sandbox2D;

public class PreciseCallbackTimer
{
    private Task? _task;
    
    private readonly Lock _stateLock = new();
    private event Action? OnCallback;
    private TimeSpan _interval;
    private bool _isRunning;
    
    public PreciseCallbackTimer(Action callback, TimeSpan interval)
    {
        Callback += callback;
        _interval = interval;
    }
    
    public bool IsRunning
    {
        get => GetIsRunning();
        set => SetIsRunning(value);
    }
    
    private bool GetIsRunning()
    {
        bool isRunning;
        lock (_stateLock)
        {
            isRunning = _isRunning;
        }
        return isRunning;
    }
    
    private void SetIsRunning(bool isRunning)
    {
        if (isRunning) Start();
        else Stop();
    }
    
    public void Start()
    {
        lock (_stateLock)
        {
            _isRunning = true;
            _task = PrecisionRepeatActionOnIntervalAsync();
        }
    }
    
    public void Stop()
    {
        lock (_stateLock)
        {
            _isRunning = false;
            _task = null;
        }
    }
    
    public TimeSpan Interval
    {
        get => GetInterval();
        set => SetInterval(value);
    }

    private TimeSpan GetInterval()
    {
        TimeSpan interval;
        lock (_stateLock)
        {
            interval = _interval;
        }
        return interval;
    }
    
    private void SetInterval(TimeSpan interval)
    {
        lock (_stateLock)
        {
            _interval = interval;
        }
    }
    
    public event Action Callback
    {
        add => AddCallback(value);
        remove => RemoveCallback(value);
    }

    private void AddCallback(Action callback)
    {
        lock (_stateLock)
        {
            OnCallback += callback;
        }
    }
    
    private void RemoveCallback(Action callback)
    {
        lock (_stateLock)
        {
            OnCallback -= callback;
        }
    }
    
    // modified version of https://github.com/SunsetQuest/Precision-Repeat-Action-On-Interval-Async-Method/blob/master/Program.cs
    public async Task PrecisionRepeatActionOnIntervalAsync()
    {
        const long stage1Delay = 20L;
        const long stage2Delay = 5 * TimeSpan.TicksPerMillisecond;

        var stopwatch = new Stopwatch();
        
        var target = TimeSpan.FromMilliseconds(stage1Delay + 2);
        
        stopwatch.Start();
        while (true)
        {
            lock (_stateLock)
            {
                if (!_isRunning)
                    break;
            }
            
            // Task.Delay
            var timeLeft = target - stopwatch.Elapsed;
            if (timeLeft.TotalMilliseconds >= stage1Delay)
            {
                await Task.Delay((int)(timeLeft.TotalMilliseconds - stage1Delay));
            }
            
            // Task.Yield
            while (stopwatch.Elapsed < target - new TimeSpan(stage2Delay))
            {
                await Task.Yield();
            }
            
            // Thread.SpinWait
            while (stopwatch.Elapsed < target)
            {
                Thread.SpinWait(64);
            }


            OnCallback?.Invoke();
            target += _interval;
        }
        stopwatch.Stop();
    }
}
