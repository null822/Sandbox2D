using Sandbox2D.Graphics;
using Sandbox2D.Maths.Quadtree;
using Sandbox2D.Maths.Quadtree.FeatureNodeTypes;

namespace Sandbox2D.World;

public interface ITile : IQuadtreeElement<ITile>, IFeatureCellularAutomata, IFeatureFileSerialization, IFeatureGpuSerialization, IFeatureElementColor
{
    /// <summary>
    /// The name of the tile
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// The data of the tile
    /// </summary>
    public Tile Tile { get; }
    
    
    Tile IFeatureGpuSerialization.GpuSerialize()
    {
        return Tile;
    }
    
    bool IQuadtreeElement<ITile>.CanCombine(ITile other)
    {
        return Tile.Equals(other.Tile);
    }
    
    Color IFeatureElementColor.GetColor()
    {
        return Color.Lime;
    }
}