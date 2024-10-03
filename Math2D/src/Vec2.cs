using System.Numerics;
using System.Runtime.InteropServices;

namespace Math2D;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Vec2<T>(T x, T y) : IEquatable<Vec2<T>> where T : INumber<T>, IConvertible, new()
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
    
    /// <summary>
    /// Using this <see cref="Vec2{T}"/> as the origin, computes which quadrant another <see cref="Vec2{T}"/> lies within.
    /// </summary>
    /// <returns>
    /// <code>
    /// Ret Val | Quadrant
    /// --------+----------------
    ///  0      | Same Position
    ///  1      | Bottom Left
    ///  2      | Bottom Right
    ///  3      | Top Left
    ///  4      | Top Right
    /// -1      | Directly Below
    /// -2      | Directly Right
    /// -3      | Directly Left
    /// -4      | Directly Above
    /// </code>
    /// </returns>
    public int Quadrant(Vec2<T> other)
    {
        return (other.X - X, other.Y - Y) switch
        {
            ( 0,  0) =>  0,
            (<0, <0) =>  1,
            (>0, <0) =>  2,
            (<0, >0) =>  3,
            (>0, >0) =>  4,
            ( 0, <0) => -1,
            (>0,  0) => -2,
            (<0,  0) => -3,
            ( 0, >0) => -4,
            _ => -5
        };
    }
    
    /// <summary>
    /// Negates the X component of this <see cref="Vec2{T}"/>.
    /// </summary>
    public Vec2<T> FlipX()
    {
        return new Vec2<T>(-X, Y);
    }
    /// <summary>
    /// Negates the Y component of this <see cref="Vec2{T}"/>.
    /// </summary>
    public Vec2<T> FlipY()
    {
        return new Vec2<T>(X, -Y);
    }
    
    public T[] ToArray()
    {
        return [X, Y];
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
    
    public bool Equals(Vec2<T> other)
    {
        return X == other.X && Y == other.Y;
    }
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (obj.GetType() != GetType()) return false;
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
    public static explicit operator Vec2<int>(Vec2<T> a)
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
    public static explicit operator Vec2<decimal>(Vec2<T> a)
    {
        return new Vec2<decimal>(Convert.ToDecimal(a.X), Convert.ToDecimal(a.Y));
    }
    public static explicit operator Vec2<float>(Vec2<T> a)
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
