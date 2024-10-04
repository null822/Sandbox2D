using System;
using Math2D;
using Sandbox2D.Registry;

namespace Sandbox2D.World.Tiles;

public class Air : Tile
{
    public Air() : base(new TileData(TileType.Air)) {}
    public Air(Span<byte> bytes) : base(bytes) { }
    
    public override Color GetColor()
    {
        return Color.Black;
    }
    
    public override string ToString()
    {
        return "Air";
    }
}