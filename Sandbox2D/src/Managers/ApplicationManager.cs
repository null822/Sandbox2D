using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenTK.Windowing.Desktop;

namespace Sandbox2D.Managers;

public static class ApplicationManager
{
    private static readonly List<WindowManager> Windows = [];
    
    public static bool IsShutdown { get; private set; }
    
    public static void AddWindow(RenderManager renderManager, NativeWindowSettings settings)
    {
        var window = new WindowManager(renderManager, settings);
        
        // make all windows other than the main window not wait for vsync when rendering
        if (Windows.Count != 0)
            window.Context.SwapInterval = 0;
        
        window.Context.MakeNoneCurrent(); // do not keep the context on the main thread
        Windows.Add(window);
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
        
        while (true)
        {
            foreach (var window in Windows)
            {
                window.NewInputFrame();
            }
            
            NativeWindow.ProcessWindowEvents(false);
            
            foreach (var window in Windows)
            {
                window.NoFrameWaitHandle.Reset(); // end NoFrame time
                window.FrameWaitHandle.Set(); // start Frame time
            }
            
            // frames get rendered
            
            foreach (var window in Windows)
            {
                window.NoFrameWaitHandle.WaitOne(); // wait for NoFrame time
                window.Context.SwapBuffers(); // display frame
            }
            
            if (Windows.Any(w => w.IsShutdown))
            {
                break;
            }
        }
        
        foreach (var window in Windows)
        {
            window.Shutdown();
            window.FrameWaitHandle.Set();
        }
        
        IsShutdown = true;
        Util.Log("Application Manager Shut Down");
    }
}
