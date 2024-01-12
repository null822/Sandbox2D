
namespace Sandbox2D;

public static class Program
{

    private static MainWindow _mainWindow;

    private static void Main()
    {
        _mainWindow = new MainWindow(800, 600, "Sandbox2D");
        _mainWindow.VSync = Constants.Vsync;
        _mainWindow.Run();
    }

    public static MainWindow Get()
    {
        return _mainWindow;
    }
    
}