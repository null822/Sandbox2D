using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Sandbox2D.Graphics.Registry;
using Sandbox2D.Graphics.Renderables;
using Sandbox2D.GUI;
using Sandbox2D.Maths;
using Sandbox2D.Maths.Quadtree;
using Sandbox2D.World;
using Sandbox2D.World.TileTypes;
using static Sandbox2D.Util;
using static Sandbox2D.Constants;

namespace Sandbox2D;

public class RenderManager(int width, int height, string title) : GameWindow(GameWindowSettings.Default,
    new NativeWindowSettings { ClientSize = (width, height), Title = title })
{
    // rendering
    private static QuadtreeRenderable _rQt;
    private static FontRenderable _rFont;
    private static bool _unuploadedGeometry = true;
    
    private static readonly Random Random = new();
    // world geometry
    private static readonly ManualResetEventSlim GeometryLock = new (true);
    private static QuadtreeModifications<ITile> _modifications = new([], []);
    private static int _treeLength;
    private static int _dataLength;
    private static QuadtreeNode _renderRoot;
    private static Range2D _renderRange;
    
    // world editing
    private static ITile _activeBrush = new Air();
    private static uint _brushId;
    private static Range2D _brushRange;
    private static Vec2<long> _leftMouseWorldCoords = new(0);
    private static Vec2<int> _middleMouseScreenCoords = new(0);

    private static (WorldAction action, string arg)? _worldAction;
    private static readonly List<WorldModification> WorldModifications = [];
    
    private static float _mspt;
    
    // controls
    private static decimal _scale = 30;
    private static Vec2<decimal> _translation;
    private static float _scrollPos = 32;
    private static Vec2<decimal> _prevTranslation;
    
    public static ref decimal Scale => ref _scale;
    public static ref Vec2<decimal> Translation => ref _translation;
    
    /// <summary>
    /// Renders everything.
    /// </summary>
    /// <param name="args"></param>
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        // only render when the game is running
        if (!GameManager.IsRunning)
            return;
        
        // clear the color buffer
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        // update world geometry / transform
        if (GeometryLock.IsSet || GeometryLock.Wait(RenderLockTimeout))
        {
            GeometryLock.Reset();
            
            // update geometry
            _rQt.SetGeometry(_modifications, _treeLength, _dataLength, _renderRoot);
            
            // reset geometry flag
            _unuploadedGeometry = false;
            
            // calculate the scale/translation to be uploaded to the GPU
            var renderScale = (float)Scale;
            var renderTranslation = (Vec2<float>)(Translation - (Vec2<decimal>)_renderRange.Center);
            
            // update the world transform
            _rQt.SetTransform(renderTranslation, renderScale);
            
            GeometryLock.Set();
        }
        
        // render the world
        _rQt.Render();
        
        // TODO: render brush outline
        
        
        // render the GUIs
        GuiManager.UpdateGuis();
        Renderables.Render(RenderableCategory.Gui);
        
        var center = GameManager.ScreenSize / 2;
        
        // FPS display
        _rFont.SetText($"{1 / args.Time:F1} FPS, {_mspt:F1} MSPT", -center + (0,10), 1f, false);
        
        _rFont.UpdateVao();
        _rFont.Render();
        _rFont.ResetGeometry();
        
        // Mouse Coordinate Display
        _rFont.SetText(ScreenToWorldCoords((Vec2<int>)MousePosition).ToString(), -center + (0,30), 1f, false);
        
        _rFont.UpdateVao();
        _rFont.Render();
        _rFont.ResetGeometry();
        
        // swap buffers
        SwapBuffers();
        
        PrintGlErrors();
    }
    
    /// <summary>
    /// Controls if the game should be running, and updates the controls.
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
        
        PrintGlErrors();
    }
    
    /// <summary>
    /// Handles all the controls.
    /// </summary>
    private void UpdateControls()
    {
        //TODO: make this method less unreadable and unmaintainable
        
        var mouseScreenCoords = new Vec2<int>((int)Math.Round(MouseState.X), (int)Math.Round(MouseState.Y));
        var mouseWorldCoords = ScreenToWorldCoords(mouseScreenCoords);
        
        var center = GameManager.ScreenSize / 2;
        
        // update GUIs
        GuiManager.MouseOver(mouseScreenCoords - center);
        
        // world actions
        if (KeyboardState.IsKeyPressed(Keys.M))
            _worldAction = (WorldAction.Map, "map.svg");
        else if (KeyboardState.IsKeyPressed(Keys.S))
            _worldAction = (WorldAction.Save, "save.qdt");
        else if (KeyboardState.IsKeyPressed(Keys.L))
            _worldAction = (WorldAction.Load, "save.qdt");
        else if (KeyboardState.IsKeyPressed(Keys.C))
            _worldAction = (WorldAction.Clear, "");
        
        var newScale = Scale;
        var newTranslation = Translation;
        
        // if the scroll wheel has moved
        if (MouseState.ScrollDelta.Y != 0)
        {
            // zoom
            
            var yDelta = -MouseState.ScrollDelta.Y;
            _scrollPos += yDelta;
            
            var scale = (decimal)Math.Pow(1.1, -_scrollPos) * 1024;
            
            const decimal min = 400m / ~(WorldHeight == 64 ? 0 : ~0x0uL << WorldHeight);
            const decimal max = 32m;
            
            switch (scale)
            {
                case < min:
                    _scrollPos --;
                    break;
                case > max:
                    _scrollPos ++;
                    break;
                default:
                    newScale = scale;
                    break;
            }
        }
        
        // mouse and keyboard controls
        
        // update the brush range
        _brushRange = new Range2D(
            Math.Min(mouseWorldCoords.X, _leftMouseWorldCoords.X),
            Math.Min(mouseWorldCoords.Y, _leftMouseWorldCoords.Y),
            Math.Max(mouseWorldCoords.X, _leftMouseWorldCoords.X),
            Math.Max(mouseWorldCoords.Y, _leftMouseWorldCoords.Y)
        );
        
        // if we are not holding lmouse, set the _leftMouseWorldCoords to the mouse pos
        if (!(MouseState.IsButtonDown(MouseButton.Left) && KeyboardState.IsKeyDown(Keys.LeftShift)))
        {
            _leftMouseWorldCoords = mouseWorldCoords;
        }
        
        // if we released lmouse while holding lshift, place the _activeBrush in the _world in the _brushRange
        if (MouseState.IsButtonReleased(MouseButton.Left) && KeyboardState.IsKeyDown(Keys.LeftShift))
        {
            WorldModifications.Add(new WorldModification(_brushRange, _activeBrush));
        }
        
        // if we are currently holding lmouse and are not holding lshift, place the brush in the world
        if (MouseState.IsButtonDown(MouseButton.Left) && !KeyboardState.IsKeyDown(Keys.LeftShift))
        {
            WorldModifications.Add(new WorldModification(_brushRange, _activeBrush));
            
            if (!KeyboardState.IsKeyDown(Keys.LeftShift))
                _leftMouseWorldCoords = mouseWorldCoords;
        }
        
        // if we pressed mmouse, set the _middleMouseScreenCoords to the mouse pos
        if (MouseState.IsButtonPressed(MouseButton.Middle))
        {
            _middleMouseScreenCoords = mouseScreenCoords;
            _prevTranslation = Translation;
        }
        
        // if we are currently holding mmouse, set the translation of the world
        if (MouseState.IsButtonDown(MouseButton.Middle))
        {
            var worldTranslationOffset = (Vec2<decimal>)(_middleMouseScreenCoords - mouseScreenCoords) / Scale;
            
            worldTranslationOffset = (worldTranslationOffset.X, -worldTranslationOffset.Y);
            
            var nextTranslation = _prevTranslation + worldTranslationOffset;

            if (Translation != nextTranslation)
            {
                newTranslation = nextTranslation;
            }
        }

        if (MouseState.IsButtonPressed(MouseButton.Right))
        {
            _brushId = (_brushId + 64) % (2 << 24);
            
            _activeBrush = new Paint((uint)(Random.Next() & 0x00ffffff));
            
            Log($"Brush Changed to #{(_activeBrush.Tile.Data & 0x00ffffff):X6}");
        }
        
        
        if (KeyboardState.IsKeyPressed(Keys.LeftControl))
        {
            Log($"Mouse Position: {mouseWorldCoords}");
        }

        Scale = newScale;
        Translation = newTranslation;
    }
    
    
    /// <summary>
    /// Initializes everything on the render thread
    /// </summary>
    protected override void OnLoad()
    {
        Log("===============[GRAPHICS LOADING]===============", OutputSource.Load);
        
        base.OnLoad();
        
        // set clear color
        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        
        // create all the shaders
        Shaders.Instantiate();
        
        // create the textures
        Textures.Instantiate();
        
        // create the renderables. must be done after creating the shaders
        Renderables.Instantiate();

        _rQt = new QuadtreeRenderable(Shaders.Qtr, BufferUsageHint.StreamDraw);
        _rFont = new FontRenderable(Shaders.Font, BufferUsageHint.DynamicDraw);
        
        // create the GUIs. must be done after creating the renderables
        GuiManager.Instantiate();
        
        // set translations
        _translation = -InitialScreenSize / 2;
        _prevTranslation = _translation;
        
        // start the game logic
        GameManager.SetRunning(true);
        
        
        Log("===============[ BEGIN PROGRAM  ]===============", OutputSource.Load);
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
        
        // update screenSize on the logic thread
        GameManager.UpdateScreenSize(new Vec2<int>(e.Width, e.Height));
        
        // Update the GUIs, since the vertex coords are calculated on creation,
        // and need to be updated with the new screen size
        GuiManager.UpdateGuis();
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

    // public setters/getters
    
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
    /// Updates the geometry on the render thread.
    /// </summary>
    /// <param name="modifications">the modifications to the world since the last time this method was called</param>
    /// <param name="treeLength">the length of the "tree" section</param>
    /// <param name="dataLength">the length of the "data" section</param>
    /// <param name="renderRoot">the root node for rendering</param>
    /// <param name="renderRange">the dimensions of <paramref name="renderRoot"/></param>
    /// <remarks>Causes the quadtree to be partially re-uploaded to the gpu.</remarks>
    public static void UpdateGeometry(QuadtreeModifications<ITile> modifications, int treeLength, int dataLength, QuadtreeNode renderRoot, Range2D renderRange)
    {
        if (!GeometryLock.IsSet && !GeometryLock.Wait(RenderLockTimeout))
        {
            // if we failed to get access in time but the new treeLength is significantly smaller, keep waiting
            if (treeLength * 1.5 < _treeLength)
                GeometryLock.Wait(Timeout.Infinite);
            else return;
        }
        GeometryLock.Reset();
        
        _modifications = modifications;
        _treeLength = treeLength;
        _dataLength = dataLength;
        _renderRoot = renderRoot;
        _renderRange = renderRange;
        
        _unuploadedGeometry = true;
        
        GeometryLock.Set();
    }
    
    /// <summary>
    /// Checks whether new geometry can be uploaded. Will wait for <see cref="Constants.RenderLockTimeout"/> to get a lock.
    /// </summary>
    public static bool CanUpdateGeometry()
    {
        if (!GeometryLock.IsSet && !GeometryLock.Wait(RenderLockTimeout)) return false;
        GeometryLock.Reset();
        
        var val = !_unuploadedGeometry;
        
        GeometryLock.Set();
        
        return val;
    }
    
    /// <summary>
    /// Updates the MSPT metric.
    /// </summary>
    /// <param name="mspt">the new mpst</param>
    public static void UpdateMspt(float mspt)
    {
        _mspt = mspt;
    }

    public override void Dispose()
    {
        GC.SuppressFinalize(this);
        
        GeometryLock.Dispose();
        WorldModifications.Clear();
        
        base.Dispose();
    }
}

public readonly struct WorldModification(Range2D range, ITile tile)
{
    public readonly Range2D Range = range;
    public readonly ITile Tile = tile;
}