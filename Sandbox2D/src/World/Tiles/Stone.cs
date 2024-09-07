using System;
using Math2D;
using Sandbox2D.Registry;

namespace Sandbox2D.World.Tiles;

public class Stone : Tile
{
    public Stone() : base(new TileData(TileType.Stone)) {}
    public Stone(Span<byte> bytes) : base(bytes) { }
    
    public override Color GetColor()
    {
        return Color.Gray;
    }
    
    public override string ToString()
    {
        return "Gray";
    }
}