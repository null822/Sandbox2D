using System.Globalization;

namespace Math2D;

public readonly struct Color
{
    public readonly byte R;
    public readonly byte G;
    public readonly byte B;
    
    public uint Decimal => R | ((uint)G << 8) | ((uint)B << 16);
    public string Hex => $"#{R:x2}{G:x2}{B:x2}";
    public byte Grayscale => (byte)((R + G + B) / 3);
    
    public Color(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }
    
    public Color(double r, double g, double b)
    {
        R = (byte)(r * 255);
        G = (byte)(g * 255);
        B = (byte)(b * 255);
    }
    
    public Color(float r, float g, float b)
    {
        R = (byte)(r * 255);
        G = (byte)(g * 255);
        B = (byte)(b * 255);
    }
    
    public Color(uint @decimal)
    {
        R = (byte)((@decimal >> 16) & 0xff);
        G = (byte)((@decimal >> 8) & 0xff);
        B = (byte)((@decimal >> 0) & 0xff);
    }
    
    public Color(string hex)
    {
        if (hex[0] != '#') throw new ArgumentException("Hex value was not valid (missing '#' prefix)");
        if (hex.Length != 7) throw new ArgumentException("Hex value was not valid");
        
        R = byte.Parse(hex[1..3], NumberStyles.HexNumber);
        G = byte.Parse(hex[3..5], NumberStyles.HexNumber);
        B = byte.Parse(hex[5..7], NumberStyles.HexNumber);
    }
    
    public override string ToString()
    {
        return Hex;
    }
    
    public static readonly Color   White = new Color(255, 255, 255);
    public static readonly Color   Black = new Color(  0,   0,   0);
    
    public static readonly Color     Red = new Color(255,   0,   0);
    public static readonly Color  Orange = new Color(255, 165,   0);
    public static readonly Color  Yellow = new Color(255, 255,   0);
    public static readonly Color    Lime = new Color(  0, 255,   0);
    public static readonly Color   Green = new Color(  0, 127,   0);
    public static readonly Color    Cyan = new Color(  0, 255, 255);
    public static readonly Color    Blue = new Color(  0,   0, 255);
    public static readonly Color  Purple = new Color(128,   0, 128);
    public static readonly Color Magenta = new Color(255,   0, 255);

    public static readonly Color   Brown = new Color(139,  69,  19);
    public static readonly Color    Gray = new Color(128, 128, 128);
    
}
