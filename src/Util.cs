#nullable enable
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using OpenTK.Graphics.OpenGL;
using Sandbox2D.Maths;

namespace Sandbox2D;

public static class Util
{
    // whether to print outputs
    private const bool EnablePrint = true;
    private const bool EnableLog = true;
    private const bool EnableDebug = true;
    private const bool EnableWarn = true;
    private const bool EnableError = true;
    
    private const ConsoleColor ErrorColor = ConsoleColor.Red;
    private const ConsoleColor WarnColor = ConsoleColor.Yellow;
    private const ConsoleColor DebugColor = ConsoleColor.Green;
    private const ConsoleColor LogColor = ConsoleColor.Cyan;
    private const ConsoleColor PrintColor = ConsoleColor.Gray;
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
        
        // return vertex coords
        return vertexCoords;
    }
    
    // the following methods are (partially modified) versions from: https://graphics.stanford.edu/%7Eseander/bithacks.html
    
    public static uint ReverseBits(uint v)
    {
        v = (((v & 0xaaaaaaaa) >> 1) | ((v & 0x55555555) << 1));
        v = (((v & 0xcccccccc) >> 2) | ((v & 0x33333333) << 2));
        v = (((v & 0xf0f0f0f0) >> 4) | ((v & 0x0f0f0f0f) << 4));
        v = (((v & 0xff00ff00) >> 8) | ((v & 0x00ff00ff) << 8));
        return((v >> 16) | (v << 16));
    }
    
    public static uint ReverseCode(uint v)
    {
        v = (((v & 0xcccccccc) >> 2) | ((v & 0x33333333) << 2));
        v = (((v & 0xf0f0f0f0) >> 4) | ((v & 0x0f0f0f0f) << 4));
        v = (((v & 0xff00ff00) >> 8) | ((v & 0x00ff00ff) << 8));

        return((v >> 16) | (v << 16));
    }

    public static uint TrailingZeros(uint v)
    {
        if ((v & 0x1) == 0x1)
            return 0;
        
        uint c = 1;
        if ((v & 0xffff) == 0) 
        {  
            v >>= 16;  
                c += 16;
        }
        if ((v & 0xff) == 0) 
        {  
            v >>= 8;  
            c += 8;
        }
        if ((v & 0xf) == 0) 
        {  
            v >>= 4;
            c += 4;
        }
        if ((v & 0x3) == 0) 
        {  
            v >>= 2; 
            c += 2;
        }
        c -= v & 0x1;
        
        return c;
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
    
    /// <summary>
    /// Prints any thrown OpenGL errors
    /// </summary>
    public static bool PrintGlErrors()
    {
        var hadErrors = false;
        while (true)
        {
            var error = GL.GetError();
            if (error != ErrorCode.NoError)
            {
                Error(error.ToString(), OutputSource.OpenGl);
                hadErrors = true;
                continue;
            }
            break;
        }
        
        return hadErrors;
    }
    
    public static void Out(object text, LogLevel level = LogLevel.Print, OutputSource source = OutputSource.None)
    {
        switch (level)
        {
            case LogLevel.Error:
                Error(text, source);
                break;
            case LogLevel.Warn:
                Warn(text, source);
                break;
            case LogLevel.Debug:
                Debug(text, source);
                break;
            case LogLevel.Log:
                Log(text, source);
                break;
            default:
                Print(text, source);
                break;
        }
    }
    
    /// <summary>
    /// Prints whether <paramref name="value"/> and <paramref name="expected"/> are equal.
    /// </summary>
    /// <param name="value">the value</param>
    /// <param name="expected">the expected value</param>
    /// <param name="name">[optional] the name of what was performed to get <paramref name="value"/></param>
    public static bool Assert(object value, object expected, string name = "")
    {
        if (Equals(value, expected))
        {
            Debug($"[ PASS ]: {name}", OutputSource.Test);
            return true;
        }
        Error($"[ FAIL ]: {name}", OutputSource.Test);
        Error($"  {value} != {expected}");
        return false;
    }
    
    public static void Error(object text, OutputSource source = OutputSource.None)
    {
        if (!EnableError) return;
        
        Console.ForegroundColor = ErrorColor;
        Console.Out.WriteLine($"{(source == OutputSource.None ? "[Error]:" : $"[{source}]")} {text}");
        Console.ForegroundColor = DefaultColor;
    }
    
    public static void Warn(object text, OutputSource source = OutputSource.None)
    {
        if (!EnableWarn) return;
        
        Console.ForegroundColor = WarnColor;
        Console.Out.WriteLine($"{(source == OutputSource.None ? "[Warn ]:" : $"[{source}]")} {text}");
        Console.ForegroundColor = DefaultColor;
    }
    
    public static void Debug(object text, OutputSource source = OutputSource.None)
    {
        if (!EnableDebug) return;
        
        Console.ForegroundColor = DebugColor;
        Console.Out.WriteLine($"{(source == OutputSource.None ? "[Debug]:" : $"[{source}]")} {text}");
        Console.ForegroundColor = DefaultColor;
    }
    
    public static void Log(object text, OutputSource source = OutputSource.None)
    {
        if (!EnableLog) return;
        
        Console.ForegroundColor = LogColor;
        Console.Out.WriteLine($"{(source == OutputSource.None ? "[ Log ]:" : $"[{source}]")} {text}");
        Console.ForegroundColor = DefaultColor;
    }
    
    public static void Print(object text, OutputSource source = OutputSource.None)
    {
        if (!EnablePrint) return;
        
        Console.ForegroundColor = PrintColor;
        Console.Out.WriteLine($"{(source == OutputSource.None ? "" : $"[{source}]")} {text}");
        Console.ForegroundColor = DefaultColor;
    }
    
}

public enum LogLevel
{
    Error,
    Warn,
    Debug,
    Log,
    Print
}

public enum OutputSource
{
    None,
    Test,
    Load,
    Render,
    OpenGl,
    Logic,
}

public readonly struct Hash
{
    private readonly ulong _d0;
    private readonly ulong _d1;
    private readonly ulong _d2;
    private readonly ulong _d3;

    public Hash(object? o)
    {
        if (Equals(o, null))
        {
            _d0 = 0;
            _d1 = 0;
            _d2 = 0;
            _d3 = 0;
            return;
        }
        
        var data = SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(o));
        
        _d0 = BitConverter.ToUInt64(data, 0);
        _d1 = BitConverter.ToUInt64(data, 8);
        _d2 = BitConverter.ToUInt64(data, 16);
        _d3 = BitConverter.ToUInt64(data, 24);
        
    }

    private Hash(ulong d0, ulong d1, ulong d2, ulong d3)
    {
        _d0 = d0;
        _d1 = d1;
        _d2 = d2;
        _d3 = d3;
    }
    
    public override string ToString()
    {
        return $"0x{_d0:X16}{_d1:X16}{_d2:X16}{_d3:X16}";
    }

    public override bool Equals(object? o)
    {
        if (o is Hash h)
        {
            return _d0 == h._d0 && _d1 == h._d1 && _d2 == h._d2 && _d3 == h._d3;
        }
        
        return Equals(this, o);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(_d0, _d1, _d2, _d3);
    }

    public static bool operator ==(Hash left, Hash right)
    {
        return left.Equals(right);
    }
    
    public static bool operator !=(Hash left, Hash right)
    {
        return !(left == right);
    }
    
    public static Hash operator ^(Hash left, Hash right)
    {
        return new Hash(left._d0 ^ right._d0, left._d1 ^ right._d1, left._d2 ^ right._d2, left._d3 ^ right._d3);
    }
}