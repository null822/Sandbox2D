using Math2D;

namespace Sandbox2DTest.World.Tiles;

public class Air : Tile
{
    public Air() : base(new TileData(Id)) {}
    public Air(Span<byte> bytes) : base(bytes) { }

    public const ushort Id = 0;
    
    public override Color GetColor()
    {
        return Color.Black;
    }
    
    public override string ToString()
    {
        return "Air";
    }
}