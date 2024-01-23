using Sandbox2D.Maths;

namespace Sandbox2D.World.TileTypes;

public class Air : IBlockMatrixTile
{
    public string Name => "Air";

    public uint Id => 0;
    
    public void AddToRenderable(Range2D worldRange)
    {
        // override the AddToRenderable method to not do anything: air will not be rendered
    }
}
