using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Sandbox2D.Managers;

public static class ApplicationManager
{
    private static readonly List<WindowManager> Windows = [];
    private static readonly List<bool> IsWindowRendering = [];
    
    public static bool IsShutdown { get; private set; }
    
    public static void AddWindow(RenderManager renderManager, NativeWindowSettings settings)
    {
        var window = new WindowManager(renderManager, settings);
        
        // make all windows other than the main window not wait for vsync when rendering
        if (Windows.Count != 0)
            window.Context.SwapInterval = 0;
        
        window.Context.MakeNoneCurrent(); // do not keep the context on the main thread
        Windows.Add(window);
        IsWindowRendering.Add(false);
    }
    
    public static void Run()
    {
        for (var i = 0; i < Windows.Count; i++)
        {
            var t = new Thread(Windows[i].Run)
            {
                Name = $"Window Thread #{i}"
            };
            t.Start();
        }
        
        var inputFrameTimer = new Stopwatch();
        var inputFrameTime = 0d;
        const double msPerInputFrame = 1000d / Constants.InputFramerate;
        const double inputFrameTimerResetMs = 1_000;
        
        inputFrameTimer.Start();
        while (true)
        {
            foreach (var window in Windows)
            {
                window.NewInputFrame();
            }
            
            GLFW.PollEvents();

            for (var i = 0; i < Windows.Count; i++)
            {
                var window = Windows[i];
                window.RenderSyncHandle.Reset(); // end render sync time
                window.RenderTimeHandle.Set(); // start render time
                IsWindowRendering[i] = true;
            }

            // frames get rendered
            
            while (true)
            {
                var allDone = true;
                for (var i = 0; i < Windows.Count; i++)
                {
                    if (!IsWindowRendering[i]) continue;
                    
                    var window = Windows[i];
                    var done = window.RenderSyncHandle.WaitOne(0); // check if we can enter sync time, if so, enter
                    if (done)
                    {
                        window.Context.SwapBuffers(); // display frame
                        IsWindowRendering[i] = false;
                    }
                    
                    allDone &= done;
                }

                if (inputFrameTimer.Elapsed.TotalMilliseconds - inputFrameTime > msPerInputFrame)
                {
                    GLFW.PollEvents();
                    inputFrameTime += msPerInputFrame;
                    
                    if (inputFrameTimer.Elapsed.TotalMilliseconds >= inputFrameTimerResetMs)
                    {
                        inputFrameTime = 0;
                        inputFrameTimer.Restart();
                    }
                }
                
                if (allDone)
                    break;
            }
            
            if (Windows.Any(w => w.IsShutdown))
            {
                break;
            }
        }
        
        foreach (var window in Windows)
        {
            window.Shutdown();
            window.RenderTimeHandle.Set();
        }
        
        IsShutdown = true;
        Util.Log("Application Manager Shut Down");
    }
}
