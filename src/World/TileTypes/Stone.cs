using Sandbox2D.Graphics.Registry;

namespace Sandbox2D.World.TileTypes;

public class Stone : IBlockMatrixTile
{
    public string Name => "Stone";
    
    public uint Id => 1;

    public uint Renderable { get; } = Renderables.GetId("game_object");
    
}
