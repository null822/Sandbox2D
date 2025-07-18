using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Math2D;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Sandbox2D.UserInterface.Input;
using static Sandbox2D.Util;
using static Sandbox2D.Constants;

namespace Sandbox2D.Managers;

// TODO: [FINISH] implement GUIs
// TODO: migrate to OpenTK 5 once its out

public class WindowManager : NativeWindow
{
    public readonly KeybindSieve KeybindManager = new();
    public readonly RenderManager RenderManager;
    
    public readonly long Id;

    private readonly InputTimeline _inputFrame = new();
    
    private bool _isRunning;
    public bool IsShutdown { get; private set; }
    private bool _isLoaded;

    private readonly Stopwatch _frameTimer = new();
    private TimeSpan _prevFrameTime = TimeSpan.Zero;
    
    public readonly ManualResetEventSlim RenderEvent = new(false);
    public volatile bool IsRenderingDone = false;
    
    private readonly Lock _resizeStateLock = new();
    private bool _doResize;
    private Vec2<int> _newSize;
    private bool _doFramebufferResize;
    private Vec2<int> _newFramebufferSize;
    
    // stored to prevent deletion by the garbage collector (only stored as pointers in unmanaged code otherwise)
    private GLFWCallbacks.KeyCallback _keyCallback;
    private GLFWCallbacks.MouseButtonCallback _mouseButtonCallback;
    private GLFWCallbacks.CursorPosCallback _cursorPosCallback;
    private GLFWCallbacks.ScrollCallback _scrollCallback;
    
    public unsafe WindowManager(RenderManager renderManager, NativeWindowSettings settings) : base(settings)
    {
        Id = (long)WindowPtr;
        
        RenderManager = renderManager;
        renderManager.SetWindowManager(this);
        
        RegisterInputCallbacks();
    }

    private unsafe void RegisterInputCallbacks()
    {
        _keyCallback = KeyCallback;
        _mouseButtonCallback = MouseButtonCallback;
        _cursorPosCallback = CursorPosCallback;
        _scrollCallback = ScrollCallback;
        
        GLFW.SetKeyCallback(WindowPtr, _keyCallback);
        GLFW.SetMouseButtonCallback(WindowPtr, _mouseButtonCallback);
        GLFW.SetCursorPosCallback(WindowPtr, _cursorPosCallback);
        GLFW.SetScrollCallback(WindowPtr, _scrollCallback);
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
                {
                    RenderEvent.Wait();
                    RenderEvent.Reset();
                    IsRenderingDone = true;
                    break;
                }
            }

            lock (_resizeStateLock)
            {
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
            }
            
            
            // wait for the frame 
            RenderEvent.Wait();
            // begin the frame
            
            _inputFrame.SwapBuffers(); // swap input buffers, reading from the one that was previously being written to
            
            RenderManager.Update(); // update
            OnUpdateInput(); // update input
            OnRenderFrame(); // render the frame
            
            Context.SwapBuffers(); // ensure the frame is fully rendered, then display the frame
            
            RenderEvent.Reset();
            IsRenderingDone = true;
        }
        IsShutdown = true;
    }
    
    
    /// <summary>
    /// Renders to the screen.
    /// </summary>
    private void OnRenderFrame()
    {
        var time = _frameTimer.Elapsed - _prevFrameTime;
        _prevFrameTime = _frameTimer.Elapsed;
        
        RenderManager.Render(time);
    }
    
    /// <summary>
    /// Updates the controls and controls if the game should be running. Runs before <see cref="OnRenderFrame"/>.
    /// </summary>
    private void OnUpdateInput()
    {
        KeybindManager.Call(_inputFrame);
        RenderManager.UpdateControls(_inputFrame);
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
        lock (_resizeStateLock)
        {
            _doResize = true;
            _newSize = newSize;
        }
    }
    
    protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
    {
        base.OnFramebufferResize(e);
        
        var newSize = e.Size.ToVec2();
        lock (_resizeStateLock)
        {
            _doFramebufferResize = true;
            _newFramebufferSize = newSize;
        }
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
    
    private unsafe void KeyCallback(Window* window, Keys key, int scancode, InputAction action, KeyModifiers mods)
    {
        _inputFrame.AddKeyboardKey(key, scancode, action, mods);
    }
    
    private unsafe void MouseButtonCallback(Window* window, MouseButton button, InputAction action, KeyModifiers mods)
    {
        _inputFrame.AddMouseButton(button, action, mods);
    }
    
    private unsafe void CursorPosCallback(Window* window, double posX, double posY)
    {
        _inputFrame.AddMousePos(posX, posY);
    }
    
    private unsafe void ScrollCallback(Window* window, double offsetX, double offsetY)
    {
        _inputFrame.AddMouseScroll(offsetX, offsetY);
    }
    
    public void Shutdown()
    {
        _isRunning = false;
    }
}
