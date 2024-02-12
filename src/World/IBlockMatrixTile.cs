using System;
using Sandbox2D.Maths.Quadtree;
using Sandbox2D.World.TileTypes;

namespace Sandbox2D.World;

public interface IBlockMatrixTile : IQuadTreeValue<IBlockMatrixTile>, ITile
{
    
    ReadOnlySpan<byte> IQuadTreeValue<IBlockMatrixTile>.Serialize()
    {
        // return the id, as bytes
        return BitConverter.GetBytes(Id);
    }
    
    static IBlockMatrixTile IQuadTreeValue<IBlockMatrixTile>.Deserialize(ReadOnlySpan<byte> bytes)
    {
        // get the tile
        var tile = Tiles.GetTile(BitConverter.ToUInt32(bytes));
        
        // if it is an IBlockMatrixTile, return it, otherwise, return air
        return tile as IBlockMatrixTile ?? new Air();
    }

    static uint IQuadTreeValue<IBlockMatrixTile>.SerializeLength => sizeof(uint);
    
    uint IQuadTreeValue<IBlockMatrixTile>.LinearSerializeId => Id;


    bool IQuadTreeValue<IBlockMatrixTile>.Equals(IBlockMatrixTile a)
    {
        return Id == a.Id;
    }
    
}