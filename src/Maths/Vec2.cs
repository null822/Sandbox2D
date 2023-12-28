using System;
using Microsoft.Xna.Framework;

namespace Sandbox2D.Maths;

public struct Vec2Long
{
    public long X;
    public long Y;
    
    public Vec2Long(long x, long y)
    {
        X = x;
        Y = y;
    }
    public Vec2Long(long a)
    {
        X = a;
        Y = a;
    }
    
    public static Vec2Long operator +(Vec2Long a, Vec2Long b)
    {
        return new Vec2Long(a.X + b.X, a.Y + b.Y);
    }
    public static Vec2Long operator -(Vec2Long a, Vec2Long b)
    {
        return new Vec2Long(a.X - b.X, a.Y - b.Y);
    }
    public static Vec2Long operator -(Vec2Long a)
    {
        return new Vec2Long(-a.X, -a.Y);
    }
    public static Vec2Long operator /(Vec2Long a, Vec2Long b)
    {
        return new Vec2Long(a.X / b.X, a.Y / b.Y);
    }
    public static Vec2Long operator /(Vec2Long a, double b)
    {
        return new Vec2Long((long)(a.X / b), (long)(a.Y / b));
    }
    public static Vec2Long operator *(Vec2Long a, Vec2Long b)
    {
        return new Vec2Long(a.X * b.X, a.Y * b.Y);
    }
    public static Vec2Long operator *(Vec2Long a, double b)
    {
        return new Vec2Long((long)(a.X * b), (long)(a.Y * b));
    }
    public static Vec2Long operator %(Vec2Long a, Vec2Long b)
    {
        return new Vec2Long(a.X % b.X, a.Y % b.Y);
    }
    public static Vec2Long operator |(Vec2Long a, Vec2Long b)
    {
        return new Vec2Long(a.X | b.X, a.Y | b.Y);
    }
    public static Vec2Long operator &(Vec2Long a, Vec2Long b)
    {
        return new Vec2Long(a.X & b.X, a.Y & b.Y);
    }
    public static Vec2Long operator ^(Vec2Long a, Vec2Long b)
    {
        return new Vec2Long(a.X ^ b.X, a.Y ^ b.Y);
    }
    public static Vec2Long operator ~(Vec2Long a)
    {
        return new Vec2Long(~a.X, ~a.Y);
    }
    public static Vec2Long operator <<(Vec2Long a, int b)
    {
        return new Vec2Long(a.X << b, a.Y << b);
    }
    public static Vec2Long operator >>(Vec2Long a, int b)
    {
        return new Vec2Long(a.X >> b, a.Y >> b);
    }
    public static bool operator ==(Vec2Long a, Vec2Long b)
    {
        return a.X == b.X && a.Y == b.Y;
    }
    public static bool operator !=(Vec2Long a, Vec2Long b)
    {
        return a.X != b.X || a.Y != b.Y;
    }
    public static bool operator >=(Vec2Long a, Vec2Long b)
    {
        return a.X >= b.X && a.Y >= b.Y;
    }
    public static bool operator <=(Vec2Long a, Vec2Long b)
    {
        return a.X <= b.X && a.Y <= b.Y;
    }
    public static bool operator >(Vec2Long a, Vec2Long b)
    {
        return a.X > b.X && a.Y > b.Y;
    }
    public static bool operator <(Vec2Long a, Vec2Long b)
    {
        return a.X < b.X && a.Y < b.Y;
    }
    
    private bool Equals(Vec2Long other)
    {
        return X == other.X && Y == other.Y;
    }
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Vec2Long)obj);
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public static implicit operator Vector2(Vec2Long a)
    {
        return new Vector2(a.X, a.Y);
    }
    public static implicit operator Vec2Long(Vector2 a)
    {
        return new Vec2Long((long)a.X, (long)a.Y);
    }

    public static explicit operator Vec2Int(Vec2Long a)
    {
        return new Vec2Int((int)a.X, (int)a.Y);
    }
    public static explicit operator Vec2Float(Vec2Long a)
    {
        return new Vec2Float(a.X, a.Y);
    }
    public static implicit operator Vec2Double(Vec2Long a)
    {
        return new Vec2Double(a.X, a.Y);
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}

public struct Vec2Int
{
    public int X;
    public int Y;
    
    public Vec2Int(int x, int y)
    {
        X = x;
        Y = y;
    }
    public Vec2Int(int a)
    {
        X = a;
        Y = a;
    }
    
    public static Vec2Int operator +(Vec2Int a, Vec2Int b)
    {
        return new Vec2Int(a.X + b.X, a.Y + b.Y);
    }
    public static Vec2Int operator -(Vec2Int a, Vec2Int b)
    {
        return new Vec2Int(a.X - b.X, a.Y - b.Y);
    }
    public static Vec2Int operator -(Vec2Int a)
    {
        return new Vec2Int(-a.X, -a.Y);
    }
    public static Vec2Int operator /(Vec2Int a, Vec2Int b)
    {
        return new Vec2Int(a.X / b.X, a.Y / b.Y);
    }
    public static Vec2Int operator /(Vec2Int a, double b)
    {
        return new Vec2Int((int)(a.X / b), (int)(a.Y / b));
    }
    public static Vec2Int operator *(Vec2Int a, Vec2Int b)
    {
        return new Vec2Int(a.X * b.X, a.Y * b.Y);
    }
    public static Vec2Int operator *(Vec2Int a, double b)
    {
        return new Vec2Int((int)(a.X * b), (int)(a.Y * b));
    }
    public static Vec2Int operator %(Vec2Int a, Vec2Int b)
    {
        return new Vec2Int(a.X % b.X, a.Y % b.Y);
    }
    public static Vec2Int operator |(Vec2Int a, Vec2Int b)
    {
        return new Vec2Int(a.X | b.X, a.Y | b.Y);
    }
    public static Vec2Int operator &(Vec2Int a, Vec2Int b)
    {
        return new Vec2Int(a.X & b.X, a.Y & b.Y);
    }
    public static Vec2Int operator ^(Vec2Int a, Vec2Int b)
    {
        return new Vec2Int(a.X ^ b.X, a.Y ^ b.Y);
    }
    public static Vec2Int operator ~(Vec2Int a)
    {
        return new Vec2Int(~a.X, ~a.Y);
    }
    public static Vec2Int operator <<(Vec2Int a, int b)
    {
        return new Vec2Int(a.X << b, a.Y << b);
    }
    public static Vec2Int operator >>(Vec2Int a, int b)
    {
        return new Vec2Int(a.X >> b, a.Y >> b);
    }
    public static bool operator ==(Vec2Int a, Vec2Int b)
    {
        return a.X == b.X && a.Y == b.Y;
    }
    public static bool operator !=(Vec2Int a, Vec2Int b)
    {
        return a.X != b.X || a.Y != b.Y;
    }
    public static bool operator >=(Vec2Int a, Vec2Int b)
    {
        return a.X >= b.X && a.Y >= b.Y;
    }
    public static bool operator <=(Vec2Int a, Vec2Int b)
    {
        return a.X <= b.X && a.Y <= b.Y;
    }
    public static bool operator >(Vec2Int a, Vec2Int b)
    {
        return a.X > b.X && a.Y > b.Y;
    }
    public static bool operator <(Vec2Int a, Vec2Int b)
    {
        return a.X < b.X && a.Y < b.Y;
    }
    
    private bool Equals(Vec2Int other)
    {
        return X == other.X && Y == other.Y;
    }
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Vec2Int)obj);
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
    
    public static implicit operator Vector2(Vec2Int a)
    {
        return new Vector2(a.X, a.Y);
    }
    public static implicit operator Vec2Int(Vector2 a)
    {
        return new Vec2Int((int)a.X, (int)a.Y);
    }
    
    public static explicit operator Vec2Long(Vec2Int a)
    {
        return new Vec2Long(a.X, a.Y);
    }
    public static explicit operator Vec2Float(Vec2Int a)
    {
        return new Vec2Float(a.X, a.Y);
    }
    public static implicit operator Vec2Double(Vec2Int a)
    {
        return new Vec2Double(a.X, a.Y);
    }
    
    public override string ToString()
    {
        return $"({X}, {Y})";
    }

}

public struct Vec2Float
{
    public float X;
    public float Y;
    
    public Vec2Float(float x, float y)
    {
        X = x;
        Y = y;
    }
    public Vec2Float(float a)
    {
        X = a;
        Y = a;
    }
    
    public static Vec2Float operator +(Vec2Float a, Vec2Float b)
    {
        return new Vec2Float(a.X + b.X, a.Y + b.Y);
    }
    public static Vec2Float operator -(Vec2Float a, Vec2Float b)
    {
        return new Vec2Float(a.X - b.X, a.Y - b.Y);
    }
    public static Vec2Float operator -(Vec2Float a)
    {
        return new Vec2Float(-a.X, -a.Y);
    }
    public static Vec2Float operator /(Vec2Float a, Vec2Float b)
    {
        return new Vec2Float(a.X / b.X, a.Y / b.Y);
    }
    public static Vec2Float operator /(Vec2Float a, double b)
    {
        return new Vec2Float((float)(a.X / b), (float)(a.Y / b));
    }
    public static Vec2Float operator *(Vec2Float a, Vec2Float b)
    {
        return new Vec2Float(a.X * b.X, a.Y * b.Y);
    }
    public static Vec2Float operator *(Vec2Float a, double b)
    {
        return new Vec2Float((float)(a.X * b), (float)(a.Y * b));
    }
    public static Vec2Float operator %(Vec2Float a, Vec2Float b)
    {
        return new Vec2Float(a.X % b.X, a.Y % b.Y);
    }
    public static bool operator >=(Vec2Float a, Vec2Float b)
    {
        return a.X >= b.X && a.Y >= b.Y;
    }
    public static bool operator <=(Vec2Float a, Vec2Float b)
    {
        return a.X <= b.X && a.Y <= b.Y;
    }
    public static bool operator >(Vec2Float a, Vec2Float b)
    {
        return a.X > b.X && a.Y > b.Y;
    }
    public static bool operator <(Vec2Float a, Vec2Float b)
    {
        return a.X < b.X && a.Y < b.Y;
    }
    
    public Vec2Float Round()
    {
        X = (float)Math.Round(X);
        Y = (float)Math.Round(Y);
        return this;
    }
    public Vec2Float Floor()
    {
        X = (float)Math.Floor(X);
        Y = (float)Math.Floor(Y);
        return this;
    }
    public Vec2Float Ceiling()
    {
        X = (float)Math.Ceiling(X);
        Y = (float)Math.Ceiling(Y);
        return this;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
    
    public static implicit operator Vector2(Vec2Float a)
    {
        return new Vector2(a.X, a.Y);
    }
    public static implicit operator Vec2Float(Vector2 a)
    {
        return new Vec2Float(a.X, a.Y);
    }
    
    public static explicit operator Vec2Int(Vec2Float a)
    {
        return new Vec2Int((int)a.X, (int)a.Y);
    }
    public static explicit operator Vec2Long(Vec2Float a)
    {
        return new Vec2Long((long)a.X, (long)a.Y);
    }
    public static implicit operator Vec2Double(Vec2Float a)
    {
        return new Vec2Double(a.X, a.Y);
    }
    
    public override string ToString()
    {
        return $"({X}, {Y})";
    }
    
}


public struct Vec2Double
{
    public double X;
    public double Y;
    
    public Vec2Double(double x, double y)
    {
        X = x;
        Y = y;
    }
    public Vec2Double(double a)
    {
        X = a;
        Y = a;
    }
    
    public static Vec2Double operator +(Vec2Double a, Vec2Double b)
    {
        return new Vec2Double(a.X + b.X, a.Y + b.Y);
    }
    public static Vec2Double operator -(Vec2Double a, Vec2Double b)
    {
        return new Vec2Double(a.X - b.X, a.Y - b.Y);
    }
    public static Vec2Double operator -(Vec2Double a)
    {
        return new Vec2Double(-a.X, -a.Y);
    }
    public static Vec2Double operator /(Vec2Double a, Vec2Double b)
    {
        return new Vec2Double(a.X / b.X, a.Y / b.Y);
    }
    public static Vec2Double operator /(Vec2Double a, double b)
    {
        return new Vec2Double(a.X / b, a.Y / b);
    }
    public static Vec2Double operator *(Vec2Double a, Vec2Double b)
    {
        return new Vec2Double(a.X * b.X, a.Y * b.Y);
    }
    public static Vec2Double operator *(Vec2Double a, double b)
    {
        return new Vec2Double(a.X * b, a.Y * b);
    }
    public static Vec2Double operator %(Vec2Double a, Vec2Double b)
    {
        return new Vec2Double(a.X % b.X, a.Y % b.Y);
    }
    public static bool operator >=(Vec2Double a, Vec2Double b)
    {
        return a.X >= b.X && a.Y >= b.Y;
    }
    public static bool operator <=(Vec2Double a, Vec2Double b)
    {
        return a.X <= b.X && a.Y <= b.Y;
    }
    public static bool operator >(Vec2Double a, Vec2Double b)
    {
        return a.X > b.X && a.Y > b.Y;
    }
    public static bool operator <(Vec2Double a, Vec2Double b)
    {
        return a.X < b.X && a.Y < b.Y;
    }
    
    public Vec2Double Round()
    {
        X = Math.Round(X);
        Y = Math.Round(Y);
        return this;
    }
    public Vec2Double Floor()
    {
        X = Math.Floor(X);
        Y = Math.Floor(Y);
        return this;
    }
    public Vec2Double Ceiling()
    {
        X = Math.Ceiling(X);
        Y = Math.Ceiling(Y);
        return this;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
    
    public static implicit operator Vector2(Vec2Double a)
    {
        return new Vector2((float)a.X, (float)a.Y);
    }
    public static implicit operator Vec2Double(Vector2 a)
    {
        return new Vec2Double(a.X, a.Y);
    }
    
    public static explicit operator Vec2Int(Vec2Double a)
    {
        return new Vec2Int((int)a.X, (int)a.Y);
    }
    public static explicit operator Vec2Long(Vec2Double a)
    {
        return new Vec2Long((long)a.X, (long)a.Y);
    }
    public static explicit operator Vec2Float(Vec2Double a)
    {
        return new Vec2Float((float)a.X, (float)a.Y);
    }
    
    public override string ToString()
    {
        return $"({X}, {Y})";
    }
    
}
