
namespace Math2D.Quadtree;

public interface IQuadtreeElement<in T>
{
    public bool CanCombine(T other);
    
    /// <summary>
    /// The maximum length of a single array of <see cref="IQuadtreeElement{T}"/>s, intended to prevent
    /// <see cref="DynamicArray{T}"/>s of <see cref="QuadtreeNode"/>s from being allocated to the LOH
    /// </summary>
    public static abstract int MaxChunkSize { get; }
}