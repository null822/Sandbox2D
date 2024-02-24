using System;
using System.Threading;
using OpenTK.Windowing.Common.Input;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = OpenTK.Windowing.Common.Input.Image;

namespace Sandbox2D;

public static class Program
{
    private static readonly RenderManager RenderManager = new (800, 600, "Sandbox2D");
    
    private static void Main()
    {
        Console.Clear();
        
        var image = (Image<Rgba32>)SixLabors.ImageSharp.Image.Load("assets/icon.png");
        
        var pixels = new byte[4 * image.Width * image.Height];
        image.CopyPixelDataTo(pixels);

        var icon = new WindowIcon(new Image(1024, 1024, pixels));

        RenderManager.Icon = icon;
        RenderManager.VSync = Constants.Vsync;
        
        
        // start up game logic
        var gameLogicThread = new Thread(GameManager.Run)
        {
            Name = "Logic Thread",
            IsBackground = true,
        };
        
        gameLogicThread.Start();

        // start the RenderManager
        RenderManager.Run();
        
    }
    
    public static RenderManager Get()
    {
        return RenderManager;
    }
    
}