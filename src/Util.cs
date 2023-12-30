#nullable enable
using System;
using Sandbox2D.Maths;

namespace Sandbox2D;

public static class Util
{
    private const ConsoleColor ErrorColor = ConsoleColor.Red;
    private const ConsoleColor WarnColor = ConsoleColor.Yellow;
    private const ConsoleColor DebugColor = ConsoleColor.Green;
    private const ConsoleColor LogColor = ConsoleColor.White;

    private const ConsoleColor DefaultColor = ConsoleColor.White;


    /// <summary>
    /// Converts coords from the screen (like mouse pos) into game coords (like positions of objects)
    /// </summary>
    /// <param name="screenCords">The coords from the screen to convert</param>
    public static Vec2Long ScreenToGameCoords(Vec2Int screenCords)
    {
        var center = Program.Get().GetScreenSize() / 2;
        return (Vec2Long)((Vec2Double)(screenCords - center) / MainWindow.GetScale() - MainWindow.GetTranslation() + center);
    }
    
    /// <summary>
    /// Converts coords from the game (like positions of objects) into screen coords (like mouse pos)
    /// </summary>
    /// <param name="gameCoords">The coords from the game to convert</param>
    public static Vec2Long GameToScreenCoords(Vec2Long gameCoords)
    {
        var center = (Vec2Float)Program.Get().GetScreenSize() / 2f;
        return (Vec2Long)((gameCoords + MainWindow.GetTranslation() - center) * MainWindow.GetScale() + center);
    }
    
    public static void Error(object text)
    {
        Console.ForegroundColor = ErrorColor;
        Console.Out.WriteLine($"[Error]: {text}");
        Console.ForegroundColor = DefaultColor;
    }
    
    public static void Warn(object text)
    {
        Console.ForegroundColor = WarnColor;
        Console.Out.WriteLine($"[Warn ]: {text}");
        Console.ForegroundColor = DefaultColor;
    }
    
    public static void Debug(object text)
    {
        Console.ForegroundColor = DebugColor;
        Console.Out.WriteLine($"[Debug]: {text}");
        Console.ForegroundColor = DefaultColor;
    }
    
    public static void Log(object text)
    {
        Console.ForegroundColor = LogColor;
        Console.Out.WriteLine($"[ Log ]: {text}");
        Console.ForegroundColor = DefaultColor;
    }
    
    public static void Error(string format, object arg0)
    {
        Console.ForegroundColor = ErrorColor;
        Console.Out.Write("[Error]: ");
        Console.Out.Write(format, arg0);
        Console.ForegroundColor = DefaultColor;
    }
    
    public static void Warn(string format, object arg0)
    {
        Console.ForegroundColor = WarnColor;
        Console.Out.Write("[Warn ]: ");
        Console.Out.Write(format, arg0);
        Console.ForegroundColor = DefaultColor;
    }
    
    public static void Debug(string format, object arg0)
    {
        Console.ForegroundColor = DebugColor;
        Console.Out.Write("[Debug]: ");
        Console.Out.Write(format, arg0);
        Console.ForegroundColor = DefaultColor;
    }
    
    public static void Log(string format, object arg0)
    {
        Console.ForegroundColor = LogColor;
        Console.Out.Write("[ Log ]: ");
        Console.Out.Write(format, arg0);
        Console.ForegroundColor = DefaultColor;
    }
    
}