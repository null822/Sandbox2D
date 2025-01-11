using Math2D;
using Math2D.Quadtree;
using Math2D.Quadtree.Features;
using Sandbox2D;
using Sandbox2D.Managers;
using Sandbox2DTest.World;
using Sandbox2DTest.World.Tiles;
using static Sandbox2D.Util;

namespace Sandbox2DTest;

public class MainGameManager(double tps, RenderManager[] renderManagers) : GameManager(tps, renderManagers)
{
    private Quadtree<Tile> _world = null!;
    public int WorldHeight { get; private set; } = Constants.InitialWorldHeight;
    public Range2D WorldDimensions => _world.Dimensions;
    
    /// <summary>
    /// Updates the game logic.
    /// </summary>
    protected override void Tick()
    {
        // apply any world modifications
        var incomingModifications = 
            RenderManagerGet<MainRenderManager, WorldModification[]>(r => r.GetWorldModifications());
        
        foreach (var modifications in incomingModifications)
        {
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
        }
        
        _world.Compress();
        // _world.UpdateSubset(screenRange); //TODO: implement subset
        
        var actions = RenderManagerGet<MainRenderManager, WorldAction?>(r => r.GetWorldAction());
        foreach (var action in actions)
        {
            HandleAction(action);
        }
        
        UpdateRender();
    }
    
    /// <summary>
    /// Updates the render thread.
    /// </summary>
    private void UpdateRender()
    {
        foreach (var r in RenderManagers)
        {
            if (r is not MainRenderManager renderManager) continue;
            
            if (!renderManager.CanUpdateGeometry()) return;
            
            // update the modifications
            var modificationCount = _world.GetModifications(
                renderManager.TreeModifications,
                renderManager.DataModifications);
            if (modificationCount.Tree != 0 || modificationCount.Data != 0)
                renderManager.SetGeometryUploaded();
            
            // update the geometry parameters
            var renderDepth = Math.Min(WorldHeight, renderManager.MaxGpuQtHeight);
            var (treeLength, dataLength) = _world.GetLength();
            var (renderRoot, renderRange) =
                _world.GetSubset(renderManager.CalculateScreenRange().Overlap(_world.Dimensions), renderDepth);
            renderManager.UpdateGeometryParameters(treeLength, dataLength, renderRoot, renderRange);
            
            renderManager.GeometryLock.Set();
        }
        _world.ClearModifications();
    }
    
    /// <summary>
    /// Initializes everything. Run once when the game launches.
    /// </summary>
    protected override void Initialize()
    {
        // create world
        _world = new Quadtree<Tile>(WorldHeight, new Air(), true);
        WorldHeight = _world.MaxHeight;
        
        Log("Initialized Game Manager", "Load/Logic");
    }
    
    private void HandleAction(WorldAction? action)
    {
        if (action == null)
            return;
        
        switch (action.Type) 
        {
            case WorldActionType.Save: 
            {
                var save = File.Create(action.Arg);
                new SerializableQuadtree<Tile>(_world).Serialize(save);
                save.Close();
                save.Dispose();
                
                Log("QuadTree Saved"); 
                break;
            }
            case WorldActionType.Load: 
            { 
                var save = File.Open(action.Arg, FileMode.Open);
                _world.Dispose();
                _world = SerializableQuadtree<Tile>.Deserialize<Tile>(save, true).Base;
                WorldHeight = _world.MaxHeight;
                save.Close();
                save.Dispose();
                
                Log("QuadTree Loaded");
                break;
            }
            case WorldActionType.Clear:
            {
                _world.Clear();
                
                Log("World Cleared");
                break;
            }
            case WorldActionType.Map:
            {
                var svgScale = (decimal)Constants.QuadTreeSvgSize / ~(WorldHeight == 64 ? 0 : ~0x0uL << WorldHeight);
                var svgMap = new MappableQuadtree<Tile>(_world).GetSvgMap(svgScale);
                
                var map = File.CreateText(action.Arg);
                map.Write(svgMap);
                map.Close();
                map.Dispose();
                
                Log("QuadTree Mapped");
                break; 
            }
        }
    }
    
    #region Public Getters / Setters
    
    public override void OnClose()
    {
        _world.Dispose();
    }
    
    #endregion
}

public enum WorldActionType
{
    Save,
    Load,
    Clear,
    Map
}

public record WorldAction(WorldActionType Type, string Arg);