using System;
using System.Globalization;
using System.Numerics;
using OpenTK.Mathematics;
using Vector2 = OpenTK.Mathematics.Vector2;

namespace Sandbox2D.Maths;

public readonly struct Vec2<T>(T x, T y)
    where T : INumber<T>, IConvertible, new()

{
    
    public readonly T X = x;
    public readonly T Y = y;

    public Vec2() : this(new T(), new T())
    {
    }
    
    public Vec2(T a) : this(a, a)
    {
    }
    
    public Vec2((T X, T Y) vec) : this(vec.X, vec.Y)
    {
        
    }
    
    public static Vec2<T> operator +(Vec2<T> a, Vec2<T> b)
    {
        return new Vec2<T>(a.X + b.X, a.Y + b.Y);
    }
    public static Vec2<T> operator -(Vec2<T> a, Vec2<T> b)
    {
        return new Vec2<T>(a.X - b.X, a.Y - b.Y);
    }
    public static Vec2<T> operator -(Vec2<T> a)
    {
        return new Vec2<T>(-a.X, -a.Y);
    }
    public static Vec2<T> operator /(Vec2<T> a, Vec2<T> b)
    {
        return new Vec2<T>(a.X / b.X, a.Y / b.Y);
    }
    public static Vec2<T> operator /(Vec2<T> a, T b)
    {
        return new Vec2<T>(a.X / b, a.Y / b);
    }
    public static Vec2<T> operator *(Vec2<T> a, Vec2<T> b)
    {
        return new Vec2<T>(a.X * b.X, a.Y * b.Y);
    }
    public static Vec2<T> operator *(Vec2<T> a, T b)
    {
        return new Vec2<T>(a.X * b, a.Y * b);
    }
    public static Vec2<T> operator %(Vec2<T> a, Vec2<T> b)
    {
        return new Vec2<T>(a.X % b.X, a.Y % b.Y);
    }
    public static bool operator ==(Vec2<T> a, Vec2<T> b)
    {
        return a.X == b.X && a.Y == b.Y;
    }
    public static bool operator !=(Vec2<T> a, Vec2<T> b)
    {
        return a.X != b.X || a.Y != b.Y;
    }
    public static bool operator >=(Vec2<T> a, Vec2<T> b)
    {
        return a.X >= b.X && a.Y >= b.Y;
    }
    public static bool operator <=(Vec2<T> a, Vec2<T> b)
    {
        return a.X <= b.X && a.Y <= b.Y;
    }
    public static bool operator >(Vec2<T> a, Vec2<T> b)
    {
        return a.X > b.X && a.Y > b.Y;
    }
    public static bool operator <(Vec2<T> a, Vec2<T> b)
    {
        return a.X < b.X && a.Y < b.Y;
    }
    
    private bool Equals(Vec2<T> other)
    {
        return X == other.X && Y == other.Y;
    }
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Vec2<T>)obj);
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
    
    // casts

    public static implicit operator Vec2<T>((T X, T Y) a)
    {
        return new Vec2<T>(a.X, a.Y);
    }
    
    public static explicit operator (T X, T Y)(Vec2<T> a)
    {
        return (a.X, a.Y);
    }
    
    public static implicit operator Vector2(Vec2<T> a)
    {
        return new Vector2(Convert.ToSingle(a.X), Convert.ToSingle(a.Y));
    }
    public static implicit operator Vector2i(Vec2<T> a)
    {
        return new Vector2i(Convert.ToInt32(a.X), Convert.ToInt32(a.Y));
    }
    public static implicit operator Vector2d(Vec2<T> a)
    {
        return new Vector2d(Convert.ToDouble(a.X), Convert.ToDouble(a.Y));
    }
    
    public static explicit operator Vec2<T>(Vector2 a)
    {
        return new Vec2<T>((T)NumberConvert.To(new T(), a.X), (T)NumberConvert.To(new T(), a.Y));
    }
    public static explicit operator Vec2<T>(Vector2i a)
    {
        return new Vec2<T>((T)NumberConvert.To(new T(), a.X), (T)NumberConvert.To(new T(), a.Y));
    }
    public static explicit operator Vec2<T>(Vector2d a)
    {
        return new Vec2<T>((T)NumberConvert.To(new T(), a.X), (T)NumberConvert.To(new T(), a.Y));
    }

    public static implicit operator Vec2<byte>(Vec2<T> a)
    {
        return new Vec2<byte>(Convert.ToByte(a.X), Convert.ToByte(a.Y));
    }
    public static implicit operator Vec2<sbyte>(Vec2<T> a)
    {
        return new Vec2<sbyte>(Convert.ToSByte(a.X), Convert.ToSByte(a.Y));
    }
    public static implicit operator Vec2<char>(Vec2<T> a)
    {
        return new Vec2<char>(Convert.ToChar(a.X), Convert.ToChar(a.Y));
    }
    public static implicit operator Vec2<short>(Vec2<T> a)
    {
        return new Vec2<short>(Convert.ToInt16(a.X), Convert.ToInt16(a.Y));
    }
    public static implicit operator Vec2<ushort>(Vec2<T> a)
    {
        return new Vec2<ushort>(Convert.ToUInt16(a.X), Convert.ToUInt16(a.Y));
    }
    public static implicit operator Vec2<int>(Vec2<T> a)
    {
        return new Vec2<int>(Convert.ToInt32(a.X), Convert.ToInt32(a.Y));
    }
    public static implicit operator Vec2<uint>(Vec2<T> a)
    {
        return new Vec2<uint>(Convert.ToUInt32(a.X), Convert.ToUInt32(a.Y));
    }
    public static implicit operator Vec2<long>(Vec2<T> a)
    {
        return new Vec2<long>(Convert.ToInt64(a.X), Convert.ToInt64(a.Y));
    }
    public static implicit operator Vec2<ulong>(Vec2<T> a)
    {
        return new Vec2<ulong>(Convert.ToUInt64(a.X), Convert.ToUInt64(a.Y));
    }
    public static implicit operator Vec2<decimal>(Vec2<T> a)
    {
        return new Vec2<decimal>(Convert.ToDecimal(a.X), Convert.ToDecimal(a.Y));
    }
    public static implicit operator Vec2<float>(Vec2<T> a)
    {
        return new Vec2<float>(Convert.ToSingle(a.X), Convert.ToSingle(a.Y));
    }
    public static implicit operator Vec2<double>(Vec2<T> a)
    {
        return new Vec2<double>(Convert.ToDouble(a.X), Convert.ToDouble(a.Y));
    }
    
    public override string ToString()
    {
        return $"({X}, {Y})";
    }

    public void Deconstruct(out T x, out T y)
    {
        x = X;
        y = Y;
    }
}

internal static class NumberConvert
{
    internal static IConvertible To(IConvertible to, IConvertible from)
    {
        return to switch
        {
            byte => Convert.ToByte(from),
            char => Convert.ToChar(from),
            DateTime => Convert.ToDateTime(from),
            decimal => Convert.ToDecimal(from),
            double => Convert.ToDouble(from),
            short => Convert.ToInt16(from),
            int => Convert.ToInt32(from),
            long => Convert.ToInt64(from),
            sbyte => Convert.ToSByte(from),
            float => Convert.ToSingle(from),
            string => Convert.ToString(from, CultureInfo.CurrentCulture),
            ushort => Convert.ToUInt16(from),
            uint => Convert.ToUInt32(from),
            ulong => Convert.ToUInt64(from),
            _ => 0
        };
    }
}
