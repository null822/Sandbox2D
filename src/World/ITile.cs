using Sandbox2D.Graphics.Registry;
using Sandbox2D.Graphics.Renderables;
using Sandbox2D.Maths;

namespace Sandbox2D.World;

public interface ITile
{
    /// <summary>
    /// The name of the tile
    /// </summary>
    public string Name => "unnamed";
    
    /// <summary>
    /// The name of the tile
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// The renderable to use
    /// </summary>
    protected ref TileRenderable Renderable => ref Renderables.Air;

    /// <summary>
    /// Adds this tile to the supplied renderable, given its pos/size
    /// </summary>
    /// <param name="worldRange">the area this tile takes up, in world coordinates</param>
    public void AddToRenderable(Range2D worldRange)
    {
        Renderable.AddQuad(
            new Vec2<long>(worldRange.MaxX, worldRange.MaxY),
            new Vec2<long>(worldRange.MinX, worldRange.MinY)
        );
    }
    
}
