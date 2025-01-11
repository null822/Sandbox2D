using Sandbox2D.Registry_;
using Sandbox2DTest.World;
using Sandbox2DTest.World.Tiles;

namespace Sandbox2DTest.RegistryPopulators;

public class ServerRegistryPopulator : IRegistryPopulator
{
    /// <summary>
    /// Registers everything, unless everything is already registered.
    /// </summary>
    public void Register()
    {
        TileDeserializer.Register(Air.Id, bytes => new Air(bytes));
        TileDeserializer.Register(Dirt.Id, bytes => new Dirt(bytes));
        TileDeserializer.Register(Stone.Id, bytes => new Stone(bytes));
        TileDeserializer.Register(Paint.Id, bytes => new Paint(bytes));
    }
}
