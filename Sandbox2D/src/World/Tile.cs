using System;
using Math2D;
using Math2D.Quadtree;
using Math2D.Quadtree.FeatureNodeTypes;

namespace Sandbox2D.World;

public abstract class Tile : IQuadtreeElement<Tile>,
    IFeatureModificationStore,
    IFeatureFileSerialization<Tile>,
    IFeatureElementColor,
    IFeatureCellularAutomata
{
    /// <summary>
    /// The data of this tile
    /// </summary>
    protected readonly TileData TileData;
    
    public static int MaxChunkSize => 10_000; /* 80kb per chunk */
    public int SerializeLength => 8;
    
    protected Tile(TileData data)
    {
        TileData = data;
    }
    
    protected Tile(Span<byte> bytes)
    {
        TileData = new TileData(bytes);
    }
    
    public byte[] Serialize()
    {
        return TileData.Serialize();
    }
    
    public static Tile Deserialize(Span<byte> bytes)
    {
        return TileDeserializer.Create(bytes);
    }
    
    public TileData GpuSerialize()
    {
        return TileData;
    }
    
    public bool CanCombine(Tile other)
    {
        return TileData.Equals(other.TileData);
    }
    
    public abstract Color GetColor();
}