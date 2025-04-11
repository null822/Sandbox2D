using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Math2D;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Sandbox2D.UserInterface.Keybinds;
using static Sandbox2D.Util;
using static Sandbox2D.Constants;

namespace Sandbox2D.Managers;

// TODO: [FINISH] implement GUIs
// TODO: migrate to OpenTK 5 once its out

public class WindowManager : NativeWindow
{
    public readonly KeybindSieve KeybindManager = new();
    public readonly RenderManager RenderManager;
    
    private bool _isRunning;
    public bool IsShutdown { get; private set; }
    private bool _isLoaded;

    private readonly Stopwatch _frameTimer = new();
    private TimeSpan _prevFrameTime = TimeSpan.Zero;
    
    public readonly EventWaitHandle FrameWaitHandle = new ManualResetEvent(false);
    public readonly EventWaitHandle NoFrameWaitHandle = new ManualResetEvent(true);
    
    private readonly Mutex _resizeStateMutex = new();
    private bool _doResize;
    private Vec2<int> _newSize;
    private bool _doFramebufferResize;
    private Vec2<int> _newFramebufferSize;
    
    public WindowManager(RenderManager renderManager, NativeWindowSettings settings) : base(settings)
    {
        RenderManager = renderManager;
        renderManager.SetWindowManager(this);
    }

    public void Run()
    {
        IsShutdown = false;
        _isRunning = true;
        
        if (!_isLoaded)
        {
            Context.MakeCurrent();
            
            OnLoad();
            _isLoaded = true;
        }
        
        _frameTimer.Start();
        
        while (true)
        {
            if (!_isRunning)
            {
                var cancelArgs = new CancelEventArgs();
                OnClosing(cancelArgs);
                if (!cancelArgs.Cancel)
                    break;
            }

            _resizeStateMutex.WaitOne();
            if (_doResize)
            {
                OnResize(_newSize);
                _doResize = false;
            }

            if (_doFramebufferResize)
            {
                OnFramebufferResize(_newFramebufferSize);
                _doFramebufferResize = false;
            }
            _resizeStateMutex.ReleaseMutex();
            
            FrameWaitHandle.WaitOne(); // wait for Frame time
            
            
            OnUpdateInput();
            OnRenderFrame();
            
            GL.Flush(); // ensure the frame is fully rendered
            
            FrameWaitHandle.Reset(); // end Frame time
            NoFrameWaitHandle.Set(); // start NoFrame time
        }
        IsShutdown = true;
    }
    
    
    /// <summary>
    /// Renders to the screen.
    /// </summary>
    private void OnRenderFrame()
    {
        var args = new FrameEventArgs((_frameTimer.Elapsed - _prevFrameTime).TotalSeconds);
        _prevFrameTime = _frameTimer.Elapsed;
        
        RenderManager.Render(args.Time);
    }
    
    /// <summary>
    /// Updates the controls and controls if the game should be running. Runs before <see cref="OnRenderFrame"/>.
    /// </summary>
    private void OnUpdateInput()
    {
        KeybindManager.Call(MouseState, KeyboardState);
        RenderManager.UpdateControls(MouseState, KeyboardState);
    }
    
    /// <summary>
    /// Initializes the render thread.
    /// </summary>
    private void OnLoad()
    {
        if (GlDebug)
        {
            GL.DebugMessageCallback(DebugMessageDelegate, IntPtr.Zero);
            GL.Enable(EnableCap.DebugOutput);

            if (SynchronousGlDebug)
                GL.Enable(EnableCap.DebugOutputSynchronous);
        }
        
        RenderManager.Initialize();
        
        Log("===============[  BEGIN RENDER  ]===============", "Load");
    }
    
    /// <summary>
    /// Called when the window gets resized
    /// </summary>
    private void OnResize(Vec2<int> newSize)
    {
        RenderManager.OnResize(newSize);
    }
    
    /// <summary>
    /// Called when the framebuffer gets resized
    /// </summary>
    private void OnFramebufferResize(Vec2<int> newSize)
    {
        GL.Viewport(0, 0, newSize.X, newSize.Y);
    }
    
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        
        var newSize = e.Size.ToVec2();
        _resizeStateMutex.WaitOne();
        _doResize = true;
        _newSize = newSize;
        _resizeStateMutex.ReleaseMutex();
    }
    
    protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
    {
        base.OnFramebufferResize(e);
        
        var newSize = e.Size.ToVec2();
        _resizeStateMutex.WaitOne();
        _doFramebufferResize = true;
        _newFramebufferSize = newSize;
        _resizeStateMutex.ReleaseMutex();
    }
    
    /// <summary>
    /// Shuts down the game.
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        RenderManager.OnClose(e);
        KeybindManager.Dispose();
        Shutdown();
        base.OnClosing(e);
    }
    
    public void Shutdown()
    {
        _isRunning = false;
    }
}
