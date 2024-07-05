using System.Runtime.InteropServices;
using Sandbox2D.Graphics;

namespace Sandbox2D.World.TileTypes;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Air : ITile
{
    public string Name => "Air";
    public Tile Tile => new(0, 0);
    
    public Air()
    {
        
    }
    
    public Color GetColor()
    {
        return Color.Black;
    }
    
    public override string ToString()
    {
        return Name;
    }
}
