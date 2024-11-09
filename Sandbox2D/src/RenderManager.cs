using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Math2D;
using Math2D.Binary;
using Math2D.Quadtree;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Sandbox2D.Graphics.ShaderControllers;
using Sandbox2D.Graphics.ShaderControllers.Quadtree;
using Sandbox2D.UserInterface.Elements;
using Sandbox2D.UserInterface.Keybinds;
using Sandbox2D.World;
using Sandbox2D.World.Tiles;
using static Sandbox2D.Util;
using static Sandbox2D.Constants;
using static Sandbox2D.DerivedConstants;
using static Sandbox2D.UserInterface.Keybinds.KeybindKeyType;

namespace Sandbox2D;

// TODO: move most of this stuff out to a static RenderManager and leave this as a WindowManager
// TODO: implement GUIs
// TODO: migrate to OpenTK 5 once its out

public class RenderManager(GameWindowSettings gameSettings, NativeWindowSettings nativeSettings) :
    GameWindow(gameSettings, nativeSettings)
{
    // rendering
    private static readonly HashSet<string> SupportedExtensions = [];
    
    public static bool Using64BitQt { get; private set; }
    public static int MaxGpuQtHeight => Using64BitQt ? 64 : 32;
    
    private static QuadtreeRenderer _rQt;
    private static TextRenderer _rText;
    
    private static bool _unuploadedGeometry;
    private static int _gpuWorldHeight = GameManager.WorldHeight;
    
    // world geometry
    /// <summary>
    /// Stores modifications to the <see cref="Quadtree{T}.Tree"/> array that are to be applied to the
    /// <see cref="QuadtreeRenderer"/>
    /// </summary>
    /// <remarks>See <see cref="GeometryLock"/></remarks>
    public static readonly DynamicArray<ArrayModification<QuadtreeNode>> TreeModifications = new(storeOccupied: false);
    /// <summary>
    /// Stores modifications to the <see cref="Quadtree{T}.Data"/> array that are to be applied to the
    /// <see cref="QuadtreeRenderer"/>
    /// </summary>
    /// <remarks>See <see cref="GeometryLock"/></remarks>
    public static readonly DynamicArray<ArrayModification<Tile>> DataModifications = new(storeOccupied: false);
    /// <summary>
    /// A lock to manage multithreaded access to <see cref="TreeModifications"/> and <see cref="DataModifications"/>
    /// </summary>
    public static readonly ManualResetEventSlim GeometryLock = new (true);
    private static int _treeIndex;
    private static int _dataIndex;
    private static long _treeLength;
    private static long _dataLength;
    private static QuadtreeNode _renderRoot;
    private static Range2D _renderRange;
    
    // world editing
    private static Tile _activeBrush = new Air();
    
    private static (WorldAction action, string arg)? _worldAction;
    private static readonly List<WorldModification> WorldModifications = [];
    private static readonly Random Random = new();
    
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
    private static decimal _scaleMaximum;
    
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
            CalculateScaleBounds();
            
            _gpuWorldHeight = newGpuWorldHeight;
            _rQt.ResetGeometry();
            _rQt.SetMaxHeight(_gpuWorldHeight);
        }
        
        // only render when the game is running
        if (!GameManager.IsRunning)
            return;
        
        // clear the color buffer
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        // update world geometry / transform
        if (_unuploadedGeometry && (GeometryLock.IsSet || GeometryLock.Wait(RenderLockTimeout)))
        {
            GeometryLock.Reset();
            
            // update geometry
            (_treeIndex, _dataIndex) = _rQt.SetGeometry(TreeModifications, DataModifications,
                _treeLength, _dataLength,
                _treeIndex, _dataIndex,
                _renderRoot);
            
            // if we have uploaded all modifications to the gpu
            if (_treeIndex >= TreeModifications.Length && _dataIndex >= DataModifications.Length)
            {
                if (TreeModifications.Length != 0)
                {
                    _treeIndex = 0;
                    TreeModifications.Clear();
                }
                
                if (DataModifications.Length != 0)
                {
                    _dataIndex = 0;
                    DataModifications.Clear();
                }
                
                // reset geometry flag
                _unuploadedGeometry = false;
            }
            
            GeometryLock.Set();
        }
        
        // update the world transform
        _rQt.SetTransform(Translation + (Vec2<decimal>)_renderRange.Center, (double)_scale);
        
        // render the world
        _rQt.Invoke();
        
        // TODO: render brush outline
        
        
        // render the GUIs
        Registry.Gui.RenderVisible();
        
        // FPS display
        var mspt = Math.Max(
            GameManager.GetCurrentTickTime().TotalMilliseconds,
            GameManager.GetPreviousTickTime().TotalMilliseconds);
        _rText.SetText($"{1 / args.Time:F4} FPS, {mspt:F4} MSPT, Load: {mspt / TargetMspt:P4}\n" +
                       $"Mouse: ({_mouseWorldCoords.X:F0}, {_mouseWorldCoords.Y:F0}) Translation: ({_translation.X:F4}, {_translation.Y:F4}) Scale: x{_scale:F16}", (4,4), 1f);
        _rText.Invoke();
        
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
            else if (scale > _scaleMaximum) _scrollPos ++;
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
        GL.ClearColor(System.Drawing.Color.FromArgb(255, 12, 12, 12));
        
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
        Registry.ShaderProgram.Register("quadtree", "quadtree_vert", "quadtree_frag");
        Registry.ShaderProgram.Register("data_patch", "data_patch_comp");
        Registry.ShaderProgram.Register("text", "gui/font/text_vert", "gui/font/text_frag");
        // debug programs
        Registry.ShaderProgram.Register("debug/noise", "debug/noise_vert", "debug/noise_frag");
        Registry.ShaderProgram.Register("debug/texture", "debug/texture_vert", "debug/texture_frag");
        Registry.ShaderProgram.Register("debug/vertex", "debug/vertex_vert", "debug/vertex_frag");
        
        // create the renderables
        _rQt = new QuadtreeRenderer(Registry.ShaderProgram.Create("quadtree"), Math.Min(GameManager.WorldHeight, MaxGpuQtHeight), BufferUsageHint.StreamDraw);
        _rText = new TextRenderer(Registry.ShaderProgram.Create("text"), BufferUsageHint.DynamicDraw);
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
        _keybindManager.Add("mapWorld", Key.M, RisingEdge, () => _worldAction = (WorldAction.Map, "map.svg"));
        _keybindManager.Add("saveWorld", Key.S, RisingEdge, () =>_worldAction = (WorldAction.Save, "save.qdt"));
        _keybindManager.Add("loadWorld", Key.L, RisingEdge, () => _worldAction = (WorldAction.Load, "save.qdt"));
        _keybindManager.Add("clearWorld", Key.C, RisingEdge, () => _worldAction = (WorldAction.Clear, ""));
        _keybindManager.Add("logMousePos", Key.LeftControl, RisingEdge, () => Log(_mouseWorldCoords));
        
        _keybindManager.Add("shuffleBrush", Key.RightMouse, RisingEdge, () =>
        {
            _activeBrush = new Paint(new Color((uint)(Random.Next() & 0x00ffffff)));
            Log($"Brush Changed to {_activeBrush}");
        });
        
        _keybindManager.Add("captureShiftMousePos", [(Key.LeftShift, Enabled), (Key.LeftMouse, RisingEdge)],
            () => _capturedMouseWorldCoordsLShift = _mouseWorldCoords);
        _keybindManager.Add("uncaptureShiftMousePos", [(Key.LeftShift, FallingEdge)],
            () => _capturedMouseWorldCoordsLShift = null);
        
        _keybindManager.Add("captureMMouse", [(Key.MiddleMouse, RisingEdge)], () =>
        {
            _capturedMouseScreenCoordsMMouse = _mouseScreenCoords;
            _capturedTranslationMMouse = Translation;
        });
        _keybindManager.Add("uncaptureMMouse", [(Key.MiddleMouse, FallingEdge)], () =>
        {
            _capturedMouseScreenCoordsMMouse = null;
            _capturedTranslationMMouse = null;
        });
        
        _keybindManager.Add("translateScreen", [(Key.MiddleMouse, Enabled)], () =>
        {
            if (!_capturedMouseScreenCoordsMMouse.HasValue) return;
            if (!_capturedTranslationMMouse.HasValue) return;
            
            var worldTranslationOffset =
                ((Vec2<decimal>)(_mouseScreenCoords - _capturedMouseScreenCoordsMMouse) / _scale).FlipY();
            
            Translation = _capturedTranslationMMouse.Value + worldTranslationOffset;
            
        });
        
        _keybindManager.Add("drawSingle", [(Key.LeftMouse, Enabled), (Key.LeftShift, Disabled)], () =>
            WorldModifications.Add(
                new WorldModification(
                    new Range2D(RoundMouseWorldCoords(_mouseWorldCoords)),
                    _activeBrush
                ))
        );
        
        _keybindManager.Add("drawRect", [(Key.LeftMouse, FallingEdge), (Key.LeftShift, Enabled)], () => 
        {
            if (!_capturedMouseWorldCoordsLShift.HasValue) return;
            WorldModifications.Add(
                new WorldModification(
                    RoundMouseWorldRange(new Range2D(_mouseWorldCoords, _capturedMouseWorldCoordsLShift.Value)),
                    _activeBrush
                ));
        });
        
        _keybindManager.Add("moveBlCorner", Key.KeyPad1, RisingEdge,
            () => _translation = -(Vec2<decimal>)GameManager.WorldDimensions.Bl);
        _keybindManager.Add("moveBottomSide", Key.KeyPad2, RisingEdge,
            () => _translation = -(Vec2<decimal>)GameManager.WorldDimensions.BottomVec);
        _keybindManager.Add("moveBrCorner", Key.KeyPad3, RisingEdge,
            () => _translation = -(Vec2<decimal>)GameManager.WorldDimensions.Br - (1, 0));
        _keybindManager.Add("moveLeftSide", Key.KeyPad4, RisingEdge,
            () => _translation = -(Vec2<decimal>)GameManager.WorldDimensions.LeftVec);
        _keybindManager.Add("moveCenter", Key.KeyPad5, RisingEdge,
            () => _translation = -(Vec2<decimal>)GameManager.WorldDimensions.Center);
        _keybindManager.Add("moveRightSide", Key.KeyPad6, RisingEdge,
            () => _translation = -(Vec2<decimal>)GameManager.WorldDimensions.RightVec - (1, 0));
        _keybindManager.Add("moveTlCorner", Key.KeyPad7, RisingEdge,
            () => _translation = -(Vec2<decimal>)GameManager.WorldDimensions.Tl - (0, 1));
        _keybindManager.Add("moveTopSide", Key.KeyPad8, RisingEdge,
            () => _translation = -(Vec2<decimal>)GameManager.WorldDimensions.TopVec - (0, 1));
        _keybindManager.Add("moveTrCorner", Key.KeyPad9, RisingEdge,
            () => _translation = -(Vec2<decimal>)GameManager.WorldDimensions.Tr - (1, 1));
        
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
    
    private static void CalculateScaleBounds()
    {
        var minScreen = (decimal)Math.Min(ScreenSize.X, ScreenSize.Y);
        
        const decimal worldFill = 0.5m;
        const decimal pixelFill = 0.5m;
        
        _scaleMinimum = minScreen / BitUtil.Pow2(GameManager.WorldHeight) * worldFill;
        _scaleMaximum = minScreen * pixelFill;
        
        Scale = Math.Clamp(Scale, _scaleMinimum, _scaleMaximum);
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
        
        CalculateScaleBounds();
        
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
    /// Updates the additional geometry information on the render thread. See <see cref="TreeModifications"/> and <see cref="DataModifications"/>.
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
    }
    
    public static void SetGeometryUploaded()
    {
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
