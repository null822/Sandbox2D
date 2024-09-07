using System;
using Math2D;
using Sandbox2D.Registry;

namespace Sandbox2D.World.Tiles;

public class Dirt : Tile
{
    public Dirt() : base(new TileData(TileType.Dirt)) {}
    public Dirt(Span<byte> bytes) : base(bytes) { }
    
    public override Color GetColor()
    {
        return Color.Brown;
    }
    
    public override string ToString()
    {
        return "Dirt";
    }
}