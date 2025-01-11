using Math2D;

namespace Sandbox2DTest.World.Tiles;

public class Dirt : Tile
{
    public Dirt() : base(new TileData(Id)) {}
    public Dirt(Span<byte> bytes) : base(bytes) { }
    
    public const ushort Id = 1;
    
    public override Color GetColor()
    {
        return Color.Brown;
    }
    
    public override string ToString()
    {
        return "Dirt";
    }
}