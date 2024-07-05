using System.Runtime.InteropServices;
using Sandbox2D.Graphics;

namespace Sandbox2D.World.TileTypes;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Stone : ITile
{
    public string Name => "Stone";
    public Tile Tile => new(2, 0);
    
    public Stone()
    {
        
    }
    
    public Color GetColor()
    {
        return Color.Gray;
    }
    
    public override string ToString()
    {
        return Name;
    }
}
