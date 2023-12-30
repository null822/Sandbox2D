using System;
using System.IO;
using System.Numerics;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Sandbox2D.Maths;
using Sandbox2D.Maths.BlockMatrix;
using Sandbox2D.World;
using Sandbox2D.World.Tiles;
using static Sandbox2D.Util;
using static Sandbox2D.Constants;

namespace Sandbox2D;

public class MainWindow : GameWindow
{
    // world
    private BlockMatrix<IBlockMatrixTile> _world = new(new Air(), new Vec2Long(WorldWidth, WorldHeight));
    
    // world editing
    private IBlockMatrixTile _activeBrush = new Stone();
    private static Range2D _brushRange;
    private static bool _isOverlapping;
    private static Vec2Long _initialMousePos = new(0);
    
    // camera position
    private static double _scale = 1;
    private static Vec2Double _translation = new(0);
    private static Vec2Double _prevTranslation = new(0);
    private static Vec2Long _gridSize;
    
    // controls
    private KeyboardState _prevKeyboardState;
    private MouseState _prevMouseState;
    
    private Vec2Int _middleMouseCords = new(0);
    private int _scrollWheelOffset = -1200;
    
    
    public MainWindow(int width, int height, string title) : base(GameWindowSettings.Default,
    new NativeWindowSettings { ClientSize = (width, height), Title = title })
    {
        var e = new Vector<int>();
        
        
        // "System Checks"
        
        Debug("===============[SYSTEM CHECKS]===============");
        
        Log("log text");
        Debug("debug text");
        Warn("warn text");
        Error("error text");

        var r1 = new Range2D(-2, -2, 2, 4);
        var r2 = new Range2D(0, 1, 3, 3);
        
        Debug(r1.Overlap(r2));
        
        Debug("===============[BEGIN PROGRAM]===============");
    }

    /// <summary>
    /// The game logic loop
    /// </summary>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        if (_prevKeyboardState == null)
            UpdatePrevKeyboardState(KeyboardState);
        if (_prevMouseState == null)
            UpdatePrevMouseState(MouseState);
        
        base.OnUpdateFrame(args);
        
        // only run when focused
        // if (!IsActive)
        //     return;
        
        // Controls

        if (KeyboardState.IsKeyDown(Keys.M) && !_prevKeyboardState.IsKeyDown(Keys.M))
        {
            var svgMap = _world.GetSvgMap().ToString();
            
            var map = File.Create("BlockMatrixMap.svg");
            map.Write(Encoding.ASCII.GetBytes(svgMap));
            map.Close();
            
            Log("BlockMatrix Map Saved");
        }
        if (KeyboardState.IsKeyDown(Keys.S) && !_prevKeyboardState.IsKeyDown(Keys.S))
        {
            var save = File.Create("save.bm");

            _world.Serialize(save);

            save.Close();
            Log("BlockMatrix Saved");
        }
        if (KeyboardState.IsKeyDown(Keys.L) && !_prevKeyboardState.IsKeyDown(Keys.L))
        {
            var save = File.Open("save.bm", FileMode.Open);

            _world = BlockMatrix<IBlockMatrixTile>.Deserialize(save);
            
            save.Close();
            Log("BlockMatrix Loaded");

        }
        if (KeyboardState.IsKeyDown(Keys.C) && !_prevKeyboardState.IsKeyDown(Keys.C))
        {
            _world = new BlockMatrix<IBlockMatrixTile>(
                new Air(),
                new Vec2Long(WorldWidth, WorldHeight));
            
            Log("World Cleared");
        }
        
        // var MousePosition = new Vec2Int(MousePosition, MouseState.Y);
        
        const int min = 1000;
        const int max = 4000;

        _scrollWheelOffset = (MouseState.Scroll.Y - _scrollWheelOffset) switch
        {
            > max => (int)MouseState.Scroll.Y - max,
            < min => (int)MouseState.Scroll.Y - min,
            _ => _scrollWheelOffset
        };
        
        // only run controls logic when hovered
        if (MousePosition.X < 0 || MousePosition.X > ClientSize.X || MousePosition.Y < 0 || MousePosition.Y > ClientSize.Y)
            return;
        
        var mousePos = ScreenToGameCoords((Vec2Int)MousePosition);
        _scale = Math.Pow((MouseState.Scroll.Y - _scrollWheelOffset) / 1024f, 4);
        
        
        switch (MouseState[MouseButton.Left])
        {
            // lMouse first tick
            case true when _prevMouseState[MouseButton.Left]:
                break;
            // !lMouse
            case false when !_prevMouseState[MouseButton.Left]:
                break;
        }

        // !lShift
        if (!KeyboardState.IsKeyDown(Keys.LeftShift))
        {
            if (_brushRange.GetArea() > 1)
            {
                _brushRange = new Range2D(mousePos.X, mousePos.Y, mousePos.X + 1, mousePos.Y + 1);
            }
        }
        
        // tick after lMouse || !lShift
        if ((!MouseState[MouseButton.Left] && _prevMouseState[MouseButton.Left]) || !KeyboardState.IsKeyDown(Keys.LeftShift))
        {
            _brushRange = new Range2D(mousePos.X, mousePos.Y, mousePos.X + 1, mousePos.Y + 1);
        }
        
        // update _isOverlapping by checking if _brushRange intersects with any tile on screen
        _isOverlapping = TileIntersect(_brushRange);

        // first tick of lMouse
        if (MouseState[MouseButton.Left] && !_prevMouseState[MouseButton.Left])
        {
            _initialMousePos = mousePos;

            // & !overlap & !lShift
            if (!_isOverlapping && !KeyboardState.IsKeyDown(Keys.LeftShift))
            {
                // _world[Brush[0].GetPos()] = Brush[0];

                _world[_brushRange] = _activeBrush;

                // _world.Set(Brush[0].GetPos(), Brush[0]);
                // AddComponent(Brush[0]);
            }
        }
        
        // tick after lMouse
        if (!MouseState[MouseButton.Left] && _prevMouseState[MouseButton.Left])
        {
            // & lShift
            if (KeyboardState.IsKeyDown(Keys.LeftShift) && !_isOverlapping)
            {
                // create the contents of the brush in the world (add to _world)
                _world.Set(_brushRange, _activeBrush);
            }
            
            _brushRange = new Range2D(mousePos.X, mousePos.Y, mousePos.X + 1, mousePos.Y + 1);
        }

        // lMouse
        if (MouseState[MouseButton.Left])
        {
            // & lShift
            if (KeyboardState.IsKeyDown(Keys.LeftShift))
            {
                _brushRange = new Range2D(
                    (int)_initialMousePos.X,
                    (int)_initialMousePos.Y,
                    (int)mousePos.X + 1,
                    (int)mousePos.Y + 1);
            }
        }
        
        // mMouse
        if (MouseState[MouseButton.Middle])
        {
            if (!_prevMouseState[MouseButton.Middle])
            {
                _middleMouseCords = (Vec2Int)MousePosition;
                _prevTranslation = _translation;
            }
            else
            {
                _translation = _prevTranslation + (Vec2Double)((Vec2Int)MousePosition - _middleMouseCords) / _scale;
            }
        }
        
        // rMouse
        if (MouseState[MouseButton.Right] && _prevMouseState[MouseButton.Right])
        {
            _translation = new Vec2Double(0);
            _scrollWheelOffset = (int)MouseState.Scroll.Y - 1200;
            
            _scale = Math.Pow(Math.Min(Math.Max((MouseState.Scroll.Y - _scrollWheelOffset) / 1024f, 0e-4), 0e4), 4);

        }

        UpdatePrevMouseState(MouseState);
        UpdatePrevKeyboardState(KeyboardState);

        _gridSize = GameToScreenCoords(new Vec2Long(0, 0)) - GameToScreenCoords(new Vec2Long(1, 1));
        
        
        
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
    private static Range2D GetCollisionRectangle(Vec2Long pos)
    {
        return new Range2D(
            pos.X,
            pos.Y,
            pos.X + 1,
            pos.Y + 1
        );
    }
    
    private void UpdatePrevMouseState(MouseState state)
    {
        _prevMouseState = state;
    }
    
    private void UpdatePrevKeyboardState(KeyboardState state)
    {
        _prevKeyboardState = state;
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
    public static Vec2Double GetTranslation()
    {
        return _translation;
    }

    /// <summary>
    /// Returns the screen size.
    /// </summary>
    public Vec2Int GetScreenSize()
    {
        return (Vec2Int)ClientSize;
    }
    
    /// <summary>
    /// Returns the screen size.
    /// </summary>
    public static Vec2Long GetGridSize()
    {
        return _gridSize;
    }
    
}
