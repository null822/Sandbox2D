using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Sandbox2D.Maths;

namespace Sandbox2D.World.Tiles;

public class Air : IBlockMatrixTile
{
    public string Texture => "air";
    public string Name => "Air";

    public void Render(SpriteBatch spriteBatch, Vec2Long pos, Color? tint = null)
    {
        
    }

    /*public void Render(SpriteBatch spriteBatch, Vec2Long pos, Color? tint = null)
    {
        
    }*/
}