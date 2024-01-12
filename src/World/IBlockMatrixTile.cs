using System;
using Sandbox2D.Maths.BlockMatrix;
using Sandbox2D.World.TileTypes;

namespace Sandbox2D.World;

public interface IBlockMatrixTile : IBlockMatrixElement<IBlockMatrixTile>, ITile
{
    
    ReadOnlySpan<byte> IBlockMatrixElement<IBlockMatrixTile>.Serialize()
    {
        // return the id, as bytes
        return BitConverter.GetBytes(Id);
    }
    
    static IBlockMatrixTile IBlockMatrixElement<IBlockMatrixTile>.Deserialize(ReadOnlySpan<byte> bytes)
    {
        // get the tile
        var tile = Tiles.GetTile(BitConverter.ToUInt32(bytes));
        
        // if it is an IBlockMatrixTile, return it, otherwise, return air
        return tile as IBlockMatrixTile ?? new Air();
    }

    static uint IBlockMatrixElement<IBlockMatrixTile>.SerializeLength => sizeof(uint);


    bool IBlockMatrixElement<IBlockMatrixTile>.Equals(IBlockMatrixTile a)
    {
        return Id == a.Id;
    }
    
}