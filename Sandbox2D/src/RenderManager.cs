using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Math2D;
using Math2D.Quadtree;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Sandbox2D.Graphics.Renderables;
using Sandbox2D.Registry;
using Sandbox2D.UserInterface;
using Sandbox2D.UserInterface.Elements;
using Sandbox2D.UserInterface.Keybinds;
using Sandbox2D.World;
using Sandbox2D.World.Tiles;
using static Sandbox2D.Util;
using static Sandbox2D.Constants;
using static Sandbox2D.UserInterface.Keybinds.KeybindKeyType;

namespace Sandbox2D;

public class RenderManager(int width, int height, string title) : GameWindow(GameWindowSettings.Default,
    new NativeWindowSettings { ClientSize = (width, height), Title = title, Flags = ContextFlags.Debug})
{
    // rendering
    private static QuadtreeRenderable _rQt;
    private static FontRenderable _rFont;
    private static bool _unuploadedGeometry;
    private static int _gpuWorldHeight = GameManager.WorldHeight;
    
    private static readonly Random Random = new();
    // world geometry
    public static readonly ManualResetEventSlim GeometryLock = new (true);
    private static long _treeIndex;
    private static long _dataIndex;
    private static DynamicArray<ArrayModification<QuadtreeNode>> _treeModifications = new(storeOccupied: false);
    private static DynamicArray<ArrayModification<Tile>> _dataModifications = new(storeOccupied: false);
    private static long _treeLength;
    private static long _dataLength;
    private static QuadtreeNode _renderRoot;
    private static Range2D _renderRange;
    
    // world editing
    private static Tile _activeBrush = new Air();
    
    private static (WorldAction action, string arg)? _worldAction;
    private static readonly List<WorldModification> WorldModifications = [];
    
    private static float _mspt;
    
    // controls
    private readonly KeybindSieve _keybindManager = new();

    /// <summary>
    /// The current screen coordinates of the mouse
    /// </summary>
    private Vec2<float> _mouseScreenCoords;
    /// <summary>
    /// The current world coordinates of the mouse
    /// </summary>
    private Vec2<long> _mouseWorldCoords;
    
    /// <summary>
    /// The world coordinates of the mouse if they're captured by pressing <see cref="Key.LeftShift"/>, or null if they aren't
    /// </summary>
    private Vec2<long>? _capturedMouseWorldCoordsLShift;
    /// <summary>
    /// The world coordinates of the mouse if they're captured by pressing <see cref="Key.MiddleMouse"/>, or null if they aren't
    /// </summary>
    private Vec2<float>? _capturedMouseScreenCoordsMMouse;
    
    /// <summary>
    /// The  translation if it's captured by pressing <see cref="Key.MiddleMouse"/>, or null if it isn't
    /// </summary>
    private static Vec2<decimal>? _capturedTranslationMMouse;

    private static decimal _scaleMinimum;
    private const decimal ScaleMaximum = 32m;

    private static decimal _scale = 1;
    private static Vec2<decimal> _translation;
    private static float _scrollPos;
    
    public static decimal Scale
    {
        get => _scale;
        private set
        {
            _scale = value;
            _scrollPos = (float)-Math.Log((double)(_scale / 1024), 1.1);
        }
    }
    public static ref Vec2<decimal> Translation => ref _translation;
    
    /// <summary>
    /// Renders to the screen. Runs after <see cref="OnUpdateFrame"/> has completed.
    /// </summary>
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        // re-create the quadtree renderable if the world height changed
        var newGpuWorldHeight = Math.Min(GameManager.WorldHeight, 16);
        if (_gpuWorldHeight != newGpuWorldHeight)
        {
            _scaleMinimum = 400m / BitUtil.Pow2(GameManager.WorldHeight);
            
            _gpuWorldHeight = newGpuWorldHeight;
            _rQt.ResetGeometry();
            _rQt.SetMaxHeight(_gpuWorldHeight);
        }
        
        // only render when the game is running
        if (!GameManager.IsRunning)
            return;
        
        // clear the color buffer
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        /* TODO: fix rare crash:
            Unhandled exception. System.NullReferenceException: Object reference not set to an instance of an object.
            at Sandbox2D.Graphics.Renderables.QuadtreeRenderable.SetGeometry(DynamicArray`1& tree, DynamicArray`1& data, Int64 treeLength, Int64 dataLength, Int64& treeIndex, Int64& dataIndex, QuadtreeNode renderRoot) in C:\Code\C#\Sandbox2D\Sandbox2D\src\Graphics\Renderables\QuadtreeRenderable.cs:line 200
            at Sandbox2D.RenderManager.OnRenderFrame(FrameEventArgs args) in C:\Code\C#\Sandbox2D\Sandbox2D\src\RenderManager.cs:line 114
            at OpenTK.Windowing.Desktop.GameWindow.Run()
            at Sandbox2D.Program.Main(String[] args) in C:\Code\C#\Sandbox2D\Sandbox2D\src\Program.cs:line 53
        */
        
        // update world geometry / transform
        if (GeometryLock.IsSet || GeometryLock.Wait(RenderLockTimeout))
        {
            GeometryLock.Reset();
            
            // update geometry
            _rQt.SetGeometry(ref _treeModifications, ref _dataModifications, _treeLength, _dataLength, ref _treeIndex, ref _dataIndex, _renderRoot);
            
            // if we have uploaded all modifications to the gpu
            if (_treeIndex >= _treeModifications.Length && _dataIndex >= _dataModifications.Length)
            {
                if (_treeModifications.Length != 0 && _dataModifications.Length != 0)
                {
                    _treeIndex = 0;
                    _dataIndex = 0;
                    _treeModifications.Clear();
                    _dataModifications.Clear();
                }
                
                // reset geometry flag
                _unuploadedGeometry = false;
            }
            
            // calculate the scale/translation to be uploaded to the GPU
            var renderTranslation = (Vec2<float>)(Translation - (Vec2<decimal>)_renderRange.Center);
            var renderScale = (float)_scale;
            
            // update the world transform
            _rQt.SetTransform(renderTranslation, renderScale);
            
            GeometryLock.Set();
        }
        
        // render the world
        _rQt.Render();
        
        // TODO: render brush outline
        
        
        // render the GUIs
        Guis.RenderVisible();
        
        // TODO: make this neater
        
        var center = GameManager.ScreenSize / 2;
        
        // FPS display
        _rFont.SetText($"{1 / args.Time:F1} FPS, {_mspt:F1} MSPT", -center + (0,10), 1f, false);
        
        _rFont.UpdateVao();
        _rFont.Render();
        _rFont.ResetGeometry();
        
        // Mouse Coordinate Display
        _rFont.SetText($"M:({_mouseWorldCoords.X:F0}, {_mouseWorldCoords.Y:F0}) T:({_translation.X:F4}, {_translation.Y:F4}) S:{_scale:F8}", -center + (0,30), 1f, false);
        
        _rFont.UpdateVao();
        _rFont.Render();
        _rFont.ResetGeometry();
        
        
        // swap the frame buffers
        SwapBuffers();
    }
    
    /// <summary>
    /// Handles all the controls. Runs before <see cref="OnRenderFrame"/>.
    /// </summary>
    private void UpdateControls()
    {
        _mouseScreenCoords = new Vec2<float>(MouseState.X, MouseState.Y);
        _mouseWorldCoords = ScreenToWorldCoords(_mouseScreenCoords);
        
        _keybindManager.Call(MouseState, KeyboardState);
        Guis.UpdateVisible();
        
        // zoom
        if (MouseState.ScrollDelta.Y != 0)
        {
            _scrollPos += -MouseState.ScrollDelta.Y;
            
            var scale = (decimal)Math.Pow(1.1, -_scrollPos) * 1024;
            
            // s = 1024 * 1.1^-p
            // s / 1024 = 1.1^-p
            // log_1.1(s / 1024) = -p
            // p = -log_1.1(s / 1024)
            
            if (scale < _scaleMinimum) _scrollPos --;
            else if (scale > ScaleMaximum) _scrollPos ++;
            else _scale = scale;
            
        }
    }
    
    /// <summary>
    /// Updates the controls and controls if the game should be running. Runs before <see cref="OnRenderFrame"/>.
    /// </summary>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        
        var isActive = CheckActive();
        
        if (!isActive)
        {
            Thread.Sleep(CheckActiveDelay);
        }
        
        // update the controls
        UpdateControls();
        
        // update whether the game is running
        GameManager.SetRunning(isActive);
    }
    
    /// <summary>
    /// Initializes the render thread.
    /// </summary>
    protected override void OnLoad()
    {
        GL.DebugMessageCallback(DebugMessageDelegate, IntPtr.Zero);
        GL.Enable(EnableCap.DebugOutput);
        
        if (SynchronousGlDebug)
            GL.Enable(EnableCap.DebugOutputSynchronous);
        
        base.OnLoad();
        
        // create the keybinds
        CreateKeybinds();
        
        // set clear color
        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        
        // create all the shaders
        Shaders.Instantiate();
        
        // create the textures
        Textures.Instantiate();
        
        // create the shaders
        _rQt = new QuadtreeRenderable(Shaders.Qtr, Math.Min(GameManager.WorldHeight, 16), BufferUsageHint.StreamDraw);
        _rFont = new FontRenderable(Shaders.Font, BufferUsageHint.DynamicDraw);
        
        // create the GUIs. must be done after creating the renderables
        CreateGuis();

        Translation = (0, 0);
        Scale = 1;
        
        Console.WriteLine(_scrollPos);
        
        // start the game logic
        GameManager.SetRunning(true);
        
        
        Log("===============[ BEGIN PROGRAM  ]===============", "Load");
    }
    
    private void CreateKeybinds()
    {
        _keybindManager.Add("mapWorld", () => _worldAction = (WorldAction.Map, "map.svg"), [(Key.M, RisingEdge)]);
        _keybindManager.Add("saveWorld", () =>_worldAction = (WorldAction.Save, "save.qdt"), [(Key.S, RisingEdge)]);
        _keybindManager.Add("loadWorld", () => _worldAction = (WorldAction.Load, "save.qdt"), [(Key.L, RisingEdge)]);
        _keybindManager.Add("clearWorld", () => _worldAction = (WorldAction.Clear, ""), [(Key.C, RisingEdge)]);
        _keybindManager.Add("logMousePos", () => Log(_mouseWorldCoords), [(Key.LeftControl, RisingEdge)]);
        
        _keybindManager.Add("shuffleBrush", () =>
        {
            _activeBrush = new Paint( new Color((uint)(Random.Next() & 0x00ffffff)));
            Log($"Brush Changed to {_activeBrush}");
        }, [
            (Key.RightMouse, RisingEdge)
        ]);
        
        _keybindManager.Add("captureShiftMousePos", () => _capturedMouseWorldCoordsLShift = _mouseWorldCoords, [
            (Key.LeftShift, Enabled),
            (Key.LeftMouse, RisingEdge)
        ]);
        _keybindManager.Add("uncaptureShiftMousePos", () => _capturedMouseWorldCoordsLShift = null, [
            (Key.LeftShift, FallingEdge)
        ]);
        
        _keybindManager.Add("captureMMouse", () =>
        {
            _capturedMouseScreenCoordsMMouse = _mouseScreenCoords;
            _capturedTranslationMMouse = Translation;
        }, [
            (Key.MiddleMouse, RisingEdge)
        ]);
        _keybindManager.Add("uncaptureMMouse", () =>
        {
            _capturedMouseScreenCoordsMMouse = null;
            _capturedTranslationMMouse = null;
        }, [
            (Key.MiddleMouse, FallingEdge)
        ]);
        
        _keybindManager.Add("translateScreen", () =>
        {
            if (!_capturedMouseScreenCoordsMMouse.HasValue) return;
            if (!_capturedTranslationMMouse.HasValue) return;
            
            var worldTranslationOffset =
                ((Vec2<decimal>)(_capturedMouseScreenCoordsMMouse - _mouseScreenCoords) / _scale).FlipY();
            
            Translation = _capturedTranslationMMouse.Value + worldTranslationOffset;
            
        }, [(Key.MiddleMouse, Enabled)]);
        
        _keybindManager.Add("drawSingle", () => WorldModifications.Add(new WorldModification(new Range2D(_mouseWorldCoords), _activeBrush)), [
            (Key.LeftMouse, Enabled),
            (Key.LeftShift, Disabled)
        ]);
        
        _keybindManager.Add("drawRect", () =>
        {
            if (!_capturedMouseWorldCoordsLShift.HasValue) return;
            WorldModifications.Add(new WorldModification(new Range2D(_mouseWorldCoords, _capturedMouseWorldCoordsLShift.Value), _activeBrush));
        }, [
            (Key.LeftMouse, FallingEdge),
            (Key.LeftShift, Enabled)
        ]);
    }
    
    private static void CreateGuis()
    {
        GuiElements.Register("test", attributes => new TestElement(attributes));
        
        GuiEvents.Register("run", () => Console.WriteLine("Hello from test event!"));
        
        Guis.Register("test", new Gui($"{GlobalVariables.AssetDirectory}/gui/test.xml"));
        Guis.SetVisibility("test", false);
    }
    
    
    /// <summary>
    /// Returns true if the game should be running (game logic, rendering, etc.)
    /// </summary>
    private bool CheckActive()
    {
        // only run when focused
        if (!IsFocused)
            return false;
        
        // only run when hovered
        if (MousePosition.X < 0 || MousePosition.X > ClientSize.X || MousePosition.Y < 0 || MousePosition.Y > ClientSize.Y)
            return false;
        
        return true;
    }
    
    /// <summary>
    /// Updates the viewport when the window is resized.
    /// </summary>
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        
        GL.Viewport(0, 0, e.Width, e.Height);
        
        var newSize = new Vec2<float>(e.Width, e.Height);
        if (GameManager.ScreenSize != (0, 0))
        {
            var scale2D = newSize / GameManager.ScreenSize;
            var scale = (decimal)(scale2D.X + scale2D.Y) / 2m;

            _scale *= scale;
            Console.WriteLine(scale);
            Console.WriteLine(_scale);
        }

        // update screenSize on the logic thread
        GameManager.UpdateScreenSize(newSize);
    }
    
    /// <summary>
    /// Shuts down the game.
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        // close the GameManager thread
        GameManager.Close();

        WorldModifications.Clear();

        base.OnClosing(e);
    }
    
    #region Public setters / getters
    
    /// <summary>
    /// Resets the world modifications on the render thread. Run once after each logic tick.
    /// </summary>
    public static (WorldModification[] Modifications, (WorldAction Action, string Arg)? Action) GetWorldModifications()
    {
        //TODO: thread safety
        var modifications = WorldModifications.ToArray();
        var action = _worldAction;
        
        _worldAction = null;
        WorldModifications.Clear();
        
        return (modifications, action);
    }
    
    /// <summary>
    /// Returns refs to the tree and data modification arrays, allowing new modification to be uploaded to the gpu.
    /// </summary>
    /// <remarks>Does not wait for <see cref="GeometryLock"/>.</remarks>
    public static QuadtreeModifications GetModificationArrays()
    {
        return new QuadtreeModifications(_treeModifications, _dataModifications);
    }
    
    /// <summary>
    /// Updates the additional geometry information on the render thread. See <see cref="GetModificationArrays"/>.
    /// </summary>
    /// <param name="treeLength">the length of the "tree" section</param>
    /// <param name="dataLength">the length of the "data" section</param>
    /// <param name="renderRoot">the root node for rendering</param>
    /// <param name="renderRange">the dimensions of <paramref name="renderRoot"/></param>
    /// <remarks>Does not wait for <see cref="GeometryLock"/>.</remarks>
    public static void UpdateGeometryParameters(long treeLength, long dataLength, QuadtreeNode renderRoot, Range2D renderRange)
    {
        _treeLength = treeLength;
        _dataLength = dataLength;
        _treeIndex = 0;
        _dataIndex = 0;
        
        _renderRoot = renderRoot;
        _renderRange = renderRange;
        
        _unuploadedGeometry = true;
    }
    
    /// <summary>
    /// Checks whether new geometry can be uploaded. Will wait for <see cref="Constants.RenderLockTimeout"/> to get a
    /// lock, and keeps that lock open only when returning true.
    /// </summary>
    public static bool CanUpdateGeometry()
    {
        if (!GeometryLock.IsSet && !GeometryLock.Wait(RenderLockTimeout)) return false;
        
        GeometryLock.Reset();
        var canUpdate = !_unuploadedGeometry;
        
        // set lock if we can't update geometry
        if (canUpdate == false) GeometryLock.Set();
        
        return canUpdate;
    }
    
    /// <summary>
    /// Updates the MSPT metric.
    /// </summary>
    /// <param name="mspt">the new mpst</param>
    public static void UpdateMspt(float mspt)
    {
        _mspt = mspt;
    }
    
    /// <summary>
    /// Overrides the current <see cref="WorldAction"/>, setting it to a new value.
    /// </summary>
    /// <param name="action">the new <see cref="WorldAction"/></param>
    /// <param name="args">the new args</param>
    public static void SetWorldAction(WorldAction action, string args)
    {
        _worldAction = (action, args);
    }
    
    public override void Dispose()
    {
        GC.SuppressFinalize(this);
        
        GeometryLock.Dispose();
        WorldModifications.Clear();
        
        base.Dispose();
    }
    
    #endregion
}

public readonly struct WorldModification(Range2D range, Tile tile)
{
    public readonly Range2D Range = range;
    public readonly Tile Tile = tile;
}

public readonly struct QuadtreeModifications
{
    public readonly DynamicArray<ArrayModification<QuadtreeNode>> Tree;
    public readonly DynamicArray<ArrayModification<Tile>> Data;
    
    public QuadtreeModifications(DynamicArray<ArrayModification<QuadtreeNode>> tree, DynamicArray<ArrayModification<Tile>> data)
    {
        Tree = tree;
        Data = data;
    }
}
