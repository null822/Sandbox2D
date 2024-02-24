using System;
using Sandbox2D.Maths.Quadtree;

namespace Sandbox2D.World;

/// <summary>
/// Represents a Tile
/// </summary>
public interface ITile : IQtSerializable<ITile>
{
    /// <summary>
    /// The id of the tile
    /// </summary>
    public int Id { get; }
    
    /// <summary>
    /// The name of the tile
    /// </summary>
    public string Name { get; }
    
    
    public ITile Get()
    {
        return Tiles.GetTile(Id);
    }
    
    bool IQtSerializable<ITile>.Equals(IQuadTreeValue<ITile> a)
    {
        return Id == a.Get().Id;
    }
    
    ReadOnlySpan<byte> IQtSerializable<ITile>.Serialize()
    {
        return BitConverter.GetBytes(Id);
    }
    
    static ITile IQtSerializable<ITile>.Deserialize(ReadOnlySpan<byte> bytes)
    {
        return Tiles.GetTile(BitConverter.ToInt32(bytes));
    }
    
    static uint IQtSerializable<ITile>.SerializeLength => sizeof(int);
    
    uint IQtSerializable<ITile>.LinearSerialize()
    {
        return (uint)Id;
    }
}