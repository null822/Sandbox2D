using System.ComponentModel;
using Math2D;
using Math2D.Binary;
using Math2D.Quadtree;
using OpenTK.Graphics.OpenGL4;
using Sandbox2D;
using Sandbox2D.Graphics.ShaderControllers;
using Sandbox2D.Managers;
using Sandbox2D.Registry_;
using Sandbox2D.UserInterface.Input;
using Sandbox2DTest.Graphics.ShaderControllers;
using Sandbox2DTest.Packets;
using Sandbox2DTest.World;
using Sandbox2DTest.World.Tiles;
using static Sandbox2D.Util;
using static Sandbox2D.Constants;
using static Sandbox2D.UserInterface.Input.KeybindKeyType;

namespace Sandbox2DTest;

public class MainRenderManager(IRegistryPopulator registry) : RenderManager(registry)
{
    private MainGameManager _gameManager = null!;
    
    public bool Using64BitQt { get; private set; }
    public int MaxGpuQtHeight => Using64BitQt ? 64 : 32;
    
    private QuadtreeRenderer _rQt = null!;
    private TextRenderer _rText = null!;
    
    private bool _unuploadedGeometry;
    private int _gpuWorldHeight;
    
    // world geometry
    /// <summary>
    /// Stores modifications to the <see cref="Quadtree{T}.Tree"/> array that are to be applied to the
    /// <see cref="QuadtreeRenderer"/>
    /// </summary>
    /// <remarks>See <see cref="GeometryLock"/></remarks>
    public readonly DynamicArray<ArrayModification<QuadtreeNode>> TreeModifications = new(storeOccupied: false);
    /// <summary>
    /// Stores modifications to the <see cref="Quadtree{T}.Data"/> array that are to be applied to the
    /// <see cref="QuadtreeRenderer"/>
    /// </summary>
    /// <remarks>See <see cref="GeometryLock"/></remarks>
    public readonly DynamicArray<ArrayModification<Tile>> DataModifications = new(storeOccupied: false);
    /// <summary>
    /// A lock to manage multithreaded access to <see cref="TreeModifications"/> and <see cref="DataModifications"/>
    /// </summary>
    public readonly ManualResetEventSlim GeometryLock = new (true);
    private int _treeIndex;
    private int _dataIndex;
    private long _treeLength;
    private long _dataLength;
    private QuadtreeNode _renderRoot;
    private Range2D _renderRange;
    
    // world editing
    private Tile _activeBrush = new Air();

    private readonly Lock _outgoingPacketLock = new();
    private readonly List<LocalPacket> _outgoingPackets = [];
    private readonly Lock _incomingPacketLock = new();
    private readonly List<LocalPacket> _incomingPackets = [];
    
    private readonly List<WorldModification> _worldModifications = [];
    private readonly Random _random = new();
    
    // controls

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
    private Vec2<decimal>? _capturedTranslationMMouse;

    private decimal _scaleMinimum;
    private decimal _scaleMaximum;
    
    private decimal _scale = 1;
    private Vec2<decimal> _translation;
    private float _scrollPos;
    
    /// <summary>
    /// The scale of the world. World coordinates are multiplied by this value to get screen coordinates
    /// </summary>
    private decimal Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            _scrollPos = (float)-Math.Log((double)(_scale / 1024), 1.1);
        }
    }
    
    /// <summary>
    /// The translation of the screen relative to the origin of the world (0, 0)
    /// </summary>
    private Vec2<decimal> Translation
    {
        get => _translation;
        set => _translation = value;
    }
    
    public override void Render(double frametime)
    {
        // clear the color buffer
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        // re-create the quadtree renderable if the world height changed
        var newGpuWorldHeight = Math.Min(_gameManager.WorldHeight, MaxGpuQtHeight);
        if (_gpuWorldHeight != newGpuWorldHeight)
        {
            CalculateScaleBounds();
            
            _gpuWorldHeight = newGpuWorldHeight;
            _rQt.ResetGeometry();
            _rQt.SetMaxHeight(_gpuWorldHeight);
        }
        
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
        _rQt.SetTransform(Translation + (Vec2<decimal>)_renderRange.Center, (double)Scale);
        
        // render the world
        _rQt.Invoke();
        
        // TODO: render brush outline
        
        // render the GUIs
        GuiManager.RenderVisible();
        
        // FPS display
        var mspt = Math.Max(
            _gameManager.GetCurrentTickTime().TotalMilliseconds,
            _gameManager.GetPreviousTickTime().TotalMilliseconds);
        _rText.SetText($"{1 / frametime:F4} FPS, {mspt:F4} MSPT, Load: {mspt / (1000.0 / Tps):P4}\n" +
                       $"Mouse: ({_mouseWorldCoords.X:F0}, {_mouseWorldCoords.Y:F0}) Translation: ({_translation.X:F4}, {_translation.Y:F4}) Scale: x{Scale:F16}",
            (4, 4), 1f, ScreenSize);
        _rText.Invoke();
        
    }

    public override void Update()
    {
        
        foreach (var packet in GetIncomingPackets())
        {
            switch (packet.Name)
            {
                case "new_brush":
                    _activeBrush = packet.GetArg<Tile>();
                    Log($"Brush Changed to {_activeBrush}");
                    break;
            }
        }
    }
    
    public override void UpdateControls(InputTimeline frame)
    {
        _mouseScreenCoords = frame.MousePos;
        _mouseWorldCoords = ScreenToWorldCoords(_mouseScreenCoords);
        
        GuiManager.UpdateVisible();
        
        if (frame.IsPressed(Key.LeftMouse) && frame.IsReleased(Key.LeftShift) && frame.IsReleased(Key.LeftControl))
        {
            _worldModifications.Add(
                new WorldModification(
                    new Range2D(RoundMouseWorldCoords(_mouseWorldCoords)),
                    _activeBrush
                ));
            
            foreach (var input in frame)
            {
                if (input.Type == InputType.MousePos)
                {
                    _worldModifications.Add(
                        new WorldModification(
                            new Range2D(RoundMouseWorldCoords(ScreenToWorldCoords(input.Vec))),
                            _activeBrush
                        ));
                }
            }
        }
        
        // zoom
        if (frame.MouseScrollDelta.Y != 0)
        {
            _scrollPos += -frame.MouseScrollDelta.Y;
            
            var scale = (decimal)Math.Pow(1.1, -_scrollPos) * 1024;
            
            // s = 1024 * 1.1^-p
            // p = -log_1.1(s / 1024)
            
            if (scale < _scaleMinimum) _scrollPos --;
            else if (scale > _scaleMaximum) _scrollPos ++;
            else _scale = scale;
        }
    }
    
    public override void Initialize()
    {
        base.Initialize();
        
        if (IsExtensionSupported("GL_ARB_gpu_shader_int64")) Using64BitQt = true;
        
        // create the keybinds
        RegisterKeybinds();
        
        // set clear color
        GL.ClearColor(System.Drawing.Color.FromArgb(255, 24, 24, 24));
        
        Translation = (0, 0);
        Scale = 1.1m;
        CalculateScaleBounds();
        
        _rQt = new QuadtreeRenderer(Math.Min(_gameManager.WorldHeight, MaxGpuQtHeight), this,
            GlContext.Registry.ShaderProgram.Create("quadtree"), BufferUsageHint.StreamDraw);
        _rText = new TextRenderer(GlContext.Registry.ShaderProgram.Create("text"), BufferUsageHint.DynamicDraw);
        _rText.SetColor(Color.Gray);
        
        GuiManager.SetVisibility("test", false);
        
        // start the game logic
        _gameManager.SetRunning(true);
    }
    
    private void RegisterKeybinds()
    {
        KeybindManager.Add("mapWorld", Key.M, RisingEdge, () => SendPacket(new LocalPacket("map", LocalPacketType.Map, "map.svg")));
        KeybindManager.Add("saveWorld", Key.S, RisingEdge, () => SendPacket( new LocalPacket("save", LocalPacketType.Save, "save.qdt")));
        KeybindManager.Add("loadWorld", Key.L, RisingEdge, () => SendPacket(new LocalPacket("load", LocalPacketType.Load, "save.qdt")));
        KeybindManager.Add("clearWorld", Key.C, RisingEdge, () => SendPacket(new LocalPacket("clear", LocalPacketType.Clear)));
        KeybindManager.Add("logMousePos", Key.LeftAlt, RisingEdge, () => Log(_mouseWorldCoords));
        
        KeybindManager.Add("shuffleBrush", Key.RightMouse, RisingEdge, () =>
        {
            _activeBrush = new Paint(new Color((uint)(_random.Next() & 0x00ffffff)));
            Log($"Brush Changed to {_activeBrush}");
        });
        
        KeybindManager.Add("captureShiftMousePos", [(Key.LeftShift, Enabled), (Key.LeftMouse, RisingEdge)],
            () => _capturedMouseWorldCoordsLShift = _mouseWorldCoords);
        KeybindManager.Add("uncaptureShiftMousePos", [(Key.LeftShift, FallingEdge)],
            () => _capturedMouseWorldCoordsLShift = null);
        
        KeybindManager.Add("captureMMouse", [(Key.MiddleMouse, RisingEdge)], () =>
        {
            _capturedMouseScreenCoordsMMouse = _mouseScreenCoords;
            _capturedTranslationMMouse = Translation;
        });
        KeybindManager.Add("uncaptureMMouse", [(Key.MiddleMouse, FallingEdge)], () =>
        {
            _capturedMouseScreenCoordsMMouse = null;
            _capturedTranslationMMouse = null;
        });
        
        KeybindManager.Add("translateScreen", [(Key.MiddleMouse, Enabled)], () =>
        {
            if (!_capturedMouseScreenCoordsMMouse.HasValue) return;
            if (!_capturedTranslationMMouse.HasValue) return;
            
            var worldTranslationOffset =
                ((Vec2<decimal>)(_mouseScreenCoords - _capturedMouseScreenCoordsMMouse) / Scale).FlipY();
            
            Translation = _capturedTranslationMMouse.Value + worldTranslationOffset;
            
        });
        
        KeybindManager.Add("pickTile", [(Key.LeftControl, Enabled), (Key.LeftMouse, RisingEdge)], () => 
        {
            SendPacket(new LocalPacket("new_brush", LocalPacketType.GetTile, _mouseWorldCoords, "new_brush"));
        });
        
        KeybindManager.Add("drawRect", [(Key.LeftMouse, FallingEdge), (Key.LeftShift, Enabled)], () => 
        {
            if (!_capturedMouseWorldCoordsLShift.HasValue) return;
            _worldModifications.Add(
                new WorldModification(
                    RoundMouseWorldRange(new Range2D(_mouseWorldCoords, _capturedMouseWorldCoordsLShift.Value)),
                    _activeBrush
                ));
        });
        
        KeybindManager.Add("moveBlCorner", Key.KeyPad1, RisingEdge,
            () => _translation = -(Vec2<decimal>)_gameManager.WorldDimensions.Bl);
        KeybindManager.Add("moveBottomSide", Key.KeyPad2, RisingEdge,
            () => _translation = -(Vec2<decimal>)_gameManager.WorldDimensions.BottomVec);
        KeybindManager.Add("moveBrCorner", Key.KeyPad3, RisingEdge,
            () => _translation = -(Vec2<decimal>)_gameManager.WorldDimensions.Br - (1, 0));
        KeybindManager.Add("moveLeftSide", Key.KeyPad4, RisingEdge,
            () => _translation = -(Vec2<decimal>)_gameManager.WorldDimensions.LeftVec);
        KeybindManager.Add("moveCenter", Key.KeyPad5, RisingEdge,
            () => _translation = -(Vec2<decimal>)_gameManager.WorldDimensions.Center);
        KeybindManager.Add("moveRightSide", Key.KeyPad6, RisingEdge,
            () => _translation = -(Vec2<decimal>)_gameManager.WorldDimensions.RightVec - (1, 0));
        KeybindManager.Add("moveTlCorner", Key.KeyPad7, RisingEdge,
            () => _translation = -(Vec2<decimal>)_gameManager.WorldDimensions.Tl - (0, 1));
        KeybindManager.Add("moveTopSide", Key.KeyPad8, RisingEdge,
            () => _translation = -(Vec2<decimal>)_gameManager.WorldDimensions.TopVec - (0, 1));
        KeybindManager.Add("moveTrCorner", Key.KeyPad9, RisingEdge,
            () => _translation = -(Vec2<decimal>)_gameManager.WorldDimensions.Tr - (1, 1));
    }
    
    
    private Range2D RoundMouseWorldRange(Range2D mouseWorldRange)
    {
        var roundDist = (long)(1 / Scale);
        if (roundDist is 0 or 1) return mouseWorldRange;
        roundDist *= DrawAccuracy;
        roundDist = (long)BitUtil.PrevPowerOf2((ulong)roundDist);
        
        var bl = RoundMouseWorldCoords(mouseWorldRange.Bl, roundDist);
        var tr = RoundMouseWorldCoords(mouseWorldRange.Tr, roundDist) - (1, 1);

        return new Range2D(bl, tr);
    }
    
    private Vec2<long> RoundMouseWorldCoords(Vec2<long> mouseWorldCoords)
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
    
    public override void OnResize(Vec2<int> newSize)
    {
        if (ScreenSize != (0, 0))
        {
            var scale2D = (Vec2<float>)newSize / (Vec2<float>)ScreenSize;
            var scale = (decimal)(scale2D.X + scale2D.Y) / 2m;
            
            Scale *= scale;
        }
        
        CalculateScaleBounds();
    }
    
    public override void OnClose(CancelEventArgs c)
    {
        _worldModifications.Clear();
    }
    
    private void CalculateScaleBounds()
    {
        var minScreen = (decimal)Math.Min(ScreenSize.X, ScreenSize.Y);
        
        const decimal worldFill = 0.5m;
        const decimal pixelFill = 0.5m;
        
        _scaleMinimum = minScreen / BitUtil.Pow2(_gameManager.WorldHeight) * worldFill;
        _scaleMaximum = minScreen * pixelFill;
        
        Scale = Math.Clamp(Scale, _scaleMinimum, _scaleMaximum);
    }
    
    
    #region Local Packets
    
    /// <summary>
    /// Returns all outgoing packets, and clears the list.
    /// </summary>
    public LocalPacket[] GetOutgoingPackets()
    {
        LocalPacket[] actions;
        lock (_outgoingPacketLock)
        {
            actions = _outgoingPackets.ToArray();
            _outgoingPackets.Clear();
        }
        
        return actions;
    }
    
    /// <summary>
    /// Returns all incoming packets, and clears the list.
    /// </summary>
    private LocalPacket[] GetIncomingPackets()
    {
        LocalPacket[] packets;
        lock (_incomingPacketLock)
        {
            packets = _incomingPackets.ToArray();
            _incomingPackets.Clear();
        }
        return packets;
    }
    
    /// <summary>
    /// Adds a <see cref="LocalPacket"/> to the list of packets to send next server tick.
    /// </summary>
    /// <param name="packet">the <see cref="LocalPacket"/></param>
    private void SendPacket(LocalPacket packet)
    {
        lock (_outgoingPacketLock)
        {
            _outgoingPackets.Add(packet);
        }
    }
    
    /// <summary>
    /// Sends a <see cref="LocalPacket"/> to this <see cref="MainRenderManager"/>.
    /// </summary>
    /// <param name="packet">the <see cref="LocalPacket"/></param>
    public void AddIncomingPacket(LocalPacket packet)
    {
        lock (_incomingPacketLock)
        {
            _incomingPackets.Add(packet);
        }
    }
    
    /// <summary>
    /// Sends multiple <see cref="LocalPacket"/>s to this <see cref="MainRenderManager"/>.
    /// </summary>
    /// <param name="packets">the <see cref="LocalPacket"/>s</param>
    public void AddIncomingPackets(LocalPacket[] packets)
    {
        lock (_incomingPacketLock)
        {
            _incomingPackets.AddRange(packets);
        }
    }
    
    #endregion
    
    #region Public setters / getters
    
    /// <summary>
    /// Gets the modifications done to the world since the last time this method was called, and removes all
    /// modifications stored on this <see cref="MainRenderManager"/>.
    /// </summary>
    public WorldModification[] GetWorldModifications()
    {
        //TODO: thread safety
        var modifications = _worldModifications.ToArray();
        _worldModifications.Clear();
        
        return modifications;
    }
    
    
    /// <summary>
    /// Updates the additional geometry information on the render thread. See <see cref="TreeModifications"/> and <see cref="DataModifications"/>.
    /// </summary>
    /// <param name="treeLength">the length of the "tree" section</param>
    /// <param name="dataLength">the length of the "data" section</param>
    /// <param name="renderRoot">the root node for rendering</param>
    /// <param name="renderRange">the dimensions of <paramref name="renderRoot"/></param>
    /// <remarks>Does not wait for <see cref="GeometryLock"/>.</remarks>
    public void UpdateGeometryParameters(long treeLength, long dataLength, QuadtreeNode renderRoot, Range2D renderRange)
    {
        _treeLength = treeLength;
        _dataLength = dataLength;
        _treeIndex = 0;
        _dataIndex = 0;
        
        _renderRoot = renderRoot;
        _renderRange = renderRange;
    }
    
    public void SetGeometryUploaded()
    {
        _unuploadedGeometry = true;
    }
    
    /// <summary>
    /// Checks whether new geometry can be uploaded. Will wait for <see cref="Constants.RenderLockTimeout"/> to get a
    /// lock, and keeps that lock open only when returning true.
    /// </summary>
    public bool CanUpdateGeometry()
    {
        if (!GeometryLock.IsSet && !GeometryLock.Wait(RenderLockTimeout)) return false;
        
        GeometryLock.Reset();
        var canUpdate = !_unuploadedGeometry;
        
        // set lock if we can't update geometry
        if (canUpdate == false) GeometryLock.Set();
        
        return canUpdate;
    }
    
    public void LoadWorld(string path)
    {
        SendPacket(new LocalPacket("load_world", LocalPacketType.Load, path));
    }
    
    /// <summary>
    /// Converts screen coords (like mouse pos) into world coords (like positions of objects).
    /// </summary>
    /// <param name="screenCoords">The screen coords to convert</param>
    public Vec2<long> ScreenToWorldCoords(Vec2<float> screenCoords)
    {
        var center = (Vec2<float>)ScreenSize / 2f;
        
        screenCoords -= center;
        
        screenCoords = screenCoords.FlipY();
        
        var value = (Vec2<decimal>)screenCoords / Scale - Translation;
        
        return new Vec2<long>(
            (long)Math.Clamp(Math.Floor(value.X), long.MinValue, long.MaxValue),
            (long)Math.Clamp(Math.Floor(value.Y), long.MinValue, long.MaxValue));
    }
    
    /// <summary>
    /// Converts world coords (like positions of objects) into screen coords (like mouse pos).
    /// </summary>
    /// <param name="worldCoords">The world coords to convert</param>
    public Vec2<int> WorldToScreenCoords(Vec2<long> worldCoords)
    {
        var screenSize = (Vec2<float>)ScreenSize;
        
        var center = screenSize / 2f;
        var value = (((Vec2<decimal>)worldCoords + Translation) * Scale).FlipY() + (Vec2<decimal>)center;
        
        return new Vec2<int>(
            (int)Math.Clamp(Math.Floor(value.X), int.MinValue, int.MaxValue), 
            (int)Math.Clamp(Math.Floor(value.Y), int.MinValue, int.MaxValue));
    }
    
    public Range2D CalculateScreenRange()
    {
        var tlScreen = ScreenToWorldCoords((0, ScreenSize.Y));
        var brScreen = ScreenToWorldCoords((ScreenSize.X, 0));
        return new Range2D(tlScreen, brScreen);
    }
    
    #endregion
    
    
    public override void Dispose()
    {
        GeometryLock.Dispose();
        _worldModifications.Clear();
        
        base.Dispose();
    }
    
    public void SetGameManager(MainGameManager manager)
    {
        _gameManager = manager;
        _gpuWorldHeight = _gameManager.WorldHeight;
    }
}

public record WorldModification(Range2D Range, Tile Tile);
