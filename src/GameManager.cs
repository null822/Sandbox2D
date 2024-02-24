using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Sandbox2D.Graphics.Renderables;
using Sandbox2D.Maths;
using Sandbox2D.Maths.QuadTree;
using Sandbox2D.World;
using Sandbox2D.World.TileTypes;
using static Sandbox2D.Constants;
using static Sandbox2D.Util;

namespace Sandbox2D;

public static class GameManager
{
    private static QuadTree<ITile, QtTile<ITile>> _world;
    
    // camera
    public static Vec2<decimal> Translation { private set; get; }
    public static decimal Scale { private set; get; } = 1;
    public static Vec2<int> ScreenSize { private set; get; }
    
    // rendering
    private static int _prevLqtLength;
    private static bool _worldUpdatedSinceLastFrame = true;
    public static Range2D RenderRange { get; private set; } = new ((0, 0), 0x1 << RenderDepth);
    
    // game state
    public static bool IsRunning { private set; get; }
    private static bool _isActive = true;
    public static bool WorldBuffer { get; private set; }
    
    /// <summary>
    /// Main logic loop.
    /// </summary>
    /// <remarks>Returns once the logic thread has shut down</remarks>
    public static void Run()
    {
        // initialize the logic
        Initialize();
        
        // create a stopwatch to keep track of how long a tick has taken
        var stopwatch = Stopwatch.StartNew();
        
        // while the game is running
        while (_isActive)
        {
            // restart the stopwatch
            stopwatch.Restart();
            
            if (IsRunning)
            {
                // update the logic
                UpdateLogic();
                
                // update the render thread
                UpdateRender();
            }
            
            const int mspt = 1000 / Tps;
            
            var elapsed = (int)stopwatch.Elapsed.TotalMilliseconds;
            var sleepTime = Math.Max(mspt - elapsed, 0);
            
            // calculate tps
            var derivedTps = 1000f / (elapsed + sleepTime);
            
            // update tps metric on the render thread
            RenderManager.UpdateTps(derivedTps);
            
            // sleep for the remaining time in the tick
            if (sleepTime > 0)
            {
                Thread.Sleep(sleepTime);
            }
        }
        
        // signal that the thread has shut down
        _isActive = true;
        
        Log("Game Logic Thread Shut Down");
    }
    
    /// <summary>
    /// Updates the game logic.
    /// </summary>
    private static void UpdateLogic()
    {
        var worldModifications = new KeyValuePair<Range2D, ITile>[RenderManager.WorldModifications.Count];
        RenderManager.WorldModifications.CopyTo(worldModifications);
        
        // apply any world modifications
        foreach (var modification in worldModifications)
        {
            _world[modification.Key] = modification.Value;
        }
        
        if (worldModifications.Length != 0)
            _worldUpdatedSinceLastFrame = true;

        var nullableAction = RenderManager.WorldAction;
        
        if (nullableAction != null)
        {
            var action = nullableAction.Value;
            
            switch (action.action)
            {
                case WorldAction.Save:
                {
                    var save = File.Create("save.qdt");
                    
                    _world.Serialize(save);
                    
                    save.Close();
                    Log("QuadTree Saved");
                    break;
                }
                case WorldAction.Load:
                {
                    var save = File.Open(action.arg, FileMode.Open);
                    
                    _world = QuadTree<ITile, QtTile<ITile>>.Deserialize(save);
                    
                    save.Close();
                    
                    _worldUpdatedSinceLastFrame = true;
                    
                    Log("QuadTree Loaded");
                    
                    break;
                }
                case WorldAction.Clear:
                {
                    _world = new QuadTree<ITile, QtTile<ITile>>(new Air(), WorldDepth);
                    
                    _worldUpdatedSinceLastFrame = true;
                    
                    Log("World Cleared");
                    
                    break;
                }
                case WorldAction.Map:
                {
                    var svgMap = _world.GetSvgMap().ToString();
                    
                    var map = File.Create(action.arg);
                    map.Write(Encoding.ASCII.GetBytes(svgMap));
                    map.Close();
                    Log("QuadTree Mapped");
                    
                    break;
                }
            }
        }
        
        RenderManager.ResetWorldModifications();
    }
    
    /// <summary>
    /// Updates the render thread.
    /// </summary>
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
            RenderRange = _world.SerializeToLinear(ref lqtList, screenRange);
            var lqt = lqtList.ToArray();
            lqtList.Clear();
            
            // update prev lqt length
            _prevLqtLength = lqt.Length;
            
            // calculate renderScale
            var renderScale = (decimal)RenderRange.Width / (0x1 << RenderDepth);
            
            // calculate scale/translation
            var scale = (float)(Scale * renderScale);
            var translation = (Vec2<float>)((Translation - (Vec2<decimal>)RenderRange.Center) / renderScale);
            
            // update the lqt on the render thread
            RenderManager.UpdateLqt(lqt, scale, translation);
            
            // reset _worldUpdatedSinceLastFrame
            _worldUpdatedSinceLastFrame = false;
        }
    }
    
    
    
    /// <summary>
    /// Initializes everything. Run once when the game launches.
    /// </summary>
    private static void Initialize()
    {
        Prog("===============[ LOGIC LOADING  ]===============");

        
        // create tiles
        Tiles.Instantiate(new ITile[]
        {
            new Air(),
            new Stone(),
            new Dirt()
        });
        
        // create wold
        _world = new QuadTree<ITile, QtTile<ITile>>(new Air(), WorldDepth);
        
        Log("Created World");
        
        // run the system checks
        Prog("===============[ SYSTEM CHECKS  ]===============");
        SystemChecks();
        
    }
    
    /// <summary>
    /// Makes sure everything works.
    /// </summary>
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

        var e = new QuadTree<ITile, QtTile<ITile>>(new Air(), 3);
        
        e[1, 1] = new Stone();
        
        Debug(e[1, 1].Get());
    }

    // public setters

    /// <summary>
    /// Sets whether the game is running.
    /// </summary>
    public static void SetRunning(bool value)
    {
        if (IsRunning != value)
            IsRunning = value;
    }
    
    /// <summary>
    /// Shuts down the Logic Thread and returns once it is shut down.
    /// </summary>
    public static void Close()
    {
        _isActive = false;
        
        while (!_isActive)
        {
            Thread.Sleep(10);
        }
    }
    
    /// <summary>
    /// Updates the screen size on the render thread.
    /// </summary>
    /// <param name="size">the new screen size</param>
    /// <remarks>Causes the world to be re-uploaded to the gpu.</remarks>
    public static void UpdateScreenSize(Vec2<int> size)
    {
        ScreenSize = size;

        _worldUpdatedSinceLastFrame = true;
    }
    
    /// <summary>
    /// Sets the transformation (scale/translation) for th next tick.
    /// </summary>
    /// <param name="scale">the new scale</param>
    /// <param name="translation">the new translation</param>
    /// <remarks>Causes the world to be re-uploaded to the gpu if something was changed.</remarks>
    public static void SetTransform(decimal scale, Vec2<decimal> translation)
    {
        // if nothing has changed, exit
        if (Scale == scale && Translation == translation)
            return;
        
        Scale = scale;
        Translation = translation;

        _worldUpdatedSinceLastFrame = true;
    }
}

public enum WorldAction
{
    Save,
    Load,
    Clear,
    Map
}