using System;

namespace Sandbox2D.Maths.Quadtree.FeatureNodeTypes;

/// <summary>
/// Enables the use of <see cref="Quadtree{T}.Serialize"/> and <see cref="Quadtree{T}.Deserialize{T2}"/>, allowing for
/// the entire <see cref="Quadtree{T}"/> to be saved to a stream, and be read back into a new <see cref="Quadtree{T}"/>.
/// </summary>
/// <typeparam name="T">the type implementing this interface</typeparam>
public interface IFeatureFileSerialization<out T>
{
    /// <summary>
    /// The length, in bytes, of the result of <see cref="Serialize"/>.
    /// </summary>
    public int SerializeLength { get; }
    
    /// <summary>
    /// Returns this <see cref="T"/>, serialized to a <see cref="byte"/>[] of length
    /// <see cref="SerializeLength"/>.
    /// </summary>
    public byte[] Serialize();
    
    /// <summary>
    /// Returns a new <see cref="T"/>, deserialized from a sequence of bytes.
    /// </summary>
    public static abstract T Deserialize(Span<byte> bytes);
}
