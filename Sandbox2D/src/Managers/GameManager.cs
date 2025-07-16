using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Sandbox2D.Managers;

public abstract class GameManager(double tps, RenderManager[] renderManagers)
{
    public bool IsRunning { private set; get; }

    protected readonly RenderManager[] RenderManagers = renderManagers;
    
    /// <summary>
    /// Whether the game is running. False if the game has shut down
    /// </summary>
    private bool _isClosing;
    /// <summary>
    /// A <see cref="Stopwatch"/> to keep track of how long the current tick has taken so far
    /// </summary>
    private readonly Stopwatch _tickTimer = new();
    /// <summary>
    /// The time that the previous tick took to complete
    /// </summary>
    private TimeSpan _prevTickTime;
    /// <summary>
    /// The maximum time that each tick should take to complete
    /// </summary>
    private readonly TimeSpan _targetTickTime = TimeSpan.FromMilliseconds(1000.0 / tps);


    protected abstract void Tick();
    protected abstract void Initialize();

    public virtual void OnClose() { }
    
    
    public void Run()
    {
        // initialize the logic
        Initialize();
        
        // while the game is running
        while (!_isClosing)
        {
            // restart the stopwatch
            _tickTimer.Restart();
            
            if (IsRunning)
            {
                Tick();
            }
            _prevTickTime = _tickTimer.Elapsed;
            
            var sleepTime = _targetTickTime.Subtract(_prevTickTime);
            
            // sleep for the remaining time in the tick
            if (sleepTime.Ticks > 0)
            {
                _tickTimer.Stop();
                Thread.Sleep(sleepTime);
            }
        }
        
        // signal that the thread has shut down
        _isClosing = false;
        
        OnClose();
        
        Util.Log("Game Logic Thread Shut Down");
    }
    
    /// <summary>
    /// Sets whether the game is running.
    /// </summary>
    public void SetRunning(bool value)
    {
        IsRunning = value;
    }
    
    /// <summary>
    /// Shuts down the Logic Thread and returns once it is shut down.
    /// </summary>
    public void Shutdown()
    {
        _isClosing = true;
        
        while (_isClosing)
        {
            Thread.Sleep(10);
        }
    }
    
    /// <summary>
    /// Gets how long the current tick has taken so far.
    /// </summary>
    public TimeSpan GetCurrentTickTime()
    {
        return _tickTimer.Elapsed;
    }
    
    /// <summary>
    /// Gets how long the previous tick has taken.
    /// </summary>
    public TimeSpan GetPreviousTickTime()
    {
        return _prevTickTime;
    }
    
    protected void CallRenderManagers<Manager>(IList<RenderManagerCall<Manager>> calls)
        where Manager : RenderManager
    {
        foreach (var r in RenderManagers)
        {
            if (r is not Manager renderManager) continue;
            
            var managerCalls = calls
                .Where(c => c.ManagerId == renderManager.Id);

            foreach (var call in managerCalls)
            {
                call.Call.Invoke(renderManager);
            }
        }
    }
    
    
    protected List<ManagerResponse<Result>> CallRenderManagers<Manager, Result>(IList<RenderManagerCall<Manager, Result>> calls)
        where Manager : RenderManager
    {
        var results = new List<ManagerResponse<Result>>();
        foreach (var r in RenderManagers)
        {
            if (r is not Manager renderManager) continue;
            
            var managerCalls = calls
                .Where(c => c.ManagerId == renderManager.Id);
            
            results.AddRange(managerCalls.Select(call =>
                new ManagerResponse<Result>(renderManager.Id, call.Call.Invoke(renderManager))));
        }

        return results;
    }

    protected List<ManagerResponse<Result>> CallRenderManagers<Manager, Result>(Func<Manager, Result> call)
        where Manager : RenderManager
    {
        var results = new List<ManagerResponse<Result>>();
        foreach (var r in RenderManagers)
        {
            if (r is not Manager renderManager) continue;
            
            results.Add(new ManagerResponse<Result>(renderManager.Id, call.Invoke(renderManager)));
        }

        return results;
    }
    
    protected void CallRenderManagers<Manager, Result>(Action<Manager> call)
        where Manager : RenderManager
    {
        foreach (var r in RenderManagers)
        {
            if (r is not Manager renderManager) continue;
            
            call.Invoke(renderManager);
        }

    }
}

public record ManagerResponse<T>(long ManagerId, T Response);

public record RenderManagerCall<Manager, Result>(long ManagerId, Func<Manager, Result> Call)
    where Manager : RenderManager;
public record RenderManagerCall<Manager>(long ManagerId, Action<Manager> Call)
    where Manager : RenderManager;
    