using Microsoft.Xna.Framework;

namespace Sandbox2D;

public static class Program
{

    private static Game _game;

    private static void Main()
    {
        _game = new MainWindow();
        _game.Run();
    }
    
}