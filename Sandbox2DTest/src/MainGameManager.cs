using Math2D;
using Math2D.Quadtree;
using Math2D.Quadtree.Features;
using Sandbox2D;
using Sandbox2D.Managers;
using Sandbox2DTest.Packets;
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
            CallRenderManagers<MainRenderManager, WorldModification[]>(r => r.GetWorldModifications());
        
        foreach (var modifications in incomingModifications)
        {
            foreach (var modification in modifications.Response)
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
        
        // get the new packets
        var incomingPackets = CallRenderManagers<MainRenderManager, LocalPacket[]>(
            r => r.GetOutgoingPackets());
        
        // process the packets
        var outgoingPackets = new List<RenderManagerCall<MainRenderManager>>();
        foreach (var (managerId, localPackets) in incomingPackets)
        {
            var responses = localPackets
                .Select(HandleAction)
                .Where(p => p != null)
                .Select(p => p!.Value)
                .ToArray();
            
            if (responses.Length != 0)
            {
                outgoingPackets.Add(new RenderManagerCall<MainRenderManager>(managerId,
                    m => m.AddIncomingPackets(responses)));
            }
        }
        
        // send back the responses
        CallRenderManagers(outgoingPackets);
        
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
    
    private LocalPacket? HandleAction(LocalPacket action)
    {
        switch (action.Type) 
        {
            case LocalPacketType.Save: 
            {
                var save = File.Create(action.GetArg<string>());
                new SerializableQuadtree<Tile>(_world).Serialize(save);
                save.Close();
                save.Dispose();
                
                Log("QuadTree Saved"); 
                break;
            }
            case LocalPacketType.Load: 
            { 
                var save = File.Open(action.GetArg<string>(), FileMode.Open);
                _world.Dispose();
                _world = SerializableQuadtree<Tile>.Deserialize<Tile>(save, true).Base;
                WorldHeight = _world.MaxHeight;
                save.Close();
                save.Dispose();
                
                Log("QuadTree Loaded");
                break;
            }
            case LocalPacketType.Clear:
            {
                _world.Clear();
                
                Log("World Cleared");
                break;
            }
            case LocalPacketType.Map:
            {
                var svgScale = (decimal)Constants.QuadTreeSvgSize / ~(WorldHeight == 64 ? 0 : ~0x0uL << WorldHeight);
                var svgMap = new MappableQuadtree<Tile>(_world).GetSvgMap(svgScale);
                
                var map = File.CreateText(action.GetArg<string>());
                map.Write(svgMap);
                map.Close();
                map.Dispose();
                
                Log("QuadTree Mapped");
                break; 
            }
            case LocalPacketType.GetTile:
            {
                return new LocalPacket(
                    action.ResponseName ?? "Requested Tile",
                    LocalPacketType.Tile,
                    _world[action.GetArg<Vec2<long>>()]);
            }
        }

        return null;
    }
    
    #region Public Getters / Setters
    
    public override void OnClose()
    {
        _world.Dispose();
    }
    
    #endregion
}



