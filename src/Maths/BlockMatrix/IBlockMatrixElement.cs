#nullable enable
using System;

namespace Sandbox2D.Maths.BlockMatrix;

public interface IBlockMatrixElement<T> where T : IBlockMatrixElement<T>
{
    
    public static abstract bool operator ==(T? a, T? b);
    public static abstract bool operator !=(T? a, T? b);
    
    /// <summary>
    /// Converts this instance into a span of bytes
    /// </summary>
    public ReadOnlySpan<byte> Serialize();
    
    /// <summary>
    /// Converts a span of bytes back into an instance of IBlockMatrixElement
    /// </summary>
    /// <param name="bytes"></param>
    public static abstract T Deserialize(ReadOnlySpan<byte> bytes);
    
    /// <summary>
    /// The size, in bytes, of the serialized IBlockMatrixElement
    /// </summary>
    public static abstract uint SerializeLength { get; }
}