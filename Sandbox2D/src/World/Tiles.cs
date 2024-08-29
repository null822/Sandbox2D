using System;
using Math2D;
using Sandbox2D.Graphics;
using static Sandbox2D.World.TileId;

namespace Sandbox2D.World;

public partial struct Tile
{
    public Tile(TileId id, ulong data = 0)
    {
        _data = new TileData((ushort)id, data);
    }
    
    private static Color GetColor(Tile tile)
    {
        return (TileId)tile.Id switch
        {
            Air => Color.Black,
            Dirt => Color.Brown,
            Stone => Color.Gray,
            Paint => new Color((uint)(tile.Data & (~0x0uL >> (64 - 24)))),
            _ => Color.Red
        };
    }
    
    public override string ToString()
    {
        return (TileId)Id switch
        {
            Paint => $"Paint: #{Data & 0xFFFFFF:x6}",
            _ => ((TileId)Id).ToString(),
        };
    }
}

public enum TileId : ushort
{
    Air = 0,
    Dirt = 1,
    Stone = 2,
    Paint = 3,
}