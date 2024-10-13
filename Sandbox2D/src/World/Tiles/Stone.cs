using System;
using Math2D;

namespace Sandbox2D.World.Tiles;

public class Stone : Tile
{
    public Stone() : base(new TileData(Id)) {}
    public Stone(Span<byte> bytes) : base(bytes) { }
    
    public const ushort Id = 2;
    
    public override Color GetColor()
    {
        return Color.Gray;
    }
    
    public override string ToString()
    {
        return "Gray";
    }
}