using System;
using Sandbox2D.Graphics.Registry;
using Sandbox2D.Graphics.Renderables;
using Sandbox2D.Maths;
using static Sandbox2D.Util;

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
    public uint Renderable { get; }

    /// <summary>
    /// Adds this tile to the supplied renderable, given its pos/size
    /// </summary>
    /// <param name="worldRange">the area this tile takes up, in world coordinates</param>
    /// <param name="renderableId">[optional] the id of the renderable to add this tile to instead of the default for this tile</param>
    public void AddToRenderable(Range2D worldRange, uint? renderableId = null)
    {
        ref var renderable = ref Renderables.Get(renderableId ?? Renderable);
        
        switch (renderable)
        {
            case GameObjectRenderable gameObjectRenderable:
            {
                // create and add the new quad to the renderable
                gameObjectRenderable.AddQuad(
                    new Vec2<long>(worldRange.MaxX, worldRange.MaxY),
                    new Vec2<long>(worldRange.MinX, worldRange.MinY)
                    );
                
                return;
            }
            case BaseRenderable baseRenderable:
            {
                // get the tile's pos and size, in screen coordinates
                var tileScreenTl = WorldToScreenCoords(new Vec2<long>(worldRange.MaxX, worldRange.MaxY));
                var tileScreenBr = WorldToScreenCoords(new Vec2<long>(worldRange.MinX, worldRange.MinY));

                // get the top left and bottom right coords of the tile in vertex coordinates
                var tileVertexPosTl = ScreenToVertexCoords(tileScreenTl);
                var tileVertexPosBr = ScreenToVertexCoords(tileScreenBr);

                // create and add the new quad to the renderable
                baseRenderable.AddQuad(tileVertexPosTl, tileVertexPosBr);
                
                return;
            }
        }
    }
    
}
