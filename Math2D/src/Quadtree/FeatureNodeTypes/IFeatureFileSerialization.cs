using Math2D.Binary;
using Math2D.Quadtree.Features;

namespace Math2D.Quadtree.FeatureNodeTypes;

/// <summary>
/// Enables the use of <see cref="SerializableQuadtree{T}"/>, allowing for
/// the entire <see cref="Quadtree{T}"/> to be saved to a stream, and be read back into a new <see cref="Quadtree{T}"/>.
/// </summary>
/// <typeparam name="T">the type implementing this interface</typeparam>
public interface IFeatureFileSerialization<out T> : IByteSerializable, IByteDeserializable<T>;
