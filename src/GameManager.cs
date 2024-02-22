using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Sandbox2D.Graphics.Renderables;
using Sandbox2D.GUI;
using Sandbox2D.Maths;
using Sandbox2D.Maths.QuadTree;
using Sandbox2D.World;
using Sandbox2D.World.TileTypes;
using static Sandbox2D.Constants;
using static Sandbox2D.Util;

namespace Sandbox2D;

public static class GameManager
{
    private const int Mspt = 1000 / MasterTps;
    
    private static QuadTree<IBlockMatrixTile> _world;
    
    // world editing
    private static IBlockMatrixTile _activeBrush;
    private static uint _brushId;
    private static Range2D _brushRange;
    private static Vec2<long> _leftMouseWorldCoords = new(0);
    private static Vec2<int> _middleMouseScreenCoords = new(0);

    // camera
    public static Vec2<decimal> Translation { private set; get; }
    public static decimal Scale { private set; get; } = 1;
    public static Vec2<int> ScreenSize { private set; get; }
    
    // controls
    private static float _scrollPos = 1;
    private static Vec2<decimal> _prevTranslation = new(0);
    private static KeyboardState _keyboardState;
    private static MouseState _mouseState;
    
    // rendering
    private static int _prevLqtLength;
    private static bool _worldUpdatedSinceLastFrame = true;
    private static Range2D _renderRange = new ((0, 0), 0x1 << RenderDepth);
    
    // game state
    public static bool IsRunning { private set; get; }
    private static bool _isActive = true;

    private static long _totalTicks;
    
    public static void Run()
    {
        // set thread affinity to 1 avoid changing cores
        SetThreadAffinityMask(GetCurrentThread(), new IntPtr(1));
        
        // initialize the logic
        Initialize();
        
        // create a stopwatch to keep track of how long a tick has taken
        var stopwatch = Stopwatch.StartNew();
        
        // while the game is running
        while (_isActive)
        {
            // restart the stopwatch
            stopwatch.Restart();
            
            // if the game should be running, update it
            if (IsRunning)
            {
                UpdateControls();
                
                // if this tick is a logic tick, update the logic
                if (_totalTicks % TicksPerLogicTick == 0)
                {
                    UpdateLogic();
                }
                
                _totalTicks++;
            }
            
            var elapsed = (int)stopwatch.Elapsed.TotalMilliseconds;
            var sleepTime = Mspt - elapsed;
            
            if (sleepTime > 0)
            {
                // sleep for the remaining time in the tick
                Thread.Sleep(sleepTime);
            }
            else if (IsRunning)
            {
                Warn($"Game is running slowly! Running at {1000f/elapsed:F1} TPS");
            }
            
            
        }
        
        Log("Game Logic Thread Shut Down");
    }
    
    
    private static void UpdateLogic()
    {
        
        
        
        // update the render
        UpdateRender();
    }
    
    private static void UpdateRender()
    {
        // update the world geometry if needed
        if (_worldUpdatedSinceLastFrame && IsRunning)
        {
            // get the world pos of the top left and bottom right corners of the screen,
            // with a small buffer to prevent culling things still partially within the frame
            var tlScreen = ScreenToWorldCoords((0, ScreenSize.Y));
            var brScreen = ScreenToWorldCoords((ScreenSize.X, 0));
            var screenRange = new Range2D(tlScreen, brScreen);
        
            // create the lqt as a list, with the starting size of the previous lqt's length
            var lqtList = new List<QuadTreeStruct>(_prevLqtLength);
        
            // serialize the _world
            _renderRange = _world.SerializeToLinear(ref lqtList, screenRange);
            var lqt = lqtList.ToArray();
            
            // update prev lqt length
            _prevLqtLength = lqt.Length;
        
            // calculate renderScale
            var renderScale = (decimal)_renderRange.Width / (0x1 << RenderDepth);
        
            // calculate scale/translation
            var scale = (float)(Scale * renderScale);
            var translation = (Vec2<float>)((Translation - (Vec2<decimal>)_renderRange.Center) / renderScale);

            // update the lqt on the render thread
            RenderManager.UpdateLqt(lqt, scale, translation);
            
            // clear the lqtList
            lqtList.Clear();
            
            // reset _worldUpdatedSinceLastFrame
            _worldUpdatedSinceLastFrame = false;
        }
    }
    
    private static void UpdateControls()
    {
        var mouseScreenCoords = new Vec2<int>((int)Math.Floor(_mouseState.X), (int)Math.Floor(_mouseState.Y));
        var mouseWorldCoords = ScreenToWorldCoords(mouseScreenCoords);
        
        var center = ScreenSize / 2;
        
        if (_keyboardState.IsKeyPressed(Keys.Escape))
        {
            IsRunning = !IsRunning;
            GuiManager.SetVisibility(0, IsRunning);
        }
        
        
        if (!IsRunning)
        {
            // if the game is paused, update only the pause menu
            GuiManager.MouseOver(mouseScreenCoords - center, 0);
            return;
        }
        
        // update GUIs
        GuiManager.MouseOver(mouseScreenCoords - center);

        
        // keyboard-only controls
        
        // mapping/saving/loading/clearing
        if (_keyboardState.IsKeyPressed(Keys.M))
        {
            var svgMap = _world.GetSvgMap().ToString();
            
            var map = File.Create("BlockMatrixMap.svg");
            map.Write(Encoding.ASCII.GetBytes(svgMap));
            map.Close();
            
            Log("QuadTree Map Saved");
        }
        if (_keyboardState.IsKeyPressed(Keys.S))
        {
            var save = File.Create("save.qdt");

            _world.Serialize(save);

            save.Close();
            Log("QuadTree Saved");
        }
        if (_keyboardState.IsKeyPressed(Keys.L))
        {
            
            var save = File.Open("save.qdt", FileMode.Open);

            _world = QuadTree<IBlockMatrixTile>.Deserialize(save);
            
            save.Close();
            
            _worldUpdatedSinceLastFrame = true;

            Log("QuadTree Loaded");

        }
        if (_keyboardState.IsKeyPressed(Keys.C))
        {
            _world = new QuadTree<IBlockMatrixTile>(
                new Air(),
                WorldDepth);

            _worldUpdatedSinceLastFrame = true;
            
            Log("World Cleared");
        }
        
        
        // if the scroll wheel has moved
        if (_mouseState.ScrollDelta.Y != 0)
        {
            // zoom
            
            var yDelta = -_mouseState.ScrollDelta.Y;
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
                    Scale = scale;
                    break;
            }
            
            // update _scale, and set the _worldUpdatedSinceLastFrame flag for culling reasons
            // _scale = newScale;
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
        if (_mouseState.IsButtonReleased(MouseButton.Left) && _keyboardState.IsKeyDown(Keys.LeftShift))
        {
            Log($"Placing at {_brushRange}");

            _world[_brushRange] = _activeBrush;
            _worldUpdatedSinceLastFrame = true;
        }
        
        // if we are not currently holding lmouse or not holding lshift, set the _leftMouseWorldCoords to the mouse pos
        if (!_mouseState.IsButtonDown(MouseButton.Left) || !_keyboardState.IsKeyDown(Keys.LeftShift))
        {
            _leftMouseWorldCoords = mouseWorldCoords;
        }
        
        // if we are currently holding lmouse and are not holding lshift, place the brush in the world
        if (_mouseState.IsButtonDown(MouseButton.Left) && !_keyboardState.IsKeyDown(Keys.LeftShift))
        {
            // technically buggy behaviour, but creates cool looking, "smooth" lines
            
            Log($"Replaced {_world[_brushRange.MaxXMaxY]}");
            Log($"Placing at {_brushRange}");

            _world[_brushRange] = _activeBrush;
            _worldUpdatedSinceLastFrame = true;
            
        }
        
        // if we pressed lmouse, set the _leftMouseWorldCoords to the mouse pos
        if (_mouseState.IsButtonPressed(MouseButton.Left))
        {
            _leftMouseWorldCoords = mouseWorldCoords;
        }
        
        // if we pressed mmouse, set the _middleMouseScreenCoords to the mouse pos
        if (_mouseState.IsButtonPressed(MouseButton.Middle))
        {
            _middleMouseScreenCoords = mouseScreenCoords;
            _prevTranslation = Translation;
        }
        
        // if we are currently holding mmouse, set the translation of the world
        if (_mouseState.IsButtonDown(MouseButton.Middle))
        {
            var worldTranslationOffset = (Vec2<decimal>)((Vec2<double>)(_middleMouseScreenCoords - mouseScreenCoords) / (double)Scale);
            
            worldTranslationOffset = (worldTranslationOffset.X, -worldTranslationOffset.Y);
            
            var newTranslation = _prevTranslation + worldTranslationOffset;

            if (Translation != newTranslation)
            {
                Translation = newTranslation;
                _worldUpdatedSinceLastFrame = true;
            }
        }

        if (_mouseState.IsButtonPressed(MouseButton.Right))
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
        
        
        if (_keyboardState.IsKeyPressed(Keys.LeftControl))
        {
            Log($"Mouse Position: {mouseWorldCoords}");
            _worldUpdatedSinceLastFrame = true;
        }
        
        // calculate renderScale
        var renderScale = (decimal)_renderRange.Width / (0x1 << RenderDepth);
        
        // calculate scale/translation
        var rScale = (float)(Scale * renderScale);
        var rTranslation = (Vec2<float>)((Translation - (Vec2<decimal>)_renderRange.Center) / renderScale);

        
        RenderManager.UpdateTransform(rScale, rTranslation);
    }
    
    
    /// <summary>
    /// Initializes everything. Run once, when the game launches
    /// </summary>
    private static void Initialize()
    {
        _world = new QuadTree<IBlockMatrixTile>(new Air(), WorldDepth);
        
        Log("Created World");

        // run the system checks

        Log("===============[SYSTEM CHECKS]===============");
        SystemChecks();
        
        Log("===============[BEGIN PROGRAM]===============");
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

        var e = new QuadTree<IBlockMatrixTile>(new Air(), 3);
        
        e[1, 1] = new Stone();
        
        Debug(e[1, 1].Id);
    }

    // public setters

    /// <summary>
    /// Starts the game logic
    /// </summary>
    public static void SetRunning(bool value)
    {
        if (IsRunning != value)
            IsRunning = value;
    }

    public static void SetScreenSize(Vec2<int> size)
    {
        ScreenSize = size;
    }

    public static void SetInputs(MouseState ms, KeyboardState ks)
    {
        _mouseState = ms;
        _keyboardState = ks;
        
    }
    
    // DllImports
    
    [DllImport("kernel32", SetLastError = true)]
    private static extern IntPtr SetThreadAffinityMask(IntPtr hThread, IntPtr dwThreadAffinityMask);
    
    [DllImport("kernel32")]
    private static extern IntPtr GetCurrentThread();
}