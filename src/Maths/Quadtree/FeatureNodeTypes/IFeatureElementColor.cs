using Sandbox2D.Graphics;

namespace Sandbox2D.Maths.Quadtree.FeatureNodeTypes;

/// <summary>
/// Assigns a <see cref="Color"/> to every element in a <see cref="Quadtree{T}"/>, allowing for
/// <see cref="Quadtree{T}.GetSvgMap()"/> to use color to discern different elements.
/// </summary>
public interface IFeatureElementColor
{
    public Color GetColor();
}