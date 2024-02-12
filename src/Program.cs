
using System;
using OpenTK.Windowing.Common.Input;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = OpenTK.Windowing.Common.Input.Image;

namespace Sandbox2D;

public static class Program
{

    private static MainWindow _mainWindow;
    
    private static void Main(string[] args)
    {
        Console.Clear();

        _mainWindow = args.Length >= 1 ?
            new MainWindow(800, 600, "Sandbox2D", args[0]) :
            new MainWindow(800, 600, "Sandbox2D");

        var image = (Image<Rgba32>)SixLabors.ImageSharp.Image.Load("assets/icon.png");
        
        var pixels = new byte[4 * image.Width * image.Height];
        image.CopyPixelDataTo(pixels);

        var icon = new WindowIcon(new Image(1024, 1024, pixels));

        _mainWindow.Icon = icon;
        
        _mainWindow.VSync = Constants.Vsync;
        _mainWindow.Run();
    }

    public static MainWindow Get()
    {
        return _mainWindow;
    }
    
}