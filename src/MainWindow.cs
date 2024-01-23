using System;
using System.IO;
using System.Text;
using System.Threading;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Sandbox2D.Graphics.Registry;
using Sandbox2D.Graphics.Renderables;
using Sandbox2D.GUI;
using Sandbox2D.Maths;
using Sandbox2D.Maths.QuadTree;
using Sandbox2D.World;
using Sandbox2D.World.TileTypes;
using static Sandbox2D.Util;
using static Sandbox2D.Constants;

namespace Sandbox2D;

public class MainWindow : GameWindow
{
    // world
    private QuadTree<IBlockMatrixTile> _world;
    
    // world editing
    private IBlockMatrixTile _activeBrush;
    private uint _brushId;
    private static Range2D _brushRange;
    private static Vec2<long> _leftMouseWorldCoords = new(0);
    private Vec2<int> _middleMouseScreenCoords = new(0);
    private bool _worldUpdatedSinceLastFrame = true;

    // camera position
    private static float _scaleBase = 1;
    private static float _scale = 1;
    private static Vec2<decimal> _translation = new(0);
    private static Vec2<decimal> _prevTranslation = new(0);

    private readonly string _savePath;
    private bool _isPaused;
    
    public MainWindow(int width, int height, string title, string savePath = null) : base(GameWindowSettings.Default,
    new NativeWindowSettings { ClientSize = (width, height), Title = title })
    {
        _savePath = savePath;
    }

    private static void SystemChecks()
    {
        Log("log text");
        Debug("debug text");
        Warn("warn text");
        Error("error text");
        
        var r1 = new Range2D(-2, -2, 2, 4);
        var r2 = new Range2D(0, 1, 3, 3);
        
        Debug(r1.Overlap(r2));
        
        Debug(new Vec2<int>(10, 2) + new Vec2<int>(11, 1));
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        // only render when the game is active
        if (!CheckActive())
        {
            Thread.Sleep(CheckActiveDelay);
            return;
        }
        
        
        // clear the color buffer
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        // render components (with off-screen culling)
        
        // if the world or translation/scale (for culling reasons) has updated and the game is not paused,
        // reupload the world to the gpu
        if (_worldUpdatedSinceLastFrame && !_isPaused)
        {
            // update the translation and scale
            TileRenderable.SetTransform(_translation, _scale);
            
            // reset the world geometry
            // TileRenderableManager.ResetGeometry();
            Renderables.ResetGeometry(RenderableCategory.Tile);

            // get the world pos of the top left and bottom right corners of the screen,
            // with a small buffer to prevent culling things still partially within the frame
            var tlScreen = ScreenToWorldCoords((0, ClientSize.Y)) - new Vec2<long>(64);
            var brScreen = ScreenToWorldCoords((ClientSize.X, 0)) + new Vec2<long>(64);
            var screenRange = new Range2D(tlScreen, brScreen);
            
            // add all the elements within the range (on screen) to the renderable
            _world.InvokeLeaf(screenRange, (tile, range) =>
            {
                tile.AddToRenderable(range);
                
                return true;
            }, ResultComparisons.Or, true);
            
            // reset worldUpdated flag
            _worldUpdatedSinceLastFrame = false;
        }
        
        // render all of the tile renderables
        Renderables.Render(RenderableCategory.Tile);
        
        
        // TODO: render brush outline
        
        
        // Text test
        ref var renderable = ref Renderables.Font;
        
        var center = GetScreenSize() / 2;
        
        renderable.SetText((1 / args.Time).ToString("0.0 FPS"), -center + (30,10), 1f);
        
        
        renderable.UpdateVao();
        renderable.Render();
        renderable.ResetGeometry();
        
        // render the GUIs
        GuiManager.UpdateGuis();
        Renderables.Render(RenderableCategory.Gui);
        
        
        
        // swap buffers
        SwapBuffers();
    }

    /// <summary>
    /// Initializes everything
    /// </summary>
    protected override void OnLoad()
    {
        Log("===============[   LOADING   ]===============");

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
        
        // create tiles. must be done after creating the renderables
        Tiles.Instantiate(new ITile[]
        {
            new Air(),
            new Stone(),
            new Dirt()
        });
        
        // create the world. must be done after creating the tiles
        if (_savePath != null)
        {
            var save = File.Open(_savePath, FileMode.Open);

            _world = QuadTree<IBlockMatrixTile>.Deserialize(save);
        }
        else
        {
            _world = new QuadTree<IBlockMatrixTile>(new Air(), WorldDepth);
        }
        
        
        // run the system checks

        Log("===============[SYSTEM CHECKS]===============");
        SystemChecks();
        
        Log("===============[BEGIN PROGRAM]===============");

    }

    /// <summary>
    /// The game logic loop
    /// </summary>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (!CheckActive())
        {
            Thread.Sleep(CheckActiveDelay);
            return;
        }
        
        var mouseScreenCoords = (Vec2<int>)MousePosition;
        var mouseWorldCoords = ScreenToWorldCoords(mouseScreenCoords);
        var center = GetScreenSize() / 2;
        
        if (KeyboardState.IsKeyPressed(Keys.Escape))
        {
            _isPaused = !_isPaused;
            GuiManager.SetVisibility(0, _isPaused);
        }
        
        
        if (_isPaused)
        {
            // if the game is paused, update only the pause menu
            GuiManager.MouseOver(mouseScreenCoords - center, 0);
            return;
        }
        
        // update GUIs
        GuiManager.MouseOver(mouseScreenCoords - center);

        
        // keyboard-only controls
        
        // mapping/saving/loading/clearing
        if (KeyboardState.IsKeyPressed(Keys.M))
        {
            var svgMap = _world.GetSvgMap().ToString();
            
            var map = File.Create("BlockMatrixMap.svg");
            map.Write(Encoding.ASCII.GetBytes(svgMap));
            map.Close();
            
            Log("QuadTree Map Saved");
        }
        if (KeyboardState.IsKeyPressed(Keys.S))
        {
            var save = File.Create("save.qdt");

            _world.Serialize(save);

            save.Close();
            Log("QuadTree Saved");
        }
        if (KeyboardState.IsKeyPressed(Keys.L))
        {
            var save = File.Open("save.qdt", FileMode.Open);

            _world = QuadTree<IBlockMatrixTile>.Deserialize(save);
            
            save.Close();
            
            _worldUpdatedSinceLastFrame = true;

            Log("QuadTree Loaded");

        }
        if (KeyboardState.IsKeyPressed(Keys.C))
        {
            _world = new QuadTree<IBlockMatrixTile>(
                new Air(),
                WorldDepth);

            _worldUpdatedSinceLastFrame = true;
            
            Log("World Cleared");
        }
        
        // zoom
        _scaleBase += MouseState.ScrollDelta.Y / 256f;
        _scaleBase = float.Clamp(_scaleBase, 0.00390625f, 10f);
        
        var newScale = (float)Math.Pow(_scaleBase, 8);
        
        // if the scale has changed
        if (MouseState.ScrollDelta.Y != 0)
        {
            // update _scale, and set the _worldUpdatedSinceLastFrame flag for culling reasons
            _scale = newScale;
            _worldUpdatedSinceLastFrame = true;
        }
        
        // Log("====[Scale/Translation]====");
        // Log(_scale);
        // Log(_translation);
        
        
        // mouse and keyboard controls
        
        // Log(mouseWorldCoords);

        // create the brush range, subtracting 1 from the max values if they are not already long.MaxValue
        var minX = Math.Min(mouseWorldCoords.X, _leftMouseWorldCoords.X);
        var minY = Math.Min(mouseWorldCoords.Y, _leftMouseWorldCoords.Y);
        var maxX = Math.Max(mouseWorldCoords.X, _leftMouseWorldCoords.X);
        var maxY = Math.Max(mouseWorldCoords.Y, _leftMouseWorldCoords.Y);
        maxX = maxX == long.MaxValue ? maxX : maxX - 1;
        maxY = maxY == long.MaxValue ? maxY : maxY - 1;
        
        const int worldSize = 0x1 << WorldDepth;
        
        // set the brushRange, clamping it within the world size
        _brushRange = new Range2D(
            Math.Clamp(minX, -worldSize, worldSize),
            Math.Clamp(minY, -worldSize, worldSize),
            Math.Clamp(maxX, -worldSize, worldSize),
            Math.Clamp(maxY, -worldSize, worldSize)
        );
        
        // if we released lmouse while holding lshift, place the _activeBrush in the _world in the _brushRange
        if (MouseState.IsButtonReleased(MouseButton.Left) && KeyboardState.IsKeyDown(Keys.LeftShift))
        {
            _world[_brushRange] = _activeBrush;
            _worldUpdatedSinceLastFrame = true;
            
            Log($"Placed at {_brushRange}");
        }
        
        // if we are not currently holding lmouse or not holding lshift, set the _leftMouseWorldCoords to the mouse pos
        if (!MouseState.IsButtonDown(MouseButton.Left) || !KeyboardState.IsKeyDown(Keys.LeftShift))
        {
            _leftMouseWorldCoords = mouseWorldCoords;
        }
        
        // if we are currently holding lmouse and are not holding lshift, place the brush in the world
        if (MouseState.IsButtonDown(MouseButton.Left) && !KeyboardState.IsKeyDown(Keys.LeftShift))
        {
            // technically buggy behaviour, but creates cool looking, "smooth" lines
            
            Log($"Replaced {_world[_brushRange.BottomLeft]}");
            
            _world[_brushRange] = _activeBrush;
            _worldUpdatedSinceLastFrame = true;
            
            Log($"Placed at {_brushRange}");
            
        }
        
        // if we pressed lmouse, set the _leftMouseWorldCoords to the mouse pos
        if (MouseState.IsButtonPressed(MouseButton.Left))
        {
            _leftMouseWorldCoords = mouseWorldCoords;
        }
        
        // if we pressed mmouse, set the _middleMouseScreenCoords to the mouse pos
        if (MouseState.IsButtonPressed(MouseButton.Middle))
        {
            _middleMouseScreenCoords = mouseScreenCoords;
            _prevTranslation = _translation;
        }
        
        // if we are currently holding mmouse, set the translation of the world
        if (MouseState.IsButtonDown(MouseButton.Middle))
        {
            var worldTranslationOffset = (Vec2<decimal>)((Vec2<double>)(mouseScreenCoords - _middleMouseScreenCoords) / _scale);

            worldTranslationOffset = (worldTranslationOffset.X, -worldTranslationOffset.Y);

            var newTranslation = _prevTranslation + worldTranslationOffset;

            if (_translation != newTranslation)
            {
                _translation = newTranslation;
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
        
    }
    

    /// <summary>
    /// Returns true if the specified rectangle intersects with any tile.
    /// </summary>
    /// <param name="rectangle">The rectangle to check for an intersection</param>
    private bool TileIntersect(Range2D rectangle)
    {
        var retValue = _world.Invoke(rectangle,
            (_, pos) => GetCollisionRectangle(pos).Overlaps(rectangle), ResultComparisons.Or, true);
        return retValue;
    }
    
    /// <summary>
    /// Returns a rectangle representing the collision of a tile based on its position
    /// </summary>
    /// <param name="pos">the position of the tile to get the collision of</param>
    private static Range2D GetCollisionRectangle(Vec2<long> pos)
    {
        return new Range2D(
            pos.X,
            pos.Y,
            pos.X + 1,
            pos.Y + 1
        );
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
    /// Update the viewport when the window is resized
    /// </summary>
    /// <param name="e"></param>
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);

        // re-render the world
        _worldUpdatedSinceLastFrame = true;
        
        // Update the GUIs, since the vertex coords are calculated on creation,
        // and need to be updated with the new screen size
        GuiManager.UpdateGuis();
    }
    
    
    // public getters

    /// <summary>
    /// Returns zoom scale multiplier.
    /// </summary>
    public static float GetScale()
    {
        return _scale;
    }

    /// <summary>
    /// Returns the translation (pan) of the world.
    /// </summary>
    public static Vec2<decimal> GetTranslation()
    {
        return _translation;
    }

    /// <summary>
    /// Returns the screen size.
    /// </summary>
    public Vec2<int> GetScreenSize()
    {
        return (Vec2<int>)ClientSize;
    }
    
}
