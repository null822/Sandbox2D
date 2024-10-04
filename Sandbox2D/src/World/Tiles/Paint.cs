using System;
using Math2D;
using Sandbox2D.Registry;

namespace Sandbox2D.World.Tiles;

public class Paint : Tile
{
    public Paint(Color color) : base(new TileData(TileType.Paint, color.Decimal)) {}
    public Paint(Span<byte> bytes) : base(bytes) { }
    
    public override Color GetColor()
    {
        return new Color((uint)(TileData.Data & 0xFFFFFF));
    }
    
    public override string ToString()
    {
        return $"Paint: #{TileData.Data & 0xFFFFFF:x6}";
    }
}
