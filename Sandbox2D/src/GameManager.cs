using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Math2D;
using Math2D.Quadtree;
using Math2D.Quadtree.Features;
using Sandbox2D.World;
using Sandbox2D.World.Tiles;
using static Sandbox2D.Util;

namespace Sandbox2D;

// TODO: Cellular Automata

public static class GameManager
{
    private static Quadtree<Tile> _world;
    public static int WorldHeight { get; private set; } = 64;
    public static Range2D WorldDimensions => _world.Dimensions;
    
    // camera
    public static Vec2<decimal> Translation { private set; get; }
    public static decimal Scale { private set; get; } = 1;
    public static Vec2<float> ScreenSize { private set; get; }
    
    // game state
    public static bool IsRunning { private set; get; }
    private static bool _isActive = true;

    /// <summary>
    /// A <see cref="Stopwatch"/> to keep track of how long the current tick has taken
    /// </summary>
    private static readonly Stopwatch TickTimer = new();
    /// <summary>
    /// The time that the previous tick took to complete
    /// </summary>
    private static TimeSpan _prevTickTime;
    /// <summary>
    /// The maximum time that each tick should take to complete
    /// </summary>
    private static readonly TimeSpan TargetTickTime = TimeSpan.FromMilliseconds(DerivedConstants.TargetMspt);
    
    /// <summary>
    /// Main logic loop.
    /// </summary>
    /// <remarks>Returns once the logic thread has shut down</remarks>
    public static void Run()
    {
        // initialize the logic
        Initialize();
        
        // while the game is running
        while (_isActive)
        {
            // restart the stopwatch
            TickTimer.Restart();
            
            if (IsRunning)
            {
                UpdateLogic();
                UpdateRender();
            }
            _prevTickTime = TickTimer.Elapsed;
            // var sleepTime = (int)Math.Max(mspt - _prevTickTime.TotalMilliseconds, 0);
            
            var sleepTime = TargetTickTime.Subtract(_prevTickTime);
            
            // sleep for the remaining time in the tick
            if (sleepTime.Ticks > 0)
            {
                TickTimer.Stop();
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
        Translation = RenderManager.Translation;
        Scale = RenderManager.Scale;
        var (modifications, action) = RenderManager.GetWorldModifications();
        // apply any world modifications
        foreach (var modification in modifications)
        {
            var range = modification.Range;
            
            // ensure the range is within the world
            if (!_world.Dimensions.Overlaps(modification.Range))
                continue;
            
            range = _world.Dimensions.Overlap(range);
            
            if (modification.Range.Bl == modification.Range.Tr)
            {
                _world[range.Bl] = modification.Tile;
            }
            else
            {
                _world[range] = modification.Tile;
            }
        }
        
        _world.Compress();
        // _world.UpdateSubset(screenRange); //TODO: implement subset
        
        if (action != null)
            HandleAction(action.Value);
    }
    
    /// <summary>
    /// Updates the render thread.
    /// </summary>
    private static void UpdateRender()
    {
        if (!RenderManager.CanUpdateGeometry()) return;
        
        // update the modifications
        var modificationCount = _world.GetModifications(
            RenderManager.TreeModifications,
            RenderManager.DataModifications);
        if (modificationCount.Tree != 0 || modificationCount.Data != 0)
            RenderManager.SetGeometryUploaded();
        
        // update the geometry parameters
        var renderDepth = Math.Min(WorldHeight, RenderManager.MaxGpuQtHeight);
        var (treeLength, dataLength) = _world.GetLength();
        var (renderRoot, renderRange) = _world.GetSubset(CalculateScreenRange().Overlap(_world.Dimensions), renderDepth);
        RenderManager.UpdateGeometryParameters(treeLength, dataLength, renderRoot, renderRange);

        
        
        RenderManager.GeometryLock.Set();
    }
    
    /// <summary>
    /// Initializes everything. Run once when the game launches.
    /// </summary>
    private static void Initialize()
    {
        // register tiles
        TileDeserializer.Register(Air.Id, bytes => new Air(bytes));
        TileDeserializer.Register(Dirt.Id, bytes => new Dirt(bytes));
        TileDeserializer.Register(Stone.Id, bytes => new Stone(bytes));
        TileDeserializer.Register(Paint.Id, bytes => new Paint(bytes));
        
        // create world
        _world = new Quadtree<Tile>(WorldHeight, new Air(), true);
        WorldHeight = _world.MaxHeight;
        
        Log("Initialized Game Manager", "Load/Logic");
    }
    
    private static void HandleAction((WorldAction action, string arg) action)
    {
        switch (action.action) 
        {
            case WorldAction.Save: 
            {
                var save = File.Create(action.arg);
                new SerializableQuadtree<Tile>(_world).Serialize(save);
                save.Close();
                save.Dispose();
                
                Log("QuadTree Saved"); 
                break;
            }
            case WorldAction.Load: 
            { 
                var save = File.Open(action.arg, FileMode.Open);
                _world.Dispose();
                _world = SerializableQuadtree<Tile>.Deserialize<Tile>(save, true).Base;
                WorldHeight = _world.MaxHeight;
                save.Close();
                save.Dispose();
                
                Log("QuadTree Loaded");
                break;
            }
            case WorldAction.Clear:
            {
                _world.Clear();
                
                Log("World Cleared");
                break;
            }
            case WorldAction.Map:
            {
                var svgMap = new MappableQuadtree<Tile>(_world).GetSvgMap(DerivedConstants.QuadTreeSvgScale);
                
                var map = File.CreateText(action.arg);
                map.Write(svgMap);
                map.Close();
                map.Dispose();
                
                Log("QuadTree Mapped");
                break; 
            }
        }
    }
    
    private static Range2D CalculateScreenRange()
    {
        var tlScreen = ScreenToWorldCoords((0, ScreenSize.Y));
        var brScreen = ScreenToWorldCoords((ScreenSize.X, 0));
        return new Range2D(tlScreen, brScreen);
    }

    #region Public Getters / Setters
    
    /// <summary>
    /// Sets whether the game is running.
    /// </summary>
    public static void SetRunning(bool value)
    {
        IsRunning = value;
    }

    public static TimeSpan GetCurrentTickTime()
    {
        return TickTimer.Elapsed;
    }
    
    public static TimeSpan GetPreviousTickTime()
    {
        return _prevTickTime;
    }
    
    /// <summary>
    /// Shuts down the Logic Thread and returns once it is shut down.
    /// </summary>
    public static void Close()
    {
        _isActive = false;
        
        _world.Dispose();
        
        while (!_isActive)
        {
            Thread.Sleep(10);
        }
    }
    
    /// <summary>
    /// Updates the screen size on the logic thread.
    /// </summary>
    /// <param name="size">the new screen size</param>
    /// <remarks>Causes the world to be re-uploaded to the gpu.</remarks>
    public static void UpdateScreenSize(Vec2<float> size)
    {
        ScreenSize = size;
    }
    
    #endregion 
}

public enum WorldAction
{
    Save,
    Load,
    Clear,
    Map
}