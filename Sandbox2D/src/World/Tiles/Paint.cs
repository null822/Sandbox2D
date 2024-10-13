using System;
using Math2D;

namespace Sandbox2D.World.Tiles;

public class Paint : Tile
{
    public Paint(Color color) : base(new TileData(Id, color.Decimal)) {}
    public Paint(Span<byte> bytes) : base(bytes) { }
    
    public const ushort Id = 3;
    
    public override Color GetColor()
    {
        return new Color((uint)(TileData.Data & 0xFFFFFF));
    }
    
    public override string ToString()
    {
        return $"Paint: #{TileData.Data & 0xFFFFFF:x6}";
    }
}
