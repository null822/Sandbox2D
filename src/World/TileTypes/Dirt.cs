using Sandbox2D.Graphics.Registry;

namespace Sandbox2D.World.TileTypes;

public class Dirt : IBlockMatrixTile
{
    public string Name => "Dirt";
    
    public uint Id => 2;

    public uint Renderable { get; } = Renderables.GetId("game_object");
    
}
