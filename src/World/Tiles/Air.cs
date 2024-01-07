using System;
using Sandbox2D.Graphics;
using Sandbox2D.Maths;

namespace Sandbox2D.World.Tiles;

public class Air : IBlockMatrixTile
{
    public static string Name => "Air";

    // override the Renderable to be null, as air will not be rendered

    public void AddToRenderable(Range2D worldRange, Renderable renderable)
    {
        // override the AddToRenderable method to not do anything: air has no texture
    }
}
