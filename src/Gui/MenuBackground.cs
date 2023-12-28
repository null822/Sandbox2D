using System;
using ElectroSim.Maths;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Sandbox2D.Maths;
using Sandbox2D.Registry;

namespace Sandbox2D.Gui;

/// <summary>
/// The parent class of all components.
/// </summary>
public class MenuBackground
{
    private readonly ScalableValue2 _pos;
    private readonly ScalableValue2 _size;
    
    public MenuBackground(ScalableValue2 pos, ScalableValue2 size)
    {
        _pos = pos;
        _size = size;
    }

    /// <summary>
    /// Renders the component.
    /// </summary>
    /// <param name="spriteBatch">The spritebatch to render to</param>
    public void Render(SpriteBatch spriteBatch)
    {
        Vector2 pos = _pos;
        Vector2 size = _size;
        
        const int tileSize = Constants.MenuBackgroundTileSize * Constants.MenuBackgroundZoom;

        var maxX = (int)Math.Floor(size.X - tileSize);
        var maxY = (int)Math.Floor(size.Y - tileSize);

        for (var x = 0; x < size.X; x+=tileSize)
        {
            for (var y = 0; y < size.Y; y+=tileSize)
            {
                Texture2D texture;
                var rotation = 0;

                //  TOP      RIGHT       BOTTOM   LEFT
                if (y == 0 ^ x >= maxX ^ x == 0 ^ y >= maxY)
                {
                    texture = TextureRegistry.GetTexture("gui/edge");

                    if (y == 0) rotation = 0;
                    else if (x >= maxX) rotation = 90;
                    else if (y >= maxY) rotation = 180;
                    else if (x == 0) rotation = 270;
                    
                }
                //        TOP LEFT              TOP RIGHT                BOTTOM RIGHT                BOTTOM LEFT              
                else if ((x == 0 && y == 0) || (x >= maxX && y == 0) || (x >= maxX && y >= maxY) || (x == 0 && y >= maxY) )
                {
                    texture = TextureRegistry.GetTexture("gui/corner");
                    
                    if (x == 0 && y == 0) rotation = 0;
                    else if (x >= maxX && y == 0) rotation = 90;
                    else if (x >= maxX && y >= maxY) rotation = 180;
                    else if (x == 0 && y >= maxY) rotation = 270;
                    
                }
                else
                {
                    texture = TextureRegistry.GetTexture("gui/center");
                }

                var rotationCompensation = rotation switch
                {
                    90 => new Vector2(1, 0),
                    180 => new Vector2(1, 1),
                    270 => new Vector2(0, 1),
                    _ => new Vector2(0, 0)
                } * tileSize;

                var rotationRad = rotation / (float)(180 / Math.PI);
                
                
                spriteBatch.Draw(
                    texture,
                    pos + new Vector2(x, y) + rotationCompensation,
                    null,
                    Color.White,
                    rotationRad,
                    new Vector2(0),
                    new Vector2(Constants.MenuBackgroundZoom),
                    SpriteEffects.None,
                    0f
                );
            }
        }
    }

    public ScalableValue2 GetPos()
    {
        return _pos;
    }
    
    public ScalableValue2 GetSize()
    {
        return _size;
    }
    
    
    /// <summary>
    /// Creates a copy of the component
    /// </summary>
    public MenuBackground Copy()
    {
        return new MenuBackground(_pos, _size);
    }

}