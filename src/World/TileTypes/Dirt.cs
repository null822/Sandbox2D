using Sandbox2D.Graphics.Registry;
using Sandbox2D.Graphics.Renderables;

namespace Sandbox2D.World.TileTypes;

public class Dirt : IBlockMatrixTile
{
    public string Name => "Dirt";
    
    public uint Id => 2;
    
    public ref TileRenderable Renderable => ref Renderables.Dirt;

}
