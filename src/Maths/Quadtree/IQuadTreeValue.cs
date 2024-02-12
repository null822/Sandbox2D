#nullable enable
using System;

namespace Sandbox2D.Maths.Quadtree;

public interface IQuadTreeValue<T> where T : IQuadTreeValue<T>
{
    /// <summary>
    /// Checks if this object is equal to the supplied object
    /// </summary>
    /// <param name="a">the other object</param>
    public bool Equals(T a);
    
    /// <summary>
    /// Converts this instance into a span of bytes
    /// </summary>
    public ReadOnlySpan<byte> Serialize();
    
    /// <summary>
    /// Converts a span of bytes back into an instance of IQuadTreeValue
    /// </summary>
    /// <param name="bytes">the bytes to convert</param>
    public static abstract T Deserialize(ReadOnlySpan<byte> bytes);
    
    /// <summary>
    /// The size, in bytes, of the serialized IQuadTreeValue
    /// </summary>
    public static abstract uint SerializeLength { get; }
    
    /// <summary>
    /// The size, in bytes, of the serialized IQuadTreeValue
    /// </summary>
    public uint LinearSerializeId { get; }
}