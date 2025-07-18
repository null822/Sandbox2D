using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Sandbox2D.Managers;

public static class ApplicationManager
{
    private static readonly List<WindowManager> Windows = [];
    private static WindowManager[] _windows = [];

    private static readonly ManualResetEventSlim InputEvent = new(false);
    private static readonly PreciseCallbackTimer InputPollTimer = new(
        InputEvent.Set,
        TimeSpan.FromMilliseconds(1000d / Constants.AdditionalInputFrequency));
    
    private static readonly Lock StatisticsLock = new();
    private static double _framerate = Constants.AdditionalInputFrequency;
    private static double _inputrate = Constants.AdditionalInputFrequency;
    
    public static void Run()
    {
        for (var i = 0; i < _windows.Length; i++)
        {
            var t = new Thread(_windows[i].Run)
            {
                Name = $"Window Thread #{i}"
            };
            t.Start();
        }
        
        var frameCount = 0;
        var inputCount = 0;

        var framerate = (double)Constants.AdditionalInputFrequency;
        
        var frameBatchTimer = new Stopwatch();
        
        frameBatchTimer.Start();
        InputPollTimer.Start();
        while (true)
        {
            foreach (var window in _windows)
            {
                window.NewInputFrame();
            }
            
            GLFW.PollEvents();

            foreach (var windowManager in _windows)
            {
                windowManager.IsRenderingDone = false;
                windowManager.RenderEvent.Set();
            }
            
            // frames get rendered
            
            while (true)
            {
                var allDone = true;
                
                foreach (var window in _windows)
                {
                    allDone &= window.IsRenderingDone;
                }
                
                // if all windows are rendered, start rendering the next frame
                if (allDone) break;
                
                if (framerate < Constants.AdditionalInputFrequency)
                {
                    InputEvent.Wait();
                    InputEvent.Reset();
                    GLFW.PollEvents();
                    inputCount++;
                }
            }
            
            // frame statistics
            var elapsed = frameBatchTimer.Elapsed.TotalMilliseconds;
            if (frameBatchTimer.Elapsed.TotalMilliseconds >= Constants.FrameStatisticsBatchTime)
            {
                lock (StatisticsLock)
                {
                    _framerate = framerate = frameCount / elapsed * 1000;
                    _inputrate = (inputCount + frameCount) / elapsed * 1000;
                }
                
                frameCount = 0;
                inputCount = 0;
                frameBatchTimer.Restart();
            }
            frameCount++;
            
            for (var i = 0; i < _windows.Length; i++)
            {
                var window = _windows[i];
                if (window.IsShutdown)
                {
                    window.RenderEvent.Set();
                    RemoveWindow(i);
                }
            }
            
            if (_windows.Length == 0) break; // exit if all windows are shutdown
        }
        
        Util.Log("Application Manager Shut Down");
    }
    
    public static void AddWindow(RenderManager renderManager, NativeWindowSettings settings)
    {
        var window = new WindowManager(renderManager, settings);
        
        // make all windows other than the main window not wait for vsync when rendering
        if (Windows.Count != 0)
            window.Context.SwapInterval = 0;
        
        window.Context.MakeNoneCurrent(); // do not keep the context on the main thread
        Windows.Add(window);

        _windows = Windows.ToArray(); // convert windows to array for faster access
    }

    private static void RemoveWindow(int i)
    {
        Windows.RemoveAt(i);
        
        _windows = Windows.ToArray(); // convert windows to array for faster access
    }
    
    public static double Framerate => GetFramerate();
    public static double Inputrate => GetInputrate();
    
    private static double GetFramerate()
    {
        double framerate;
        lock (StatisticsLock)
        {
            framerate = _framerate;
        }
        return framerate;
    }
    
    private static double GetInputrate()
    {
        double inputrate;
        lock (StatisticsLock)
        {
            inputrate = _inputrate;
        }
        return inputrate;
    }
}
