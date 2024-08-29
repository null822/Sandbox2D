
namespace Math2D.Quadtree;

public interface IQuadtreeElement<in T>
{
    public bool CanCombine(T other);
}