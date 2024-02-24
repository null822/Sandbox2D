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
using Sandbox2D.World;
using Sandbox2D.World.TileTypes;
using static Sandbox2D.Util;
using static Sandbox2D.Constants;

namespace Sandbox2D;

public class RenderManager(int width, int height, string title) : GameWindow(GameWindowSettings.Default,
    new NativeWindowSettings { ClientSize = (width, height), Title = title })
{
    // rendering
    private static QuadTreeStruct[] _linearQuadTree = [];
    private static float _scale;
    private static Vec2<float> _translation;
    private static bool _worldUpdatedSinceLastFrame = true;

    // world editing
    private static ITile _activeBrush;
    private static uint _brushId;
    private static Range2D _brushRange;
    private static Vec2<long> _leftMouseWorldCoords = new(0);
    private static Vec2<int> _middleMouseScreenCoords = new(0);

    public static (WorldAction action, string arg)? WorldAction { get; private set; }
    public static readonly List<KeyValuePair<Range2D, ITile>> WorldModifications = [];

    private static float _tps;
    
    // controls
    private static float _scrollPos = 1;
    private static Vec2<decimal> _prevTranslation = new(0);
    
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

        // render world (with off-screen culling)
        
        ref var pt = ref Renderables.Pt;
        
        // reupload the world to the gpu if needed
        if (_worldUpdatedSinceLastFrame && GameManager.IsRunning)
        {
            // reset the world geometry, update the transform, and set the new geometry
            Renderables.ResetGeometry(RenderableCategory.Pt);
            pt.SetGeometry(_linearQuadTree);
            
            // reset worldUpdated flag
            _worldUpdatedSinceLastFrame = false;
        }
        
        // update the world transform
        pt.SetTransform(_translation, _scale);
        
        // render the world
        pt.Render();
        
        // TODO: render brush outline
        
        
        // render the GUIs
        GuiManager.UpdateGuis();
        Renderables.Render(RenderableCategory.Gui);
        
        
        ref var renderable = ref Renderables.Font;
        var center = GameManager.ScreenSize / 2;
        
        // FPS display
        renderable.SetText($"{1 / args.Time:F1} FPS, {_tps:F1} TPS", -center + (0,10), 1f, false);
        
        renderable.UpdateVao();
        renderable.Render();
        renderable.ResetGeometry();
        
        // Mouse Coordinate Display
        renderable.SetText(ScreenToWorldCoords((Vec2<int>)MousePosition).ToString(), -center + (0,30), 1f, false);
        
        renderable.UpdateVao();
        renderable.Render();
        renderable.ResetGeometry();
        
        // swap buffers
        SwapBuffers();
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
    }
    
    /// <summary>
    /// Handles all of the controls of the game.
    /// </summary>
    private void UpdateControls()
    {
        //TODO: make this method less unreadable and unmaintainable
        
        var mouseScreenCoords = new Vec2<int>((int)Math.Floor(MouseState.X), (int)Math.Floor(MouseState.Y));
        var mouseWorldCoords = ScreenToWorldCoords(mouseScreenCoords);
        
        var center = GameManager.ScreenSize / 2;
        
        if (KeyboardState.IsKeyPressed(Keys.Escape))
        {
            // pause the game
            GameManager.SetRunning(!GameManager.IsRunning);
            
            // show pause gui
            GuiManager.SetVisibility(0, GameManager.IsRunning);
        }
        
        if (!GameManager.IsRunning)
        {
            // if the game is paused, update only the pause menu
            GuiManager.MouseOver(mouseScreenCoords - center, 0);
            return;
        }

        
        // update GUIs
        GuiManager.MouseOver(mouseScreenCoords - center);
        
        
        #region keyboard-only controls
        
        // mapping/saving/loading/clearing
        if (KeyboardState.IsKeyPressed(Keys.M))
            WorldAction = (Sandbox2D.WorldAction.Map, "map.svg");
        
        else if (KeyboardState.IsKeyPressed(Keys.S))
            WorldAction = (Sandbox2D.WorldAction.Save, "save.qdt");
        
        else if (KeyboardState.IsKeyPressed(Keys.L))
            WorldAction = (Sandbox2D.WorldAction.Load, "save.qdt");
        
        else if (KeyboardState.IsKeyPressed(Keys.C))
            WorldAction = (Sandbox2D.WorldAction.Clear, "");
        
        #endregion

        
        var newScale = GameManager.Scale;
        var newTranslation = GameManager.Translation;
        
        // if the scroll wheel has moved
        if (MouseState.ScrollDelta.Y != 0)
        {
            // zoom
            
            var yDelta = -MouseState.ScrollDelta.Y;
            _scrollPos += yDelta;
            
            var scale = (decimal)Math.Pow(1.1, -_scrollPos) * 1024;
            
            const decimal min = 400m / (0x1uL << WorldDepth);
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
            
            _worldUpdatedSinceLastFrame = true;
        }
        
        // mouse and keyboard controls
        
        // create the brush range, subtracting 1 from the max values if they are not already long.MaxValue
        var minX = Math.Min(mouseWorldCoords.X, _leftMouseWorldCoords.X);
        var minY = Math.Min(mouseWorldCoords.Y, _leftMouseWorldCoords.Y);
        var maxX = Math.Max(mouseWorldCoords.X, _leftMouseWorldCoords.X);
        var maxY = Math.Max(mouseWorldCoords.Y, _leftMouseWorldCoords.Y);
        maxX = maxX == long.MaxValue ? maxX : maxX - 1;
        maxY = maxY == long.MaxValue ? maxY : maxY - 1;
        
        const long worldRadius = 0x1L << (WorldDepth - 1);
        
        // set the brushRange, clamping it within the world size
        _brushRange = new Range2D(
            Math.Clamp(minX, -worldRadius, worldRadius),
            Math.Clamp(minY, -worldRadius, worldRadius),
            Math.Clamp(maxX, -worldRadius, worldRadius),
            Math.Clamp(maxY, -worldRadius, worldRadius)
        );
        
        // if we released lmouse while holding lshift, place the _activeBrush in the _world in the _brushRange
        if (MouseState.IsButtonReleased(MouseButton.Left) && KeyboardState.IsKeyDown(Keys.LeftShift))
        {
            WorldModifications.Add(new KeyValuePair<Range2D, ITile>(_brushRange, _activeBrush));
        }
        
        // if we are not currently holding lmouse or not holding lshift, set the _leftMouseWorldCoords to the mouse pos
        if (!MouseState.IsButtonDown(MouseButton.Left) && !KeyboardState.IsKeyDown(Keys.LeftShift))
        {
            _leftMouseWorldCoords = mouseWorldCoords;
        }
        
        // if we are currently holding lmouse and are not holding lshift, place the brush in the world
        if (MouseState.IsButtonDown(MouseButton.Left) && !KeyboardState.IsKeyDown(Keys.LeftShift))
        {
            // technically buggy behaviour, but creates cool looking, "smooth" lines
            WorldModifications.Add(new KeyValuePair<Range2D, ITile>(_brushRange, _activeBrush));
            _leftMouseWorldCoords = mouseWorldCoords;
            
        }
        
        // if we just pressed lmouse, set the _leftMouseWorldCoords to the mouse pos
        if (MouseState.IsButtonPressed(MouseButton.Left))
        {
            _leftMouseWorldCoords = mouseWorldCoords;
        }
        
        // if we pressed mmouse, set the _middleMouseScreenCoords to the mouse pos
        if (MouseState.IsButtonPressed(MouseButton.Middle))
        {
            _middleMouseScreenCoords = mouseScreenCoords;
            _prevTranslation = GameManager.Translation;
        }
        
        // if we are currently holding mmouse, set the translation of the world
        if (MouseState.IsButtonDown(MouseButton.Middle))
        {
            var worldTranslationOffset = (Vec2<decimal>)(_middleMouseScreenCoords - mouseScreenCoords) / GameManager.Scale;
            
            worldTranslationOffset = (worldTranslationOffset.X, -worldTranslationOffset.Y);
            
            var nextTranslation = _prevTranslation + worldTranslationOffset;

            if (GameManager.Translation != nextTranslation)
            {
                newTranslation = nextTranslation;
                _worldUpdatedSinceLastFrame = true;
            }
        }

        if (MouseState.IsButtonPressed(MouseButton.Right))
        {
            _brushId = (_brushId + 1) % 3;

            _activeBrush = _brushId switch
            {
                0 => new Air(),
                1 => new Stone(),
                2 => new Dirt(),
                _ => new Air()
            };
                
            
            Log($"Brush Changed to {_activeBrush.Name}");
        }
        
        
        if (KeyboardState.IsKeyPressed(Keys.LeftControl))
        {
            Log($"Mouse Position: {mouseWorldCoords}");
            _worldUpdatedSinceLastFrame = true;
        }
        
        // calculate renderScale
        var renderScale = (decimal)GameManager.RenderRange.Width / (0x1 << RenderDepth);
        
        // calculate scale/translation
        _scale = (float)(GameManager.Scale * renderScale);
        _translation = (GameManager.Translation - (Vec2<decimal>)GameManager.RenderRange.Center) / renderScale;
        
        // update the transform for the logic
        GameManager.SetTransform(newScale, newTranslation);
    }
    
    
    /// <summary>
    /// Initializes everything on the render thread
    /// </summary>
    protected override void OnLoad()
    {
        Prog("===============[GRAPHICS LOADING]===============");
        
        base.OnLoad();
        
        // set clear color
        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        
        // create all of the shaders
        Shaders.Instantiate();
        
        // create the textures
        Textures.Instantiate();
        
        // create the renderables. must be done after creating the shaders
        Renderables.Instantiate();
        
        // create the GUIs. must be done after creating the renderables
        GuiManager.Instantiate();
        
        
        // start the game logic
        GameManager.SetRunning(true);
        
        Prog("===============[ BEGIN PROGRAM  ]===============");
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
        
        base.OnClosing(e);
    }
    
    // public setters
    
    /// <summary>
    /// Resets the world modifications on the render thread. Run once after each logic tick.
    /// </summary>
    public static void ResetWorldModifications()
    {
        WorldAction = null;
        WorldModifications.Clear();
    }
    
    /// <summary>
    /// Updates the Linear Quad Tree on the render thread.
    /// </summary>
    /// <param name="lqt">the new Linear Quad Tree</param>
    /// <param name="scale">the new scale</param>
    /// <param name="translation">the new translation</param>
    /// <remarks>Causes the world to be re-uploaded to the gpu.</remarks>
    public static void UpdateLqt(QuadTreeStruct[] lqt, float scale, Vec2<float> translation)
    {
        _linearQuadTree = lqt;

        _scale = scale;
        _translation = translation;

        _worldUpdatedSinceLastFrame = true;
    }
    
    /// <summary>
    /// Updates the TPS metric.
    /// </summary>
    /// <param name="tps">the new tps</param>
    public static void UpdateTps(float tps)
    {
        _tps = tps;
    }
    
}
