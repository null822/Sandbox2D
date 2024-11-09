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
    
    protected Tile(TileData data)
    {
        TileData = data;
    }
    
    protected Tile(Span<byte> bytes)
    {
        TileData = new TileData(bytes);
    }
    
    public abstract Color GetColor();
    
    public bool CanCombine(Tile other)
    {
        return TileData.Equals(other.TileData);
    }
    
    public static int SerializeLength => 8;
    
    public byte[] Serialize(bool bigEndian = false)
    {
        return TileData.Serialize(bigEndian);
    }
    
    public static Tile Deserialize(byte[] bytes, bool bigEndian = false)
    {
        return TileDeserializer.Create(bytes, bigEndian);
    }
}