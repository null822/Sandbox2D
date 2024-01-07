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
    /// <param name="screenCoords">The coords from the screen to convert</param>
    public static Vec2Long ScreenToWorldCoords(Vec2Int screenCoords)
    {
        var center = Program.Get().GetScreenSize() / 2;
        return (Vec2Long)((Vec2Double)(screenCoords - center) / MainWindow.GetScale() - MainWindow.GetTranslation() + center);
    }
    
    /// <summary>
    /// Converts coords from the game (like positions of objects) into screen coords (like mouse pos)
    /// </summary>
    /// <param name="worldCoords">The coords from the game to convert</param>
    public static Vec2Int WorldToScreenCoords(Vec2Long worldCoords)
    {
        var center = (Vec2Float)Program.Get().GetScreenSize() / 2f;
        return (Vec2Int)((worldCoords + MainWindow.GetTranslation() - center) * MainWindow.GetScale() + center);
    }

    public static Vec2Float ScreenToVertexCoords(Vec2Int screenCoords)
    {
        // get the screen size
        var screenSize = Program.Get().GetScreenSize();
        
        // cast screenCoords to a float
        var screenCoordsF = (Vec2Float)screenCoords;
        
        // divide screenCoordsF by screenSize, to get it to a 0-1 range
        screenCoordsF /= (Vec2Float)screenSize;
        
        // multiply screenCoordsF by 2, to get it to a 0-2 range
        screenCoordsF *= 2;
        
        // subtract 1 from screenCoordsF, to get it to a (-1)-1 range
        screenCoordsF -= new Vec2Float(1);
        
        // negate the Y axis to flip the coords correctly
        screenCoordsF = new Vec2Float(screenCoordsF.X, -screenCoordsF.Y);
        
        // return screenCoordsF
        return screenCoordsF;
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