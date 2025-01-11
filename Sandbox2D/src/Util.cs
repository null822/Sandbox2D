using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Math2D;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Sandbox2D;

public static class Util
{
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
    
    private const ConsoleColor FatalColor = ConsoleColor.Red;
    private const ConsoleColor ErrorColor = ConsoleColor.Red;
    private const ConsoleColor WarnColor = ConsoleColor.Yellow;
    private const ConsoleColor DebugColor = ConsoleColor.Green;
    private const ConsoleColor LogColor = ConsoleColor.Cyan;
    private const ConsoleColor PrintColor = ConsoleColor.Gray;
    private const ConsoleColor DefaultColor = ConsoleColor.White;
    
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

    public static void Fatal(object text)
    {
        Fatal(new Exception($"{text}"));
    }
    
    public static void Fatal(Exception exception)
    {
        Console.ForegroundColor = FatalColor;
        Console.BackgroundColor = ConsoleColor.Black;
        throw exception;
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

    public static string ArrayToString<T>(T[] arr)
    {
        var str = new StringBuilder("[");
        foreach (var v in arr)
        {
            str.Append($"{v}, ");
        }

        str.Remove(str.Length - 2, 2);
        str.Append(']');
        
        return str.ToString();
    }
    
    public static readonly DebugProc DebugMessageDelegate = OnDebugMessage;
    
    private static void OnDebugMessage(DebugSource source, DebugType type, int id, DebugSeverity severity, int length,
        IntPtr pMessage, IntPtr pUserParam)
    {
        var logLevel = severity switch
        {
            DebugSeverity.DontCare => LogLevel.Print,
            DebugSeverity.DebugSeverityNotification => LogLevel.Log,
            DebugSeverity.DebugSeverityLow => LogLevel.Debug,
            DebugSeverity.DebugSeverityMedium => LogLevel.Warn,
            DebugSeverity.DebugSeverityHigh => LogLevel.Error,
            _ => LogLevel.Unknown
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
    
    public static Vector2 ToVector2(this Vec2<float> v)
    {
        return new Vector2(v.X, v.Y);
    }
    
    public static Vec2<float> ToVec2(this Vector2 v)
    {
        return new Vec2<float>(v.X, v.Y);
    }
    
    // ReSharper disable once InconsistentNaming
    public static Vector2i ToVector2i(this Vec2<int> v)
    {
        return new Vector2i(v.X, v.Y);
    }
    
    public static Vec2<int> ToVec2(this Vector2i v)
    {
        return new Vec2<int>(v.X, v.Y);
    }
    
    public static Vector3 ToVector3(this Color color)
    {
        return new Vector3(color.R / 256f, color.G / 256f, color.B / 256f);
    }
}

public enum LogLevel
{
    Fatal,
    Error,
    Warn,
    Debug,
    Log,
    Print,
    
    Unknown
}
