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
    private bool _temporaryBrushToggle;
    private static Range2D _brushRange;
    private static Vec2<long> _leftMouseWorldCoords = new(0);
    private Vec2<int> _middleMouseScreenCoords = new(0);
    private bool _worldUpdatedSinceLastFrame = true;

    // camera position
    private static float _scaleBase = 1;
    private static float _scale = 1;
    private static Vec2<decimal> _translation = new(0);
    private static Vec2<decimal> _prevTranslation = new(0);
    private static Vec2<int> _gridSize;
    
    public MainWindow(int width, int height, string title) : base(GameWindowSettings.Default,
    new NativeWindowSettings { ClientSize = (width, height), Title = title })
    {
        
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

        Debug("===========================================");

        var qt = new QuadTree<IBlockMatrixTile>(new Air(), 8);
        
        qt[92, -42] = new Stone();
        
        Debug(qt[092, -42] ?? new Dirt());
        // Debug(qt[237, 432] ?? new Dirt());
        
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        if (!CheckActive())
        {
            Thread.Sleep(CheckActiveDelay);
            return;
        }

        base.OnRenderFrame(args);
        
        // only run when focused
        if (!IsFocused)
            return;
        
        // clear the color buffer
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        // render components (with off-screen culling)
        const uint renderableId = 2;
        
        var renderable = (GameObjectRenderable)Renderables.Get(renderableId);

        // temporarily disabled if statement, for debugging
        
        // if the world or translation/scale (for culling reasons) has updated, reupload the world to the gpu
        if (_worldUpdatedSinceLastFrame || Math.Abs(renderable.GetScale() - _scale) > float.Epsilon || renderable.GetTranslation() != _translation)
        {
            // update scale/translation
            renderable.SetTransform(_translation, _scale);

            // get the world pos of the top left and bottom right corners of the screen,
            // with a small buffer to prevent culling things still partially within the frame
            var tlScreen = ScreenToWorldCoords((0, ClientSize.Y)) - new Vec2<long>(64);
            var brScreen = ScreenToWorldCoords((ClientSize.X, 0)) + new Vec2<long>(64);
            var screenRange = new Range2D(tlScreen, brScreen);
            
            // reset renderable geometry
            renderable.ResetGeometry();


            // add all the elements within the range (on screen) to the renderable
            _world.InvokeLeaf(screenRange, (tile, range) =>
            {
                tile.AddToRenderable(range, renderableId);
                
                return true;
            }, ResultComparisons.Or, true);

            // update the VAO
            renderable.UpdateVao();
            
            // reset worldUpdated flag
            _worldUpdatedSinceLastFrame = false;
        }
        
        // render the world
        renderable.Render();
        
        
        // TODO: render brush outline
        
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
        
        // create all of the renderables. must be done after creating the shaders
        Renderables.Instantiate();
        
        Tiles.Instantiate(new ITile[]
        {
            new Air(),
            new Stone(),
            new Dirt()
        });
        
        // create the world. must be done after creating the renderables
        _world = new QuadTree<IBlockMatrixTile>(new Air(), WorldDepth);
        
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
            var save = File.Create("save.qt");

            _world.Serialize(save);

            save.Close();
            Log("QuadTree Saved");
        }
        if (KeyboardState.IsKeyPressed(Keys.L))
        {
            var save = File.Open("save.bm", FileMode.Open);

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
        
        _scale = (float)Math.Pow(_scaleBase, 8);
        
        // Log("====[Scale/Translation]====");
        // Log(_scale);
        // Log(_translation);
        
        
        // mouse and keyboard controls
        
        var mouseScreenCoords = (Vec2<int>)MousePosition;
        var mouseWorldCoords = ScreenToWorldCoords(mouseScreenCoords);
        
        // Log(mouseWorldCoords);

        // create the brush range, adding 1 to the max values if they are not already long.MaxValue
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
        
        // if we released lmouse, place the _activeBrush in the _world in the _brushRange
        if (MouseState.IsButtonReleased(MouseButton.Left))
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
        
        // if we are currently holding lmouse and not holding lshift, place the brush in the world
        if (MouseState.IsButtonDown(MouseButton.Left) && !KeyboardState.IsKeyDown(Keys.LeftShift))
        {
            // technically buggy behaviour, but creates cool looking, "smooth" lines
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

            _translation = _prevTranslation + worldTranslationOffset;

            // _translation = (_translation.X, -_translation.Y);
        }

        if (MouseState.IsButtonPressed(MouseButton.Right))
        {
            _temporaryBrushToggle = !_temporaryBrushToggle;

            _activeBrush = _temporaryBrushToggle ? new Stone() : new Air();
            
            Log($"Brush Changed to {_activeBrush.Name}");
        }
        
        
        _gridSize = WorldToScreenCoords(new Vec2<long>(0, 0)) - WorldToScreenCoords(new Vec2<long>(1, 1));
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
    
    /// <summary>
    /// Returns the size of a 1x1 in-world area in screen coordinates
    /// </summary>
    public static Vec2<int> GetGridSize()
    {
        return _gridSize;
    }
    
}
