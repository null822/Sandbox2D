using System;
using Sandbox2D.Maths;

namespace Sandbox2D;

public static class Util
{
    private const ConsoleColor ErrorColor = ConsoleColor.Red;
    private const ConsoleColor WarnColor = ConsoleColor.Yellow;
    private const ConsoleColor DebugColor = ConsoleColor.Green;
    private const ConsoleColor LogColor = ConsoleColor.Cyan;

    private const ConsoleColor DefaultColor = ConsoleColor.White;


    /// <summary>
    /// Converts coords from the screen (like mouse pos) into game coords (like positions of objects)
    /// </summary>
    /// <param name="screenCoords">The coords from the screen to convert</param>
    public static Vec2<long> ScreenToWorldCoords(Vec2<int> screenCoords)
    {
        var screenSize = GameManager.ScreenSize;
        
        screenCoords = new Vec2<int>(screenCoords.X, screenSize.Y - screenCoords.Y);
        
        var center = (Vec2<decimal>)screenSize / 2;
        var value = ((Vec2<decimal>)screenCoords - center) / GameManager.Scale + GameManager.Translation + center;
        
        return new Vec2<long>(
            (long)Math.Clamp(Math.Floor(value.X), long.MinValue, long.MaxValue),
            (long)Math.Clamp(Math.Floor(value.Y), long.MinValue, long.MaxValue));
    }
    
    /// <summary>
    /// Converts coords from the game (like positions of objects) into screen coords (like mouse pos)
    /// </summary>
    /// <param name="worldCoords">The coords from the game to convert</param>
    public static Vec2<int> WorldToScreenCoords(Vec2<long> worldCoords)
    {
        var screenSize = GameManager.ScreenSize;

        var center = (Vec2<float>)screenSize / 2f;
        var value = ((Vec2<decimal>)worldCoords + GameManager.Translation - (Vec2<decimal>)center) * GameManager.Scale + (Vec2<decimal>)center;
        
        return new Vec2<int>(
                           (int)Math.Clamp(Math.Floor(value.X), int.MinValue, int.MaxValue), 
            screenSize.Y - (int)Math.Clamp(Math.Floor(value.Y), int.MinValue, int.MaxValue));
    }

    public static Vec2<float> ScreenToVertexCoords(Vec2<int> screenCoords)
    {
        // get the screen size
        var screenSize = GameManager.ScreenSize;
        
        // cast screenCoords to a float
        var vertexCoords = (Vec2<float>)screenCoords;
        
        // divide vertexCoords by screenSize, to get it to a 0-1 range
        vertexCoords /= (Vec2<float>)screenSize;
        
        // multiply vertexCoords by 2, to get it to a 0-2 range
        vertexCoords *= 2;
        
        // subtract 1 from vertexCoords, to get it to a (-1)-1 range
        vertexCoords -= new Vec2<float>(1);
        
        // negate the Y axis to flip the coords correctly
        vertexCoords = new Vec2<float>(vertexCoords.X, -vertexCoords.Y);
        
        // return screenCoordsF
        return vertexCoords;
    }
    
    
    public static uint Interleave(Vec2<uint> pos, byte depth)
    {
        uint code = 0;
        
        for(var i = 0; i < depth; i++)
        {
            code |= (pos.X >> (Constants.RenderDepth-1-i) & 0x1u) << i*2;
            code |= (pos.Y >> (Constants.RenderDepth-1-i) & 0x1u) << i*2+1;
        }
        
        return code;
    }

    public static uint ReverseBits(uint x)
    {
        x = (((x & 0xaaaaaaaa) >> 1) | ((x & 0x55555555) << 1));
        x = (((x & 0xcccccccc) >> 2) | ((x & 0x33333333) << 2));
        x = (((x & 0xf0f0f0f0) >> 4) | ((x & 0x0f0f0f0f) << 4));
        x = (((x & 0xff00ff00) >> 8) | ((x & 0x00ff00ff) << 8));
        return((x >> 16) | (x << 16));
    }
    
    public static uint ReverseCode(uint c)
    {
        c = (((c & 0xcccccccc) >> 2) | ((c & 0x33333333) << 2));
        c = (((c & 0xf0f0f0f0) >> 4) | ((c & 0x0f0f0f0f) << 4));
        c = (((c & 0xff00ff00) >> 8) | ((c & 0x00ff00ff) << 8));

        return((c >> 16) | (c << 16));
    }
    
    /// <summary>
    /// Returns the lowest power of 2 above a value.
    /// </summary>
    /// <param name="v">the value</param>
    public static ulong NextPowerOf2(ulong v)
    {
        const ulong half64 = ulong.MaxValue / 4 + 1;
        const ulong full64 = ulong.MaxValue / 2 + 1;
        
        if (v > half64)
            return full64;
        
        v--;
        v |= v >> 1;
        v |= v >> 2;
        v |= v >> 4;
        v |= v >> 8;
        v |= v >> 16;
        v |= v >> 32;
        v++;
        
        return v;
    }
    
    /// <summary>
    /// Returns the highest power of 2 below a value.
    /// </summary>
    /// <param name="v">the value</param>
    public static ulong PrevPowerOf2(ulong v)
    {
        v--;
        v |= v >> 1;
        v |= v >> 2;
        v |= v >> 4;
        v |= v >> 8;
        v |= v >> 16;
        v |= v >> 32;
        v++;
        
        return v / 2;
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