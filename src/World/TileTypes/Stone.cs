using Sandbox2D.Graphics.Registry;
using Sandbox2D.Graphics.Renderables;

namespace Sandbox2D.World.TileTypes;

public class Stone : IBlockMatrixTile
{
    public string Name => "Stone";
    
    public uint Id => 1;

    public ref TileRenderable Renderable => ref Renderables.Stone;
    
}
