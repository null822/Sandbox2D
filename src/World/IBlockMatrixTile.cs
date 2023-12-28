using System;
using Sandbox2D.Maths.BlockMatrix;
using Sandbox2D.World.Tiles;

namespace Sandbox2D.World;

public interface IBlockMatrixTile : IBlockMatrixElement<IBlockMatrixTile>, ITile
{
    
    // ITile


    // IBlockMatrixElement
    
    static bool IBlockMatrixElement<IBlockMatrixTile>.operator !=(IBlockMatrixTile a, IBlockMatrixTile b)
    {
        return false;
    }

    static bool IBlockMatrixElement<IBlockMatrixTile>.operator ==(IBlockMatrixTile a, IBlockMatrixTile b)
    {
        return false;
    }

    
    ReadOnlySpan<byte> IBlockMatrixElement<IBlockMatrixTile>.Serialize()
    {
        return new Span<byte>([0]);
    }
    
    static IBlockMatrixTile IBlockMatrixElement<IBlockMatrixTile>.Deserialize(ReadOnlySpan<byte> bytes)
    {
        return new Air();
    }

    static uint IBlockMatrixElement<IBlockMatrixTile>.SerializeLength => 1;
}