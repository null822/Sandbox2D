using System;
using Math2D;
using Math2D.Quadtree;
using Math2D.Quadtree.FeatureNodeTypes;
using Sandbox2D.Graphics;

namespace Sandbox2D.World;

public readonly partial struct Tile : IQuadtreeElement<Tile>,
    IFeatureModificationStore,
    IFeatureFileSerialization<Tile>,
    IFeatureElementColor,
    IFeatureCellularAutomata
{
    
    /// <summary>
    /// The data of this tile.
    /// </summary>
    private readonly TileData _data;
    
    /// <summary>
    /// The id of this tile. 16 bits total.
    /// </summary>
    public ushort Id => _data.Id;
    /// <summary>
    /// The data of this tile. 48 bits total.
    /// </summary>
    public ulong Data => _data.Data;
    
    
    public int SerializeLength => 8;
    
    
    private Tile(TileData data)
    {
        _data = data;
    }
    
    public byte[] Serialize()
    {
        return _data.Serialize();
    }
    
    public static Tile Deserialize(Span<byte> bytes)
    {
        return new Tile(new TileData(BitUtil.GetULong(bytes)));
    }
    
    public TileData GpuSerialize()
    {
        return _data;
    }
    
    bool IQuadtreeElement<Tile>.CanCombine(Tile other)
    {
        return _data.Equals(other._data);
    }
    
    Color IFeatureElementColor.GetColor()
    {
        return GetColor(this);
    }
}