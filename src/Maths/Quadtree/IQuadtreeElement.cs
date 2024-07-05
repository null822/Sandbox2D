
namespace Sandbox2D.Maths.Quadtree;

public interface IQuadtreeElement<in T>
{
    public bool CanCombine(T other);
}