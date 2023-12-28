using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Sandbox2D.Maths;
using Sandbox2D.Registry;

namespace Sandbox2D.World;

public interface ITile
{
    public string Texture => "missing";
    public string Name => "unnamed";

    
    public void Render(SpriteBatch spriteBatch, Vec2Long pos, Color? tint = null)
    {
        var tintNonNull = tint ?? Color.White;
        var texture = TextureRegistry.GetTexture(Texture);
        
        var scaleVec = new Vec2Double(MainWindow.GetScale()) / 32;
        
        var screenPos = Util.GameToScreenCoords(pos);
        
        spriteBatch.Draw(
            texture,
            screenPos,
            null,
            tintNonNull,
            0f,
            new Vec2Float(0),
            scaleVec,
            SpriteEffects.None,
            0f
        );
    }

}
