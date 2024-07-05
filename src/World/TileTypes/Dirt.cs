using System.Runtime.InteropServices;
using Sandbox2D.Graphics;

namespace Sandbox2D.World.TileTypes;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Dirt : ITile
{
    public string Name => "Dirt";
    public Tile Tile => new(1, 0);
    
    public Dirt()
    {
        
    }
    
    public Color GetColor()
    {
        return Color.Brown;
    }
    
    public override string ToString()
    {
        return Name;
    }
}
