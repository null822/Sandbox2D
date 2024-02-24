using System.Collections.Generic;

namespace Sandbox2D.World;

public static class Tiles
{
    private static readonly List<ITile> TilesList = [];
    
    public static ITile GetTile(int id)
    {
        if (TilesList.Count < id)
        {
            Util.Error($"The tile of id {id} does not exist");
            return TilesList[0];
        }
        
        return TilesList[id];
    }
    
    public static void Instantiate(IEnumerable<ITile> tiles)
    {
        TilesList.AddRange(tiles); 
        
        Util.Log("Created Tiles");
    }
}