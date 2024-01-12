using System;
using System.Collections.Generic;

namespace Sandbox2D.World;

public static class Tiles
{
    private static readonly Dictionary<uint, ITile> IdTile = new();
    
    public static ITile GetTile(uint id)
    {
        return IdTile.TryGetValue(id, out var tile) ? tile : NoId(id);
    }
    
    private static ITile NoId(uint id)
    {
        Util.Error($"The tile of id {id} does not exist");
        return IdTile[0];
    }
    
    public static void Initialize(IEnumerable<ITile> tiles)
    {
        uint id = 0;
        foreach (var tile in tiles)
        {
            IdTile.Add(id, tile);
            
            Console.WriteLine(tile.Name);

            id++;
        }
    }
}