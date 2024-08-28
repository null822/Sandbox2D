﻿#nullable enable
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OpenTK.Graphics.OpenGL4;
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
    
    private const bool GlEnablePrint = false;
    private const bool GlEnableLog = false;
    private const bool GlEnableDebug = true;
    private const bool GlEnableWarn = true;
    private const bool GlEnableError = true;

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
    
    public static void Out(object text, LogLevel level = LogLevel.Print, string source = "")
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
            case LogLevel.Print:
                Print(text, source);
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
    /// <param name="source">[optional] the source of the assertion</param>
    public static bool Assert(object value, object expected, string name = "", string source = "Test")
    {
        if (Equals(value, expected))
        {
            Debug($"[ PASS ]: {name}", source);
            return true;
        }
        Error($"[ FAIL ]: {name}", source);
        Error( $"         {value} != {expected}");
        return false;
    }
    
    public static void Error(object text, string source = "")
    {
        if (!EnableError) return;
        
        Console.ForegroundColor = ErrorColor;
        Console.Out.WriteLine($"{(source == "" ? "[Error]:" : $"[{source,-5}]")} {text}");
        Console.ForegroundColor = DefaultColor;
    }
    
    public static void Warn(object text, string source = "")
    {
        if (!EnableWarn) return;
        
        Console.ForegroundColor = WarnColor;
        Console.Out.WriteLine($"{(source == "" ? "[Warn ]:" : $"[{source,-5}]")} {text}");
        Console.ForegroundColor = DefaultColor;
    }
    
    public static void Debug(object text, string source = "")
    {
        if (!EnableDebug) return;
        
        Console.ForegroundColor = DebugColor;
        Console.Out.WriteLine($"{(source == "" ? "[Debug]:" : $"[{source,-5}]")} {text}");
        Console.ForegroundColor = DefaultColor;
    }
    
    public static void Log(object text, string source = "")
    {
        if (!EnableLog) return;
        
        Console.ForegroundColor = LogColor;
        Console.Out.WriteLine($"{(source == "" ? "[ Log ]:" : $"[{source,-5}]")} {text}");
        Console.ForegroundColor = DefaultColor;
    }
    
    public static void Print(object text, string source = "")
    {
        if (!EnablePrint) return;
        
        Console.ForegroundColor = PrintColor;
        Console.Out.WriteLine($"{(source == "" ? "" : $"[{source,-5}]")} {text}");
        Console.ForegroundColor = DefaultColor;
    }
    
    public static readonly DebugProc DebugMessageDelegate = OnDebugMessage;
    
    private static void OnDebugMessage(DebugSource source, DebugType type, int id, DebugSeverity severity, int length,
        IntPtr pMessage, IntPtr pUserParam)
    {
        // The rest of the function is up to you to implement, however a debug output
        // is always useful.
        var logLevel = severity switch
        {
            DebugSeverity.DontCare => LogLevel.Print,
            DebugSeverity.DebugSeverityNotification => LogLevel.Log,
            DebugSeverity.DebugSeverityLow => LogLevel.Debug,
            DebugSeverity.DebugSeverityMedium => LogLevel.Warn,
            DebugSeverity.DebugSeverityHigh => LogLevel.Error,
            _ => (LogLevel)5
        };
        
        switch (logLevel)
        {
            case LogLevel.Print when !GlEnablePrint:
            case LogLevel.Log when !GlEnableLog:
            case LogLevel.Debug when !GlEnableDebug:
            case LogLevel.Warn when !GlEnableWarn:
            case LogLevel.Error when !GlEnableError:
                return;
        }
        
        var message = Marshal.PtrToStringUTF8(pMessage, length);
        

        var outString = new StringBuilder($"[{id}: {type}] {message}");
        
        if (Constants.SynchronousGlDebug)
        {
            var frames = new System.Diagnostics.StackTrace(true).GetFrames();
            for (var i = 1; i < frames.Length; i++)
            {
                var frame = frames[i];
                var method = frame.GetMethod();
                if (method == null) continue;
                
                outString.Append($"{Environment.NewLine}    at {GetMethodSignature(method)}");
                
                if (frame.GetFileName() != null)
                    outString.Append($" in {frames[i].GetFileName()}:{frames[i].GetFileLineNumber()}");
            }
        }
        
        Out(outString, logLevel, $"OpenGL/{source}");
    }
    
    private static string GetMethodSignature(MethodBase method)
    {
        var signature = new StringBuilder($"{method.Name}(");

        var parameters = method.GetParameters();
        foreach (var parameter in parameters)
        {
            signature.Append($"{parameter.ParameterType.Name}, ");
        }
        
        if (parameters.Length > 0)
        {
            signature.Remove(signature.Length - 2, 2);
        }
        signature.Append(')');
        
        return signature.ToString();
    }
    
}

public enum LogLevel
{
    Error,
    Warn,
    Debug,
    Log,
    Print,
}

public readonly struct Hash : IEquatable<Hash>
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
    
    public bool Equals(Hash other)
    {
        return _d0 == other._d0 && _d1 == other._d1 && _d2 == other._d2 && _d3 == other._d3;
    }
}