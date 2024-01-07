using System;
using OpenTK.Graphics.OpenGL4;
using Sandbox2D.Graphics;
using Sandbox2D.Graphics.Registry;
using Sandbox2D.Maths;
using static Sandbox2D.Util;

namespace Sandbox2D.World;

public interface ITile
{
    /// <summary>
    /// The name of the tile
    /// </summary>
    public static string Name => "unnamed";
    
    /// <summary>
    /// Adds this tile to the supplied renderable, given its pos/size
    /// </summary>
    /// <param name="worldRange">the area this tile takes up in world coordinates</param>
    /// <param name="renderable">the renderable to add this tile to</param>
    public void AddToRenderable(Range2D worldRange, Renderable renderable)
    {
        // get the tile's pos and size, in screen coordinates
        var tileScreenTl = WorldToScreenCoords(new Vec2Long(worldRange.MinX, worldRange.MinY));
        var tileScreenBr = WorldToScreenCoords(new Vec2Long(worldRange.MaxX, worldRange.MaxY));
        
        // get the top left and bottom right coords of the tile in vertex coordinates
        var tileVertexPosTl = ScreenToVertexCoords(tileScreenTl);
        var tileVertexPosBr = ScreenToVertexCoords(tileScreenBr);
        
        // create and add the new quad to the renderable
        renderable.AddQuad(tileVertexPosTl, tileVertexPosBr);
    }
}
