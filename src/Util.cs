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
    public static Vec2<long> ScreenToWorldCoords(Vec2<int> screenCoords)
    {
        var screenSize = Program.Get().GetScreenSize();

        screenCoords = new Vec2<int>(screenCoords.X, screenSize.Y - screenCoords.Y);
        
        var center = (Vec2<decimal>)screenSize / 2;
        var value = ((Vec2<decimal>)screenCoords - center) / new Vec2<decimal>((decimal)MainWindow.GetScale()) - MainWindow.GetTranslation() + center;
        
        return new Vec2<long>(
            (long)Math.Clamp(value.X, long.MinValue, long.MaxValue), 
            (long)Math.Clamp(value.Y, long.MinValue, long.MaxValue));

    }
    
    /// <summary>
    /// Converts coords from the game (like positions of objects) into screen coords (like mouse pos)
    /// </summary>
    /// <param name="worldCoords">The coords from the game to convert</param>
    public static Vec2<int> WorldToScreenCoords(Vec2<long> worldCoords)
    {
        var screenSize = Program.Get().GetScreenSize();

        var center = (Vec2<float>)screenSize / 2f;
        var value = ((Vec2<decimal>)worldCoords + MainWindow.GetTranslation() - (Vec2<decimal>)center) * (decimal)MainWindow.GetScale() + (Vec2<decimal>)center;
        
        return new Vec2<int>(
            (int)Math.Clamp(value.X, int.MinValue, int.MaxValue), 
            screenSize.Y - (int)Math.Clamp(value.Y, int.MinValue, int.MaxValue));
    }

    public static Vec2<float> ScreenToVertexCoords(Vec2<int> screenCoords)
    {
        // get the screen size
        var screenSize = Program.Get().GetScreenSize();
        
        // cast screenCoords to a float
        var screenCoordsF = (Vec2<float>)screenCoords;
        
        // divide screenCoordsF by screenSize, to get it to a 0-1 range
        screenCoordsF /= (Vec2<float>)screenSize;
        
        // multiply screenCoordsF by 2, to get it to a 0-2 range
        screenCoordsF *= 2;
        
        // subtract 1 from screenCoordsF, to get it to a (-1)-1 range
        screenCoordsF -= new Vec2<float>(1);
        
        // negate the Y axis to flip the coords correctly
        screenCoordsF = new Vec2<float>(screenCoordsF.X, -screenCoordsF.Y);
        
        // return screenCoordsF
        return screenCoordsF;
    }
    
    public static void Error(object text)
    {
        if (!Constants.Error) return;
        
        Console.ForegroundColor = ErrorColor;
        Console.Out.WriteLine($"[Error]: {text}");
        Console.ForegroundColor = DefaultColor;
    }
    
    public static void Warn(object text)
    {
        if (!Constants.Warn) return;

        Console.ForegroundColor = WarnColor;
        Console.Out.WriteLine($"[Warn ]: {text}");
        Console.ForegroundColor = DefaultColor;
    }
    
    public static void Debug(object text)
    {
        if (!Constants.Debug) return;

        Console.ForegroundColor = DebugColor;
        Console.Out.WriteLine($"[Debug]: {text}");
        Console.ForegroundColor = DefaultColor;
    }
    
    public static void Log(object text)
    {
        if (!Constants.Log) return;

        Console.ForegroundColor = LogColor;
        Console.Out.WriteLine($"[ Log ]: {text}");
        Console.ForegroundColor = DefaultColor;
    }
    
    public static void Error(string format, object arg0)
    {
        if (!Constants.Error) return;

        Console.ForegroundColor = ErrorColor;
        Console.Out.Write("[Error]: ");
        Console.Out.Write(format, arg0);
        Console.ForegroundColor = DefaultColor;
    }
    
    public static void Warn(string format, object arg0)
    {
        if (!Constants.Warn) return;

        Console.ForegroundColor = WarnColor;
        Console.Out.Write("[Warn ]: ");
        Console.Out.Write(format, arg0);
        Console.ForegroundColor = DefaultColor;
    }
    
    public static void Debug(string format, object arg0)
    {
        if (!Constants.Debug) return;

        Console.ForegroundColor = DebugColor;
        Console.Out.Write("[Debug]: ");
        Console.Out.Write(format, arg0);
        Console.ForegroundColor = DefaultColor;
    }
    
    public static void Log(string format, object arg0)
    {
        if (!Constants.Log) return;

        Console.ForegroundColor = LogColor;
        Console.Out.Write("[ Log ]: ");
        Console.Out.Write(format, arg0);
        Console.ForegroundColor = DefaultColor;
    }
    
}