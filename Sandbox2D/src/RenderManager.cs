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
using Sandbox2D.UserInterface.Elements;
using Sandbox2D.UserInterface.Keybinds;
using Sandbox2D.World;
using Sandbox2D.World.Tiles;
using static Sandbox2D.Util;
using static Sandbox2D.Constants;
using static Sandbox2D.UserInterface.Keybinds.KeybindKeyType;

namespace Sandbox2D;

// TODO: implement GUIs
// TODO: migrate to OpenTK 5 once its out

public class RenderManager(int width, int height, string title) : GameWindow(GameWindowSettings.Default,
    new NativeWindowSettings { ClientSize = (width, height), Title = title, Flags = ContextFlags.Debug})
{
    
    // rendering
    private static readonly HashSet<string> SupportedExtensions = [];
    
    public static bool Using64BitQt { get; private set; }
    public static int MaxGpuQtHeight => Using64BitQt ? 64 : 32;
    
    private static QuadtreeRenderable _rQt;
    private static TextRenderable _rText;
    
    private static bool _unuploadedGeometry;
    private static int _gpuWorldHeight = GameManager.WorldHeight;
    
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
    private static readonly Random Random = new();
    
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
    
    /// <summary>
    /// The scale of the world. World coordinates are multiplied by this value to get screen coordinates
    /// </summary>
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
    public static Vec2<int> ScreenSize => GlobalVariables.RenderManager.ClientSize.ToVec2();
    
    /// <summary>
    /// Renders to the screen. Runs after <see cref="OnUpdateFrame"/> has completed.
    /// </summary>
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        // re-create the quadtree renderable if the world height changed
        var newGpuWorldHeight = Math.Min(GameManager.WorldHeight, MaxGpuQtHeight);
        if (_gpuWorldHeight != newGpuWorldHeight)
        {
            _scaleMinimum = (decimal)Math.Min(ScreenSize.X, ScreenSize.Y) / BitUtil.Pow2(GameManager.WorldHeight) * 0.8m;
            
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
            
            // calculate the translation to be uploaded to the GPU
            var renderTranslation = Translation + (Vec2<decimal>)_renderRange.Center;
            
            // update the world transform
            _rQt.SetTransform(renderTranslation, (double)_scale);
            
            GeometryLock.Set();
        }
        
        // render the world
        _rQt.Render();
        
        // TODO: render brush outline
        
        
        // render the GUIs
        Registry.Gui.RenderVisible();
        
        // FPS display
        _rText.SetText($"{1 / args.Time:F1} FPS, {_mspt:F1} MSPT\n" +
                       $"M:({_mouseWorldCoords.X:F0}, {_mouseWorldCoords.Y:F0}) T:({_translation.X:F4}, {_translation.Y:F4}) S:{_scale:F16}", (4,4), 1f);
        _rText.Render();
        
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
        Registry.Gui.UpdateVisible();
        
        // zoom
        if (MouseState.ScrollDelta.Y != 0)
        {
            _scrollPos += -MouseState.ScrollDelta.Y;
            
            var scale = (decimal)Math.Pow(1.1, -_scrollPos) * 1024;
            
            // s = 1024 * 1.1^-p
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
        
        // load supported extensions
        GL.GetInteger(GetPName.NumExtensions, out var extensionCount);
        for (var i = 0; i < extensionCount; i++) {
            var ext = GL.GetString(StringNameIndexed.Extensions, i);
            SupportedExtensions.Add(ext);
        }

        if (IsExtensionSupported("GL_ARB_gpu_shader_int64")) Using64BitQt = true;
        
        // create the keybinds
        RegisterKeybinds();
        
        // set clear color
        GL.ClearColor(System.Drawing.Color.Magenta);
        
        // register everything
        RegisterGraphics();
        
        Translation = (0, 0);
        Scale = 1;
        
        // start the game logic
        GameManager.SetRunning(true);
        
        
        Log("===============[ BEGIN PROGRAM  ]===============", "Load");
    }
    
    private static void RegisterGraphics()
    {
        // auto-create the shaders and textures
        Registry.Shader.RegisterAll($"{GlobalVariables.AssetDirectory}/shaders/");
        Registry.Texture.RegisterAll($"{GlobalVariables.AssetDirectory}/textures/");
        
        // create the shader programs
        Registry.ShaderProgram.Register("quadtree", ["quadtree_vert", "quadtree_frag"]);
        Registry.ShaderProgram.Register("text", ["gui/font/text_vert", "gui/font/text_frag"]);
        // debug programs
        Registry.ShaderProgram.Register("noise", ["noise_vert", "noise_frag"]);
        Registry.ShaderProgram.Register("vertex_debug", ["vertex_debug_vert", "vertex_debug_frag"]);
        Registry.ShaderProgram.Register("texture_debug", ["texture_vert", "texture_frag"]);
        
        // create the renderables
        _rQt = new QuadtreeRenderable(Registry.ShaderProgram.Create("quadtree"), Math.Min(GameManager.WorldHeight, MaxGpuQtHeight), BufferUsageHint.StreamDraw);
        _rText = new TextRenderable(Registry.ShaderProgram.Create("text"), BufferUsageHint.DynamicDraw);
        _rText.SetColor(Color.Gray);
        
        // create the GUI elements
        Registry.GuiElement.Register("test", TestElement.Constructor);
        // create the GUI events
        Registry.GuiEvent.Register("run", () => Console.WriteLine("Hello from test event!"));
        // create the GUIs
        Registry.Gui.RegisterAll($"{GlobalVariables.AssetDirectory}/gui/");
        // Registry.Gui.Register("test", new Gui($"{GlobalVariables.AssetDirectory}/gui/test.xml"));
        Registry.Gui.SetVisibility("test", false);
    }
    
    private void RegisterKeybinds()
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
                ((Vec2<decimal>)(_mouseScreenCoords - _capturedMouseScreenCoordsMMouse) / _scale).FlipY();
            
            Translation = _capturedTranslationMMouse.Value + worldTranslationOffset;
            
        }, [(Key.MiddleMouse, Enabled)]);
        
        _keybindManager.Add("drawSingle",
            () => WorldModifications.Add(
                new WorldModification(
                    new Range2D(RoundMouseWorldCoords(_mouseWorldCoords)),
                    _activeBrush
                )),
            [
            (Key.LeftMouse, Enabled),
            (Key.LeftShift, Disabled)
            ]
        );
        
        _keybindManager.Add("drawRect", () =>
        {
            if (!_capturedMouseWorldCoordsLShift.HasValue) return;
            WorldModifications.Add(
                new WorldModification(
                    RoundMouseWorldRange(new Range2D(_mouseWorldCoords, _capturedMouseWorldCoordsLShift.Value)),
                    _activeBrush
                ));
        }, [
            (Key.LeftMouse, FallingEdge),
            (Key.LeftShift, Enabled)
        ]);
    }
    
    private static Range2D RoundMouseWorldRange(Range2D mouseWorldRange)
    {
        var roundDist = (long)(1 / Scale);
        if (roundDist is 0 or 1) return mouseWorldRange;
        roundDist *= DrawAccuracy;
        roundDist = (long)BitUtil.PrevPowerOf2((ulong)roundDist);
        
        var bl = RoundMouseWorldCoords(mouseWorldRange.Bl, roundDist);
        var tr = RoundMouseWorldCoords(mouseWorldRange.Tr, roundDist) - (1, 1);

        return new Range2D(bl, tr);
    }
    
    private static Vec2<long> RoundMouseWorldCoords(Vec2<long> mouseWorldCoords)
    {
        var roundDist = (long)(1 / Scale);
        if (roundDist is 0 or 1) return mouseWorldCoords;
        roundDist *= DrawAccuracy;
        roundDist = (long)BitUtil.PrevPowerOf2((ulong)roundDist);
        
        return RoundMouseWorldCoords(mouseWorldCoords, roundDist);
    }
    
    private static Vec2<long> RoundMouseWorldCoords(Vec2<long> mouseWorldCoords, long roundDist)
    {
        var roundX = mouseWorldCoords.X / roundDist * roundDist;
        var roundY = mouseWorldCoords.Y / roundDist * roundDist;
        
        if (mouseWorldCoords.X - roundX >= roundDist / 2)
            roundX += roundDist;
        if (mouseWorldCoords.Y - roundY >= roundDist / 2)
            roundY += roundDist;
        
        return (roundX, roundY);
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
        }
        
        _scaleMinimum = (decimal)Math.Min(ScreenSize.X, ScreenSize.Y) / BitUtil.Pow2(GameManager.WorldHeight) * 0.8m;
        
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

    public static bool IsExtensionSupported(string extension)
    {
        return SupportedExtensions.Contains(extension);
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
