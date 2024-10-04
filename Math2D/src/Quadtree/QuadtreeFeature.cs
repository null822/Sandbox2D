namespace Math2D.Quadtree;

/// <summary>
/// Provides support for adding functionality to <see cref="Quadtree{T}"/> with access to its raw underlying data.
/// </summary>
/// <typeparam name="T">the type of the data stored in the <see cref="Quadtree{T}"/></typeparam>
public abstract class QuadtreeFeature<T> where T : IQuadtreeElement<T>
{
    /// <summary>
    /// The base <see cref="Quadtree{T}"/> this <see cref="QuadtreeFeature{T}"/> builds upon.
    /// </summary>
    public abstract Quadtree<T> Base { get; }
    
    /// <summary>
    /// See <see cref="Quadtree{T}.Tree"/>.
    /// </summary>
    protected DynamicArray<QuadtreeNode> Tree => Base.Tree;
    /// <summary>
    /// See <see cref="Quadtree{T}.Data"/>.
    /// </summary>
    protected DynamicArray<T> Data => Base.Data;

    /// <summary>
    /// See <see cref="Quadtree{T}.MaxHeight"/>.
    /// </summary>
    public int MaxHeight => Base.MaxHeight;
    
    /// <summary>
    /// See <see cref="Quadtree{T}.Dimensions"/>.
    /// </summary>
    public Range2D Dimensions => Base.Dimensions;
    
    /// <summary>
    /// See <see cref="Quadtree{T}.GetNextNode"/>.
    /// </summary>
    protected (UInt128 zValue, int height, long nodeRef) GetNextNode(
        UInt128 zValue, int height, UInt128 maxZValue, ref long[] path)
        => Base.GetNextNode(zValue, height, maxZValue, ref path);
    
    /// <summary>
    /// See <see cref="Quadtree{T}.GetNextNodePos"/>.
    /// </summary>
    protected Vec2<long> GetNextNodePos(
        Vec2<long> nodePos, UInt128 zValue, int height)
        => Base.GetNextNodePos(nodePos, zValue, height);
    
    /// <summary>
    /// See <see cref="Quadtree{T}.GetNodeRef"/>.
    /// </summary>
    protected long GetNodeRef(
        UInt128 zValue, int targetHeight = 0, bool readOnly = false, long[]? path = null)
        => Base.GetNodeRef(zValue, targetHeight, readOnly, path);
}
