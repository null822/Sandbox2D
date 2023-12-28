using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ElectroSim.Content;
using ElectroSim.Gui;
using ElectroSim.Maths.BlockMatrix;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using Sandbox2D.Gui.MenuElements;
using Sandbox2D.Maths;
using Sandbox2D.Registry;
using Sandbox2D.World;
using Sandbox2D.World.Tiles;
using static Sandbox2D.Util;
using static Sandbox2D.Constants;

namespace Sandbox2D;

public class MainWindow : Game
{
    
    // rendering
    private readonly GraphicsDeviceManager _graphics;
    private static SpriteBatch _spriteBatch;

    // world/ui
    private BlockMatrix<IBlockMatrixTile> _world = new(new Air(), new Vec2Long(WorldWidth, WorldHeight));
    private readonly List<Menu> _menus = [];
    
    // world editing
    private IBlockMatrixTile _activeBrush = new Stone();
    private static Range2D _brushRange;
    private static bool _isOverlapping;
    private static Vec2Long _initialMousePos = Vector2.Zero;
    
    // camera position
    private static double _scale = 1;
    private static Vec2Double _translation = Vector2.Zero;
    private static Vec2Double _prevTranslation = Vector2.Zero;
    private static Vec2Long _gridSize;
    
    // output/screen
    private static Vec2Int _screenSize = Vector2.One;
    
    
    
    // controls
    // private readonly bool[] _prevMouseButtons = new bool[5];
    private MouseState _prevMouseState;
    private KeyboardState _prevKeyboardState;
    private Vec2Int _middleMouseCords = Vector2.Zero;
    private int _scrollWheelOffset = -1200;
    
    
    public MainWindow()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "assets";
        IsMouseVisible = true;
        
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnResize;
        
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

    protected override void Initialize()
    {
        base.Initialize();
        
        Console.WriteLine("Initialized");
    }

    /// <summary>
    /// Loads all of the games resources/fonts and initializes some variables
    /// </summary>
    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        RegisterTextures(new[]
        {
            "missing",
            "dirt",
            "stone",
            
            "gui/center",
            "gui/corner",
            "gui/edge"
        });
        
        RegisterFonts(new[]
        {
            "consolas"
        });

        var brushTypes = new List<IBlockMatrixTile>
        {
            new Dirt(),
            new Stone()
        };

        var brushTypeMenuElements = new MenuElement[brushTypes.Count];

        var i = 0;
        foreach (var brushType in brushTypes)
        {
            brushTypeMenuElements[i] = 
                new ImageElement(
                    new ScalableValue2(
                        new ScalableValue(0, AxisBind.X, 8, 8),
                        new ScalableValue(0.1f * (i+1), AxisBind.Y)
                    ),
                    new ScalableValue2(new Vector2(0), new Vector2(48), new Vector2(48)),
                    brushType.Texture,
                    () =>
                    {
                        _activeBrush = brushType;
                        Console.WriteLine(brushType.Name);
                    }
                );

            i++;
        }
        
        // debug / testing
        _menus.Add(new Menu(
            new ScalableValue2(new Vector2(0, 0.15f)),
            new ScalableValue2(
                new ScalableValue(0, AxisBind.X, 56, 56),
                new ScalableValue(0.7f, AxisBind.Y)
                ),
            brushTypeMenuElements
            )
        );


    }

    /// <summary>
    /// The game logic loop
    /// </summary>
    protected override void Update(GameTime gameTime)
    {
        // only run when focused
        if (!IsActive)
            return;
        
        // Logic
        _screenSize = new Vector2(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        
        
        // Controls
        var keyboardState = Keyboard.GetState();
        var mouseState = Mouse.GetState();

        if (keyboardState.IsKeyDown(Keys.M) && !_prevKeyboardState.IsKeyDown(Keys.M))
        {
            var svgMap = _world.GetSvgMap().ToString();
            
            var map = File.Create("BlockMatrixMap.svg");
            map.Write(Encoding.ASCII.GetBytes(svgMap));
            map.Close();
            
            Log("BlockMatrix Map Saved");
        }
        if (keyboardState.IsKeyDown(Keys.S) && !_prevKeyboardState.IsKeyDown(Keys.S))
        {
            var save = File.Create("save.bm");

            _world.Serialize(save);

            save.Close();
            Log("BlockMatrix Saved");
        }
        if (keyboardState.IsKeyDown(Keys.L) && !_prevKeyboardState.IsKeyDown(Keys.L))
        {
            var save = File.Open("save.bm", FileMode.Open);

            _world = BlockMatrix<IBlockMatrixTile>.Deserialize(save);
            
            save.Close();
            Log("BlockMatrix Loaded");

        }
        if (keyboardState.IsKeyDown(Keys.C) && !_prevKeyboardState.IsKeyDown(Keys.C))
        {
            _world = new BlockMatrix<IBlockMatrixTile>(
                new Air(),
                new Vec2Long(WorldWidth, WorldHeight));
            
            Log("World Cleared");
        }
        
        var mouseScreenCords = new Vec2Int(mouseState.X, mouseState.Y);
        
        const int min = 1000;
        const int max = 4000;

        _scrollWheelOffset = (mouseState.ScrollWheelValue - _scrollWheelOffset) switch
        {
            > max => mouseState.ScrollWheelValue - max,
            < min => mouseState.ScrollWheelValue - min,
            _ => _scrollWheelOffset
        };
        
        // only run controls logic when hovered
        if (mouseScreenCords.X < 0 || mouseScreenCords.X > _screenSize.X || mouseScreenCords.Y < 0 || mouseScreenCords.Y > _screenSize.Y)
            return;
        
        var mousePos = Util.ScreenToGameCoords(mouseScreenCords);
        _scale = Math.Pow((mouseState.ScrollWheelValue - _scrollWheelOffset) / 1024f, 4);
        
        foreach (var menu in _menus)
        {
            menu.CheckHover(mouseScreenCords);
        }

        switch (mouseState.LeftButton)
        {
            // lMouse first tick
            case ButtonState.Pressed when _prevMouseState.LeftButton == ButtonState.Released && _menus.Any(menu => menu.Click()):
                UpdatePrevMouseState(mouseState);
                return;
            // !lMouse
            case ButtonState.Released when _prevMouseState.LeftButton == ButtonState.Pressed:
                break;
        }

        // !lShift
        if (!keyboardState.IsKeyDown(Keys.LeftShift))
        {
            if (_brushRange.GetArea() > 1)
            {
                _brushRange = new Range2D(mousePos.X, mousePos.Y, mousePos.X + 1, mousePos.Y + 1);
            }
        }
        
        // tick after lMouse || !lShift
        if ((mouseState.LeftButton == ButtonState.Released && _prevMouseState.LeftButton == ButtonState.Released) || !keyboardState.IsKeyDown(Keys.LeftShift))
        {
            _brushRange = new Range2D(mousePos.X, mousePos.Y, mousePos.X + 1, mousePos.Y + 1);
        }
        
        // update _isOverlapping by checking if _brushRange intersects with any tile on screen
        _isOverlapping = TileIntersect(_brushRange);

        // first tick of lMouse
        if (mouseState.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
        {
            _initialMousePos = mousePos;

            // & !overlap & !lShift
            if (!_isOverlapping && !keyboardState.IsKeyDown(Keys.LeftShift))
            {
                // _world[Brush[0].GetPos()] = Brush[0];

                _world[_brushRange] = _activeBrush;

                // _world.Set(Brush[0].GetPos(), Brush[0]);
                // AddComponent(Brush[0]);
            }
        }
        
        // tick after lMouse
        if (mouseState.LeftButton == ButtonState.Released && _prevMouseState.LeftButton == ButtonState.Pressed)
        {
            // & lShift
            if (keyboardState.IsKeyDown(Keys.LeftShift) && !_isOverlapping)
            {
                // create the contents of the brush in the world (add to _world)
                _world.Set(_brushRange, _activeBrush);
            }
            
            _brushRange = new Range2D(mousePos.X, mousePos.Y, mousePos.X + 1, mousePos.Y + 1);
        }

        // lMouse
        if (mouseState.LeftButton == ButtonState.Pressed)
        {
            // & lShift
            if (keyboardState.IsKeyDown(Keys.LeftShift))
            {
                _brushRange = new Range2D(
                    (int)_initialMousePos.X,
                    (int)_initialMousePos.Y,
                    (int)mousePos.X + 1,
                    (int)mousePos.Y + 1);
            }
        }
        
        // mMouse
        if (mouseState.MiddleButton == ButtonState.Pressed)
        {
            if (_prevMouseState.MiddleButton == ButtonState.Released)
            {
                _middleMouseCords = mouseScreenCords;
                _prevTranslation = _translation;
            }
            else
            {
                _translation = _prevTranslation + (Vec2Double)(mouseScreenCords - _middleMouseCords) / _scale;
            }
        }
        
        // rMouse
        if (mouseState.RightButton == ButtonState.Pressed && _prevMouseState.RightButton == ButtonState.Released)
        {
            _translation = new Vec2Double(0);
            _scrollWheelOffset = mouseState.ScrollWheelValue - 1200;
            
            _scale = Math.Pow(Math.Min(Math.Max((mouseState.ScrollWheelValue - _scrollWheelOffset) / 1024f, 0e-4), 0e4), 4);

        }

        UpdatePrevMouseState(mouseState);
        UpdatePrevKeyboardState(keyboardState);

        _gridSize = GameToScreenCoords(new Vec2Long(0, 0)) - Util.GameToScreenCoords(new Vec2Long(1, 1));

        base.Update(gameTime);
    }

    /// <summary>
    /// The draw loop
    /// </summary>
    protected override void Draw(GameTime gameTime)
    {
        // only run when focused
        if (!IsActive)
            return;
        
        GraphicsDevice.Clear(Colors.CircuitBackground);
        
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        
        // render tiles (with off-screen culling)
        
        // game coords of the top left and bottom right corners of the screen, with a small buffer to prevent culling things still partially within the frame
        var tlScreen = ScreenToGameCoords(new Vector2(0, 0) - new Vector2(64));
        var brScreen = ScreenToGameCoords(_screenSize + new Vec2Int(64));
        
        _world.InvokeRanged(new Range2D(tlScreen, brScreen), (tile, pos) =>
        {
            tile.Render(_spriteBatch, pos);
            return true;
        }, ResultComparisons.Or, true);
        
        // render brush outline

        var brushScreenCoordsBl = GameToScreenCoords(new Vec2Long(_brushRange.MinX, _brushRange.MinY));
        var brushScreenCoordsTr = GameToScreenCoords(new Vec2Long(_brushRange.MaxX, _brushRange.MaxY));

        var brushScreenSize = brushScreenCoordsTr - brushScreenCoordsBl;
        
        _spriteBatch.DrawRectangle(brushScreenCoordsBl.X, brushScreenCoordsBl.Y, brushScreenSize.X, brushScreenSize.Y,
            Color.White, 2f);

        foreach (var menu in _menus)
        {
            menu.Render(_spriteBatch);
        }
        
        _spriteBatch.End();
        
        
        base.Draw(gameTime);
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
    /// Update the program when the size changes.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnResize(object sender, EventArgs e)
    {
        if (_graphics.PreferredBackBufferWidth == _graphics.GraphicsDevice.Viewport.Width &&
            _graphics.PreferredBackBufferHeight == _graphics.GraphicsDevice.Viewport.Height)
            return;
        
        _graphics.PreferredBackBufferWidth = _graphics.GraphicsDevice.Viewport.Width;
        _graphics.PreferredBackBufferHeight = _graphics.GraphicsDevice.Viewport.Height;
        
        _graphics.ApplyChanges();
    }
    
    
    /// <summary>
    /// Register multiple textures and store them in TextureRegistry.
    /// </summary>
    /// <param name="names">An array of the names of the textures to load</param>
    private void RegisterTextures(IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            TextureRegistry.RegisterTexture(name, Content.Load<Texture2D>("textures/" + name));
        }
    }
    
    
    /// <summary>
    /// Register multiple fonts and store them in FontRegistry.
    /// </summary>
    /// <param name="names">An array of the names of the fonts to load</param>
    private void RegisterFonts(IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            var fontName = name[(name.LastIndexOf('/') + 1)..];
        
            FontRegistry.RegisterFont(fontName, Content.Load<BitmapFont>("fonts/" + name));
        }
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
    public static Vec2Int GetScreenSize()
    {
        return _screenSize;
    }
    
    /// <summary>
    /// Returns the screen size.
    /// </summary>
    public static Vec2Long GetGridSize()
    {
        return _gridSize;
    }
    
}
