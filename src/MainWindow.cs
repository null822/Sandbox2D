using System;
using System.IO;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Sandbox2D.Graphics.Registry;
using Sandbox2D.Graphics.Renderables;
using Sandbox2D.Maths;
using Sandbox2D.Maths.BlockMatrix;
using Sandbox2D.World;
using Sandbox2D.World.TileTypes;
using static Sandbox2D.Util;
using static Sandbox2D.Constants;

namespace Sandbox2D;

public class MainWindow : GameWindow
{
    // world
    private BlockMatrix<IBlockMatrixTile> _world;
    
    // world editing
    private IBlockMatrixTile _activeBrush;
    private bool _temporaryBrushToggle;
    private static Range2D _brushRange;
    private static Vec2<long> _leftMouseWorldCoords = new(0);
    private Vec2<int> _middleMouseScreenCoords = new(0);

    // camera position
    private static float _scaleBase = 1;
    private static float _scale = 1;
    private static Vec2<double> _translation = new(0);
    private static Vec2<double> _prevTranslation = new(0);
    private static Vec2<int> _gridSize;
    
    public MainWindow(int width, int height, string title) : base(GameWindowSettings.Default,
    new NativeWindowSettings { ClientSize = (width, height), Title = title })
    {
        // "System Checks"
        
        Debug("===============[SYSTEM CHECKS]===============");
        
        Log("log text");
        Debug("debug text");
        Warn("warn text");
        Error("error text");
        
        var r1 = new Range2D(-2, -2, 2, 4);
        var r2 = new Range2D(0, 1, 3, 3);
        
        Debug(r1.Overlap(r2));
        
        Debug(new Vec2<int>(10, 2) + new Vec2<int>(11, 1));
        
        // Debug((IBlockMatrixTile)new Air() == new Stone());
        
        Debug("===============[BEGIN PROGRAM]===============");
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        // only run when focused
        if (!IsFocused)
            return;
        
        // clear the color buffer
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        // [old] shouldn't be a problem anymore
        // _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        
        // render components (with off-screen culling)
        
        // get the world pos of the top left and bottom right corners of the screen,
        // with a small buffer to prevent culling things still partially within the frame
        var tlScreen = ScreenToWorldCoords(new Vec2<int>(0, 0)) - new Vec2<long>(64);
        var brScreen = ScreenToWorldCoords((Vec2<int>)ClientSize) + new Vec2<long>(64);
        var screenRange = new Range2D(tlScreen, brScreen);

        const uint renderableId = 2;
        
        var renderable = (GameObjectRenderable)Renderables.Get(renderableId);
        
        renderable.SetScale(_scale);
        renderable.SetTranslation(_translation);
        
        renderable.ResetGeometry();
        
        
        // add all the elements within the range (on screen) to the renderable
        _world.InvokeRangedBlock(screenRange, (tile, range) =>
        {
            tile.AddToRenderable(range, renderableId);
            
            return true;
        }, ResultComparisons.Or, true);
        
        // re-get the renderable after the new geometry has been set, since we have the object by-value and it is out of data
        renderable = (GameObjectRenderable)Renderables.Get(renderableId);

        
        // update the VAO and render
        renderable.UpdateVao();
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
        base.OnLoad();
        
        // set clear color
        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        
        // create all of the shaders
        Shaders.Instantiate();
        
        // create all of the renderables. must be done after creating the shaders
        Renderables.Instantiate();
        
        Tiles.Initialize(new ITile[]
        {
            new Air(),
            new Stone(),
            new Dirt()
        });
        
        // create the world. must be done after creating the renderables
        _world = new BlockMatrix<IBlockMatrixTile>(new Air(), new Vec2<long>(WorldWidth, WorldHeight));
        
    }

    /// <summary>
    /// The game logic loop
    /// </summary>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        
        // only run when hovered
        if (MousePosition.X < 0 || MousePosition.X > ClientSize.X || MousePosition.Y < 0 || MousePosition.Y > ClientSize.Y)
            return;
        
        // only run when focused
        if (!IsFocused)
            return;
        
        // keyboard-only controls
        
        // mapping/saving/loading/clearing
        if (KeyboardState.IsKeyPressed(Keys.M))
        {
            var svgMap = _world.GetSvgMap().ToString();
            
            var map = File.Create("BlockMatrixMap.svg");
            map.Write(Encoding.ASCII.GetBytes(svgMap));
            map.Close();
            
            Log("BlockMatrix Map Saved");
        }
        if (KeyboardState.IsKeyPressed(Keys.S))
        {
            var save = File.Create("save.bm");

            _world.Serialize(save);

            save.Close();
            Log("BlockMatrix Saved");
        }
        if (KeyboardState.IsKeyPressed(Keys.L))
        {
            var save = File.Open("save.bm", FileMode.Open);

            _world = BlockMatrix<IBlockMatrixTile>.Deserialize(save);
            
            save.Close();
            Log("BlockMatrix Loaded");

        }
        if (KeyboardState.IsKeyPressed(Keys.C))
        {
            _world = new BlockMatrix<IBlockMatrixTile>(
                new Air(),
                new Vec2<long>(WorldWidth, WorldHeight));
            
            Log("World Cleared");
        }
        
        // zoom
        _scaleBase = float.Clamp(_scaleBase + MouseState.ScrollDelta.Y / 16f, 0.1f, 10f);
        _scale = (float)Math.Pow(_scaleBase, 4);
        
        // mouse and keyboard controls
        
        var mouseWorldCoords = ScreenToWorldCoords((Vec2<int>)MousePosition);
        var mouseScreenCoords = (Vec2<int>)MousePosition;

        // set the brushRange
        _brushRange = new Range2D(
            Math.Min(mouseWorldCoords.X, _leftMouseWorldCoords.X),
            Math.Min(mouseWorldCoords.Y, _leftMouseWorldCoords.Y),
            Math.Max(mouseWorldCoords.X, _leftMouseWorldCoords.X) + 1,
            Math.Max(mouseWorldCoords.Y, _leftMouseWorldCoords.Y) + 1);
        
        // if we released lmouse, place the _activeBrush in the _world in the _brushRange
        if (MouseState.IsButtonReleased(MouseButton.Left))
        {
            _world[_brushRange] = _activeBrush;
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
        }
        
        // if we pressed lmouse, set the _leftMouseWorldCoords to the mouse pos
        if (MouseState.IsButtonPressed(MouseButton.Left))
        {
            _leftMouseWorldCoords = mouseWorldCoords;
        }
        
        // if we pressed mmouse, set the _middleMouseScreenCoords to the mouse pos
        if (MouseState.IsButtonPressed(MouseButton.Middle))
        {
            _middleMouseScreenCoords = (Vec2<int>)MousePosition;
            _prevTranslation = _translation;
        }
        
        // if we are currently holding mmouse, set the translation of the world
        if (MouseState.IsButtonDown(MouseButton.Middle))
        {
            _translation = _prevTranslation + (Vec2<double>)(mouseScreenCoords - _middleMouseScreenCoords) / _scale;
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
        var retValue = _world.InvokeRanged(rectangle,
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
    public static double GetScale()
    {
        return _scale;
    }

    /// <summary>
    /// Returns the translation (pan) of the world.
    /// </summary>
    public static Vec2<double> GetTranslation()
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
