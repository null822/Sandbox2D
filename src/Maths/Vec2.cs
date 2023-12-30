using System;
using OpenTK.Mathematics;

namespace Sandbox2D.Maths;

public readonly struct Vec2Byte(byte x, byte y)
{
    public readonly byte X = x;
    public readonly byte Y = y;

    public Vec2Byte(byte a) : this(a, a)
    {
    }
    
    public Vec2Byte(int x, int y) : this((byte)x, (byte)y)
    {
    }
    public Vec2Byte(int a) : this((byte)a, (byte)a)
    {
    }
    
    public static Vec2Byte operator +(Vec2Byte a, Vec2Byte b)
    {
        return new Vec2Byte(a.X + b.X, a.Y + b.Y);
    }
    public static Vec2Byte operator -(Vec2Byte a, Vec2Byte b)
    {
        return new Vec2Byte(a.X - b.X, a.Y - b.Y);
    }
    public static Vec2Byte operator -(Vec2Byte a)
    {
        return new Vec2Byte(-a.X, -a.Y);
    }
    public static Vec2Byte operator /(Vec2Byte a, Vec2Byte b)
    {
        return new Vec2Byte(a.X / b.X, a.Y / b.Y);
    }
    public static Vec2Byte operator /(Vec2Byte a, byte b)
    {
        return new Vec2Byte((byte)(a.X / b), (byte)(a.Y / b));
    }
    public static Vec2Byte operator *(Vec2Byte a, Vec2Byte b)
    {
        return new Vec2Byte(a.X * b.X, a.Y * b.Y);
    }
    public static Vec2Byte operator *(Vec2Byte a, byte b)
    {
        return new Vec2Byte((byte)(a.X * b), (byte)(a.Y * b));
    }
    public static Vec2Byte operator %(Vec2Byte a, Vec2Byte b)
    {
        return new Vec2Byte(a.X % b.X, a.Y % b.Y);
    }
    public static Vec2Byte operator |(Vec2Byte a, Vec2Byte b)
    {
        return new Vec2Byte(a.X | b.X, a.Y | b.Y);
    }
    public static Vec2Byte operator &(Vec2Byte a, Vec2Byte b)
    {
        return new Vec2Byte(a.X & b.X, a.Y & b.Y);
    }
    public static Vec2Byte operator ^(Vec2Byte a, Vec2Byte b)
    {
        return new Vec2Byte(a.X ^ b.X, a.Y ^ b.Y);
    }
    public static Vec2Byte operator ~(Vec2Byte a)
    {
        return new Vec2Byte(~a.X, ~a.Y);
    }
    public static Vec2Byte operator <<(Vec2Byte a, int b)
    {
        return new Vec2Byte(a.X << b, a.Y << b);
    }
    public static Vec2Byte operator >>(Vec2Byte a, int b)
    {
        return new Vec2Byte(a.X >> b, a.Y >> b);
    }
    public static bool operator ==(Vec2Byte a, Vec2Byte b)
    {
        return a.X == b.X && a.Y == b.Y;
    }
    public static bool operator !=(Vec2Byte a, Vec2Byte b)
    {
        return a.X != b.X || a.Y != b.Y;
    }
    public static bool operator >=(Vec2Byte a, Vec2Byte b)
    {
        return a.X >= b.X && a.Y >= b.Y;
    }
    public static bool operator <=(Vec2Byte a, Vec2Byte b)
    {
        return a.X <= b.X && a.Y <= b.Y;
    }
    public static bool operator >(Vec2Byte a, Vec2Byte b)
    {
        return a.X > b.X && a.Y > b.Y;
    }
    public static bool operator <(Vec2Byte a, Vec2Byte b)
    {
        return a.X < b.X && a.Y < b.Y;
    }
    
    private bool Equals(Vec2Byte other)
    {
        return X == other.X && Y == other.Y;
    }
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Vec2Byte)obj);
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
    
    // casts
    
    // to other
    public static implicit operator Vector2(Vec2Byte a)
    {
        return new Vector2(a.X, a.Y);
    }
    public static implicit operator Vector2i(Vec2Byte a)
    {
        return new Vector2i(a.X, a.Y);
    }
    public static implicit operator Vector2h(Vec2Byte a)
    {
        return new Vector2h(a.X, a.Y);
    }
    public static implicit operator Vector2d(Vec2Byte a)
    {
        return new Vector2d(a.X, a.Y);
    }

    
    public static explicit operator Vec2Short(Vec2Byte a)
    {
        return new Vec2Short(a.X, a.Y);
    }
    public static explicit operator Vec2Ushort(Vec2Byte a)
    {
        return new Vec2Ushort(a.X, a.Y);
    }
    public static explicit operator Vec2Int(Vec2Byte a)
    {
        return new Vec2Int(a.X, a.Y);
    }
    public static explicit operator Vec2Uint(Vec2Byte a)
    {
        return new Vec2Uint(a.X, a.Y);
    }
    public static explicit operator Vec2Long(Vec2Byte a)
    {
        return new Vec2Long(a.X, a.Y);
    }
    public static explicit operator Vec2Ulong(Vec2Byte a)
    {
        return new Vec2Ulong(a.X, a.Y);
    }
    public static explicit operator Vec2Float(Vec2Byte a)
    {
        return new Vec2Float(a.X, a.Y);
    }
    public static implicit operator Vec2Double(Vec2Byte a)
    {
        return new Vec2Double(a.X, a.Y);
    }
    
    // from other
    public static explicit operator Vec2Byte(Vector2 a)
    {
        return new Vec2Byte((byte)a.X, (byte)a.Y);
    }
    public static explicit operator Vec2Byte(Vector2i a)
    {
        return new Vec2Byte((byte)a.X, (byte)a.Y);
    }
    public static explicit operator Vec2Byte(Vector2h a)
    {
        return new Vec2Byte((byte)a.X, (byte)a.Y);
    }
    public static explicit operator Vec2Byte(Vector2d a)
    {
        return new Vec2Byte((byte)a.X, (byte)a.Y);
    }
    
    
    public static explicit operator Vec2Byte(Vec2Short a)
    {
        return new Vec2Byte((byte)a.X, (byte)a.Y);
    }
    public static explicit operator Vec2Byte(Vec2Ushort a)
    {
        return new Vec2Byte((byte)a.X, (byte)a.Y);
    }
    public static explicit operator Vec2Byte(Vec2Int a)
    {
        return new Vec2Byte((byte)a.X, (byte)a.Y);
    }
    public static explicit operator Vec2Byte(Vec2Uint a)
    {
        return new Vec2Byte((byte)a.X, (byte)a.Y);
    }
    public static explicit operator Vec2Byte(Vec2Long a)
    {
        return new Vec2Byte((byte)a.X, (byte)a.Y);
    }
    public static explicit operator Vec2Byte(Vec2Ulong a)
    {
        return new Vec2Byte((byte)a.X, (byte)a.Y);
    }
    public static explicit operator Vec2Byte(Vec2Float a)
    {
        return new Vec2Byte((byte)a.X, (byte)a.Y);
    }
    public static explicit operator Vec2Byte(Vec2Double a)
    {
        return new Vec2Byte((byte)a.X, (byte)a.Y);
    }
    
    
    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}

public readonly struct Vec2Short(short x, short y)
{
    public readonly short X = x;
    public readonly short Y = y;

    public Vec2Short(short a) : this(a, a)
    {
    }
    
    public Vec2Short(int x, int y) : this((short)x, (short)y)
    {
    }
    public Vec2Short(int a) : this((short)a, (short)a)
    {
    }
    
    public static Vec2Short operator +(Vec2Short a, Vec2Short b)
    {
        return new Vec2Short(a.X + b.X, a.Y + b.Y);
    }
    public static Vec2Short operator -(Vec2Short a, Vec2Short b)
    {
        return new Vec2Short(a.X - b.X, a.Y - b.Y);
    }
    public static Vec2Short operator -(Vec2Short a)
    {
        return new Vec2Short(-a.X, -a.Y);
    }
    public static Vec2Short operator /(Vec2Short a, Vec2Short b)
    {
        return new Vec2Short(a.X / b.X, a.Y / b.Y);
    }
    public static Vec2Short operator /(Vec2Short a, short b)
    {
        return new Vec2Short((short)(a.X / b), (short)(a.Y / b));
    }
    public static Vec2Short operator *(Vec2Short a, Vec2Short b)
    {
        return new Vec2Short(a.X * b.X, a.Y * b.Y);
    }
    public static Vec2Short operator *(Vec2Short a, short b)
    {
        return new Vec2Short((short)(a.X * b), (short)(a.Y * b));
    }
    public static Vec2Short operator %(Vec2Short a, Vec2Short b)
    {
        return new Vec2Short(a.X % b.X, a.Y % b.Y);
    }
    public static Vec2Short operator |(Vec2Short a, Vec2Short b)
    {
        return new Vec2Short(a.X | b.X, a.Y | b.Y);
    }
    public static Vec2Short operator &(Vec2Short a, Vec2Short b)
    {
        return new Vec2Short(a.X & b.X, a.Y & b.Y);
    }
    public static Vec2Short operator ^(Vec2Short a, Vec2Short b)
    {
        return new Vec2Short(a.X ^ b.X, a.Y ^ b.Y);
    }
    public static Vec2Short operator ~(Vec2Short a)
    {
        return new Vec2Short(~a.X, ~a.Y);
    }
    public static Vec2Short operator <<(Vec2Short a, int b)
    {
        return new Vec2Short(a.X << b, a.Y << b);
    }
    public static Vec2Short operator >>(Vec2Short a, int b)
    {
        return new Vec2Short(a.X >> b, a.Y >> b);
    }
    public static bool operator ==(Vec2Short a, Vec2Short b)
    {
        return a.X == b.X && a.Y == b.Y;
    }
    public static bool operator !=(Vec2Short a, Vec2Short b)
    {
        return a.X != b.X || a.Y != b.Y;
    }
    public static bool operator >=(Vec2Short a, Vec2Short b)
    {
        return a.X >= b.X && a.Y >= b.Y;
    }
    public static bool operator <=(Vec2Short a, Vec2Short b)
    {
        return a.X <= b.X && a.Y <= b.Y;
    }
    public static bool operator >(Vec2Short a, Vec2Short b)
    {
        return a.X > b.X && a.Y > b.Y;
    }
    public static bool operator <(Vec2Short a, Vec2Short b)
    {
        return a.X < b.X && a.Y < b.Y;
    }
    
    private bool Equals(Vec2Short other)
    {
        return X == other.X && Y == other.Y;
    }
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Vec2Short)obj);
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
    
    // casts
    
    // to other
    public static implicit operator Vector2(Vec2Short a)
    {
        return new Vector2(a.X, a.Y);
    }
    public static implicit operator Vector2i(Vec2Short a)
    {
        return new Vector2i(a.X, a.Y);
    }
    public static implicit operator Vector2h(Vec2Short a)
    {
        return new Vector2h(a.X, a.Y);
    }
    public static implicit operator Vector2d(Vec2Short a)
    {
        return new Vector2d(a.X, a.Y);
    }

    
    public static explicit operator Vec2Ushort(Vec2Short a)
    {
        return new Vec2Ushort((ushort)a.X, (ushort)a.Y);
    }
    public static explicit operator Vec2Int(Vec2Short a)
    {
        return new Vec2Int(a.X, a.Y);
    }
    public static explicit operator Vec2Uint(Vec2Short a)
    {
        return new Vec2Uint((uint)a.X, (uint)a.Y);
    }
    public static explicit operator Vec2Long(Vec2Short a)
    {
        return new Vec2Long(a.X, a.Y);
    }
    public static explicit operator Vec2Ulong(Vec2Short a)
    {
        return new Vec2Ulong((ulong)a.X, (ulong)a.Y);
    }
    public static explicit operator Vec2Float(Vec2Short a)
    {
        return new Vec2Float(a.X, a.Y);
    }
    public static implicit operator Vec2Double(Vec2Short a)
    {
        return new Vec2Double(a.X, a.Y);
    }
    
    // from other
    public static explicit operator Vec2Short(Vector2 a)
    {
        return new Vec2Short((short)a.X, (short)a.Y);
    }
    public static explicit operator Vec2Short(Vector2i a)
    {
        return new Vec2Short((short)a.X, (short)a.Y);
    }
    public static explicit operator Vec2Short(Vector2h a)
    {
        return new Vec2Short((short)a.X, (short)a.Y);
    }
    public static explicit operator Vec2Short(Vector2d a)
    {
        return new Vec2Short((short)a.X, (short)a.Y);
    }
    
    public static explicit operator Vec2Short(Vec2Byte a)
    {
        return new Vec2Short(a.X, a.Y);
    }
    public static explicit operator Vec2Short(Vec2Ushort a)
    {
        return new Vec2Short((short)a.X, (short)a.Y);
    }
    public static explicit operator Vec2Short(Vec2Int a)
    {
        return new Vec2Short((short)a.X, (short)a.Y);
    }
    public static explicit operator Vec2Short(Vec2Uint a)
    {
        return new Vec2Short((short)a.X, (short)a.Y);
    }
    public static explicit operator Vec2Short(Vec2Long a)
    {
        return new Vec2Short((short)a.X, (short)a.Y);
    }
    public static explicit operator Vec2Short(Vec2Ulong a)
    {
        return new Vec2Short((short)a.X, (short)a.Y);
    }
    public static explicit operator Vec2Short(Vec2Float a)
    {
        return new Vec2Short((short)a.X, (short)a.Y);
    }
    public static explicit operator Vec2Short(Vec2Double a)
    {
        return new Vec2Short((short)a.X, (short)a.Y);
    }
    


    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}

public readonly struct Vec2Ushort(ushort x, ushort y)
{
    public readonly ushort X = x;
    public readonly ushort Y = y;

    public Vec2Ushort(ushort a) : this(a, a)
    {
    }
    
    public Vec2Ushort(int x, int y) : this((ushort)x, (ushort)y)
    {
    }
    public Vec2Ushort(int a) : this((ushort)a, (ushort)a)
    {
    }
    
    public static Vec2Ushort operator +(Vec2Ushort a, Vec2Ushort b)
    {
        return new Vec2Ushort(a.X + b.X, a.Y + b.Y);
    }
    public static Vec2Ushort operator -(Vec2Ushort a, Vec2Ushort b)
    {
        return new Vec2Ushort(a.X - b.X, a.Y - b.Y);
    }
    public static Vec2Ushort operator /(Vec2Ushort a, Vec2Ushort b)
    {
        return new Vec2Ushort(a.X / b.X, a.Y / b.Y);
    }
    public static Vec2Ushort operator /(Vec2Ushort a, ushort b)
    {
        return new Vec2Ushort((ushort)(a.X / b), (ushort)(a.Y / b));
    }
    public static Vec2Ushort operator *(Vec2Ushort a, Vec2Ushort b)
    {
        return new Vec2Ushort(a.X * b.X, a.Y * b.Y);
    }
    public static Vec2Ushort operator *(Vec2Ushort a, ushort b)
    {
        return new Vec2Ushort((ushort)(a.X * b), (ushort)(a.Y * b));
    }
    public static Vec2Ushort operator %(Vec2Ushort a, Vec2Ushort b)
    {
        return new Vec2Ushort(a.X % b.X, a.Y % b.Y);
    }
    public static Vec2Ushort operator |(Vec2Ushort a, Vec2Ushort b)
    {
        return new Vec2Ushort(a.X | b.X, a.Y | b.Y);
    }
    public static Vec2Ushort operator &(Vec2Ushort a, Vec2Ushort b)
    {
        return new Vec2Ushort(a.X & b.X, a.Y & b.Y);
    }
    public static Vec2Ushort operator ^(Vec2Ushort a, Vec2Ushort b)
    {
        return new Vec2Ushort(a.X ^ b.X, a.Y ^ b.Y);
    }
    public static Vec2Ushort operator ~(Vec2Ushort a)
    {
        return new Vec2Ushort(~a.X, ~a.Y);
    }
    public static Vec2Ushort operator <<(Vec2Ushort a, int b)
    {
        return new Vec2Ushort(a.X << b, a.Y << b);
    }
    public static Vec2Ushort operator >>(Vec2Ushort a, int b)
    {
        return new Vec2Ushort(a.X >> b, a.Y >> b);
    }
    public static bool operator ==(Vec2Ushort a, Vec2Ushort b)
    {
        return a.X == b.X && a.Y == b.Y;
    }
    public static bool operator !=(Vec2Ushort a, Vec2Ushort b)
    {
        return a.X != b.X || a.Y != b.Y;
    }
    public static bool operator >=(Vec2Ushort a, Vec2Ushort b)
    {
        return a.X >= b.X && a.Y >= b.Y;
    }
    public static bool operator <=(Vec2Ushort a, Vec2Ushort b)
    {
        return a.X <= b.X && a.Y <= b.Y;
    }
    public static bool operator >(Vec2Ushort a, Vec2Ushort b)
    {
        return a.X > b.X && a.Y > b.Y;
    }
    public static bool operator <(Vec2Ushort a, Vec2Ushort b)
    {
        return a.X < b.X && a.Y < b.Y;
    }
    
    private bool Equals(Vec2Ushort other)
    {
        return X == other.X && Y == other.Y;
    }
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Vec2Ushort)obj);
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
    
    // casts
    
    // to other
    public static implicit operator Vector2(Vec2Ushort a)
    {
        return new Vector2(a.X, a.Y);
    }
    public static implicit operator Vector2i(Vec2Ushort a)
    {
        return new Vector2i(a.X, a.Y);
    }
    public static implicit operator Vector2h(Vec2Ushort a)
    {
        return new Vector2h(a.X, a.Y);
    }
    public static implicit operator Vector2d(Vec2Ushort a)
    {
        return new Vector2d(a.X, a.Y);
    }

    
    public static explicit operator Vec2Short(Vec2Ushort a)
    {
        return new Vec2Short((short)a.X, (short)a.Y);
    }
    public static explicit operator Vec2Int(Vec2Ushort a)
    {
        return new Vec2Int(a.X, a.Y);
    }
    public static explicit operator Vec2Uint(Vec2Ushort a)
    {
        return new Vec2Uint(a.X, a.Y);
    }
    public static explicit operator Vec2Long(Vec2Ushort a)
    {
        return new Vec2Long(a.X, a.Y);
    }
    public static explicit operator Vec2Ulong(Vec2Ushort a)
    {
        return new Vec2Ulong(a.X, a.Y);
    }
    public static explicit operator Vec2Float(Vec2Ushort a)
    {
        return new Vec2Float(a.X, a.Y);
    }
    public static implicit operator Vec2Double(Vec2Ushort a)
    {
        return new Vec2Double(a.X, a.Y);
    }
    
    // from other
    public static explicit operator Vec2Ushort(Vector2 a)
    {
        return new Vec2Ushort((ushort)a.X, (ushort)a.Y);
    }
    public static explicit operator Vec2Ushort(Vector2i a)
    {
        return new Vec2Ushort((ushort)a.X, (ushort)a.Y);
    }
    public static explicit operator Vec2Ushort(Vector2h a)
    {
        return new Vec2Ushort((ushort)a.X, (ushort)a.Y);
    }
    public static explicit operator Vec2Ushort(Vector2d a)
    {
        return new Vec2Ushort((ushort)a.X, (ushort)a.Y);
    }
    
    public static explicit operator Vec2Ushort(Vec2Byte a)
    {
        return new Vec2Ushort(a.X, a.Y);
    }
    public static explicit operator Vec2Ushort(Vec2Short a)
    {
        return new Vec2Ushort((ushort)a.X, (ushort)a.Y);
    }
    public static explicit operator Vec2Ushort(Vec2Int a)
    {
        return new Vec2Ushort((ushort)a.X, (ushort)a.Y);
    }
    public static explicit operator Vec2Ushort(Vec2Uint a)
    {
        return new Vec2Ushort((ushort)a.X, (ushort)a.Y);
    }
    public static explicit operator Vec2Ushort(Vec2Long a)
    {
        return new Vec2Ushort((ushort)a.X, (ushort)a.Y);
    }
    public static explicit operator Vec2Ushort(Vec2Ulong a)
    {
        return new Vec2Ushort((ushort)a.X, (ushort)a.Y);
    }
    public static explicit operator Vec2Ushort(Vec2Float a)
    {
        return new Vec2Ushort((ushort)a.X, (ushort)a.Y);
    }
    public static explicit operator Vec2Ushort(Vec2Double a)
    {
        return new Vec2Ushort((ushort)a.X, (ushort)a.Y);
    }
    


    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}

public readonly struct Vec2Int(int x, int y)
{
    public readonly int X = x;
    public readonly int Y = y;

    public Vec2Int(int a) : this(a, a)
    {
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
    public static Vec2Int operator /(Vec2Int a, int b)
    {
        return new Vec2Int(a.X / b, a.Y / b);
    }
    public static Vec2Int operator *(Vec2Int a, Vec2Int b)
    {
        return new Vec2Int(a.X * b.X, a.Y * b.Y);
    }
    public static Vec2Int operator *(Vec2Int a, int b)
    {
        return new Vec2Int(a.X * b, a.Y * b);
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
    
    // casts
    
    // to other
    public static implicit operator Vector2(Vec2Int a)
    {
        return new Vector2(a.X, a.Y);
    }
    public static implicit operator Vector2i(Vec2Int a)
    {
        return new Vector2i(a.X, a.Y);
    }
    public static implicit operator Vector2h(Vec2Int a)
    {
        return new Vector2h(a.X, a.Y);
    }
    public static implicit operator Vector2d(Vec2Int a)
    {
        return new Vector2d(a.X, a.Y);
    }

    
    public static explicit operator Vec2Short(Vec2Int a)
    {
        return new Vec2Short((short)a.X, (short)a.Y);
    }
    public static explicit operator Vec2Ushort(Vec2Int a)
    {
        return new Vec2Ushort((ushort)a.X, (ushort)a.Y);
    }
    public static explicit operator Vec2Uint(Vec2Int a)
    {
        return new Vec2Uint((uint)a.X, (uint)a.Y);
    }
    public static explicit operator Vec2Long(Vec2Int a)
    {
        return new Vec2Long(a.X, a.Y);
    }
    public static explicit operator Vec2Ulong(Vec2Int a)
    {
        return new Vec2Ulong((ulong)a.X, (ulong)a.Y);
    }
    public static explicit operator Vec2Float(Vec2Int a)
    {
        return new Vec2Float(a.X, a.Y);
    }
    public static implicit operator Vec2Double(Vec2Int a)
    {
        return new Vec2Double(a.X, a.Y);
    }
    
    // from other
    public static explicit operator Vec2Int(Vector2 a)
    {
        return new Vec2Int((int)a.X, (int)a.Y);
    }
    public static explicit operator Vec2Int(Vector2i a)
    {
        return new Vec2Int(a.X, a.Y);
    }
    public static explicit operator Vec2Int(Vector2h a)
    {
        return new Vec2Int((int)a.X, (int)a.Y);
    }
    public static explicit operator Vec2Int(Vector2d a)
    {
        return new Vec2Int((int)a.X, (int)a.Y);
    }
    
    
    
    
    public static explicit operator Vec2Int(Vec2Uint a)
    {
        return new Vec2Int((int)a.X, (int)a.Y);
    }
    public static explicit operator Vec2Int(Vec2Long a)
    {
        return new Vec2Int((int)a.X, (int)a.Y);
    }
    public static explicit operator Vec2Int(Vec2Ulong a)
    {
        return new Vec2Int((int)a.X, (int)a.Y);
    }
    public static explicit operator Vec2Int(Vec2Float a)
    {
        return new Vec2Int((int)a.X, (int)a.Y);
    }
    public static explicit operator Vec2Int(Vec2Double a)
    {
        return new Vec2Int((int)a.X, (int)a.Y);
    }
    


    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}

public readonly struct Vec2Uint(uint x, uint y)
{
    public readonly uint X = x;
    public readonly uint Y = y;

    public Vec2Uint(uint a) : this(a, a)
    {
    }
    
    public Vec2Uint(long x, long y) : this((uint)x, (uint)y)
    {
    }
    public Vec2Uint(long a) : this((uint)a, (uint)a)
    {
    }
    
    public static Vec2Uint operator +(Vec2Uint a, Vec2Uint b)
    {
        return new Vec2Uint(a.X + b.X, a.Y + b.Y);
    }
    public static Vec2Uint operator -(Vec2Uint a, Vec2Uint b)
    {
        return new Vec2Uint(a.X - b.X, a.Y - b.Y);
    }
    public static Vec2Uint operator /(Vec2Uint a, Vec2Uint b)
    {
        return new Vec2Uint(a.X / b.X, a.Y / b.Y);
    }
    public static Vec2Uint operator /(Vec2Uint a, uint b)
    {
        return new Vec2Uint(a.X / b, a.Y / b);
    }
    public static Vec2Uint operator *(Vec2Uint a, Vec2Uint b)
    {
        return new Vec2Uint(a.X * b.X, a.Y * b.Y);
    }
    public static Vec2Uint operator *(Vec2Uint a, uint b)
    {
        return new Vec2Uint(a.X * b, a.Y * b);
    }
    public static Vec2Uint operator %(Vec2Uint a, Vec2Uint b)
    {
        return new Vec2Uint(a.X % b.X, a.Y % b.Y);
    }
    public static Vec2Uint operator |(Vec2Uint a, Vec2Uint b)
    {
        return new Vec2Uint(a.X | b.X, a.Y | b.Y);
    }
    public static Vec2Uint operator &(Vec2Uint a, Vec2Uint b)
    {
        return new Vec2Uint(a.X & b.X, a.Y & b.Y);
    }
    public static Vec2Uint operator ^(Vec2Uint a, Vec2Uint b)
    {
        return new Vec2Uint(a.X ^ b.X, a.Y ^ b.Y);
    }
    public static Vec2Uint operator ~(Vec2Uint a)
    {
        return new Vec2Uint(~a.X, ~a.Y);
    }
    public static Vec2Uint operator <<(Vec2Uint a, int b)
    {
        return new Vec2Uint(a.X << b, a.Y << b);
    }
    public static Vec2Uint operator >>(Vec2Uint a, int b)
    {
        return new Vec2Uint(a.X >> b, a.Y >> b);
    }
    public static bool operator ==(Vec2Uint a, Vec2Uint b)
    {
        return a.X == b.X && a.Y == b.Y;
    }
    public static bool operator !=(Vec2Uint a, Vec2Uint b)
    {
        return a.X != b.X || a.Y != b.Y;
    }
    public static bool operator >=(Vec2Uint a, Vec2Uint b)
    {
        return a.X >= b.X && a.Y >= b.Y;
    }
    public static bool operator <=(Vec2Uint a, Vec2Uint b)
    {
        return a.X <= b.X && a.Y <= b.Y;
    }
    public static bool operator >(Vec2Uint a, Vec2Uint b)
    {
        return a.X > b.X && a.Y > b.Y;
    }
    public static bool operator <(Vec2Uint a, Vec2Uint b)
    {
        return a.X < b.X && a.Y < b.Y;
    }
    
    private bool Equals(Vec2Uint other)
    {
        return X == other.X && Y == other.Y;
    }
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Vec2Uint)obj);
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
    
    // casts
    
    // to other
    public static implicit operator Vector2(Vec2Uint a)
    {
        return new Vector2(a.X, a.Y);
    }
    public static implicit operator Vector2i(Vec2Uint a)
    {
        return new Vector2i((int)a.X, (int)a.Y);
    }
    public static implicit operator Vector2h(Vec2Uint a)
    {
        return new Vector2h(a.X, a.Y);
    }
    public static implicit operator Vector2d(Vec2Uint a)
    {
        return new Vector2d(a.X, a.Y);
    }

    
    public static explicit operator Vec2Short(Vec2Uint a)
    {
        return new Vec2Short((short)a.X, (short)a.Y);
    }
    public static explicit operator Vec2Ushort(Vec2Uint a)
    {
        return new Vec2Ushort((ushort)a.X, (ushort)a.Y);
    }
    public static explicit operator Vec2Long(Vec2Uint a)
    {
        return new Vec2Long(a.X, a.Y);
    }
    public static explicit operator Vec2Ulong(Vec2Uint a)
    {
        return new Vec2Ulong(a.X, a.Y);
    }
    public static explicit operator Vec2Float(Vec2Uint a)
    {
        return new Vec2Float(a.X, a.Y);
    }
    public static implicit operator Vec2Double(Vec2Uint a)
    {
        return new Vec2Double(a.X, a.Y);
    }
    
    // from other
    public static explicit operator Vec2Uint(Vector2 a)
    {
        return new Vec2Uint((uint)a.X, (uint)a.Y);
    }
    public static explicit operator Vec2Uint(Vector2i a)
    {
        return new Vec2Uint((uint)a.X, (uint)a.Y);
    }
    public static explicit operator Vec2Uint(Vector2h a)
    {
        return new Vec2Uint((uint)a.X, (uint)a.Y);
    }
    public static explicit operator Vec2Uint(Vector2d a)
    {
        return new Vec2Uint((uint)a.X, (uint)a.Y);
    }
    
    public static explicit operator Vec2Uint(Vec2Ushort a)
    {
        return new Vec2Uint(a.X, a.Y);
    }
    public static explicit operator Vec2Uint(Vec2Long a)
    {
        return new Vec2Uint((uint)a.X, (uint)a.Y);
    }
    public static explicit operator Vec2Uint(Vec2Ulong a)
    {
        return new Vec2Uint((uint)a.X, (uint)a.Y);
    }
    public static explicit operator Vec2Uint(Vec2Float a)
    {
        return new Vec2Uint((uint)a.X, (uint)a.Y);
    }
    public static explicit operator Vec2Uint(Vec2Double a)
    {
        return new Vec2Uint((uint)a.X, (uint)a.Y);
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}

public readonly struct Vec2Long(long x, long y)
{
    public readonly long X = x;
    public readonly long Y = y;

    public Vec2Long(long a) : this(a, a)
    {
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
    public static Vec2Long operator /(Vec2Long a, long b)
    {
        return new Vec2Long(a.X / b, a.Y / b);
    }
    public static Vec2Long operator *(Vec2Long a, Vec2Long b)
    {
        return new Vec2Long(a.X * b.X, a.Y * b.Y);
    }
    public static Vec2Long operator *(Vec2Long a, long b)
    {
        return new Vec2Long(a.X * b, a.Y * b);
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
    
    // casts
    
    // to other
    public static implicit operator Vector2(Vec2Long a)
    {
        return new Vector2(a.X, a.Y);
    }
    public static implicit operator Vector2i(Vec2Long a)
    {
        return new Vector2i((int)a.X, (int)a.Y);
    }
    public static implicit operator Vector2h(Vec2Long a)
    {
        return new Vector2h(a.X, a.Y);
    }
    public static implicit operator Vector2d(Vec2Long a)
    {
        return new Vector2d(a.X, a.Y);
    }

    
    public static explicit operator Vec2Short(Vec2Long a)
    {
        return new Vec2Short((short)a.X, (short)a.Y);
    }
    public static explicit operator Vec2Ushort(Vec2Long a)
    {
        return new Vec2Ushort((ushort)a.X, (ushort)a.Y);
    }
    
    public static explicit operator Vec2Ulong(Vec2Long a)
    {
        return new Vec2Ulong((ulong)a.X, (ulong)a.Y);
    }
    public static explicit operator Vec2Float(Vec2Long a)
    {
        return new Vec2Float(a.X, a.Y);
    }
    public static implicit operator Vec2Double(Vec2Long a)
    {
        return new Vec2Double(a.X, a.Y);
    }
    
    // from other
    public static explicit operator Vec2Long(Vector2 a)
    {
        return new Vec2Long((long)a.X, (long)a.Y);
    }
    public static explicit operator Vec2Long(Vector2i a)
    {
        return new Vec2Long(a.X, a.Y);
    }
    public static explicit operator Vec2Long(Vector2h a)
    {
        return new Vec2Long((long)a.X, (long)a.Y);
    }
    public static explicit operator Vec2Long(Vector2d a)
    {
        return new Vec2Long((long)a.X, (long)a.Y);
    }
    
    public static explicit operator Vec2Long(Vec2Ulong a)
    {
        return new Vec2Long((long)a.X, (long)a.Y);
    }
    public static explicit operator Vec2Long(Vec2Float a)
    {
        return new Vec2Long((long)a.X, (long)a.Y);
    }
    public static explicit operator Vec2Long(Vec2Double a)
    {
        return new Vec2Long((long)a.X, (long)a.Y);
    }
    


    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}

public readonly struct Vec2Ulong(ulong x, ulong y)
{
    public readonly ulong X = x;
    public readonly ulong Y = y;

    public Vec2Ulong(ulong a) : this(a, a)
    {
    }
    
    public static Vec2Ulong operator +(Vec2Ulong a, Vec2Ulong b)
    {
        return new Vec2Ulong(a.X + b.X, a.Y + b.Y);
    }
    public static Vec2Ulong operator -(Vec2Ulong a, Vec2Ulong b)
    {
        return new Vec2Ulong(a.X - b.X, a.Y - b.Y);
    }
    public static Vec2Ulong operator /(Vec2Ulong a, Vec2Ulong b)
    {
        return new Vec2Ulong(a.X / b.X, a.Y / b.Y);
    }
    public static Vec2Ulong operator /(Vec2Ulong a, ulong b)
    {
        return new Vec2Ulong(a.X / b, a.Y / b);
    }
    public static Vec2Ulong operator *(Vec2Ulong a, Vec2Ulong b)
    {
        return new Vec2Ulong(a.X * b.X, a.Y * b.Y);
    }
    public static Vec2Ulong operator *(Vec2Ulong a, ulong b)
    {
        return new Vec2Ulong(a.X * b, a.Y * b);
    }
    public static Vec2Ulong operator %(Vec2Ulong a, Vec2Ulong b)
    {
        return new Vec2Ulong(a.X % b.X, a.Y % b.Y);
    }
    public static Vec2Ulong operator |(Vec2Ulong a, Vec2Ulong b)
    {
        return new Vec2Ulong(a.X | b.X, a.Y | b.Y);
    }
    public static Vec2Ulong operator &(Vec2Ulong a, Vec2Ulong b)
    {
        return new Vec2Ulong(a.X & b.X, a.Y & b.Y);
    }
    public static Vec2Ulong operator ^(Vec2Ulong a, Vec2Ulong b)
    {
        return new Vec2Ulong(a.X ^ b.X, a.Y ^ b.Y);
    }
    public static Vec2Ulong operator ~(Vec2Ulong a)
    {
        return new Vec2Ulong(~a.X, ~a.Y);
    }
    public static Vec2Ulong operator <<(Vec2Ulong a, int b)
    {
        return new Vec2Ulong(a.X << b, a.Y << b);
    }
    public static Vec2Ulong operator >>(Vec2Ulong a, int b)
    {
        return new Vec2Ulong(a.X >> b, a.Y >> b);
    }
    public static bool operator ==(Vec2Ulong a, Vec2Ulong b)
    {
        return a.X == b.X && a.Y == b.Y;
    }
    public static bool operator !=(Vec2Ulong a, Vec2Ulong b)
    {
        return a.X != b.X || a.Y != b.Y;
    }
    public static bool operator >=(Vec2Ulong a, Vec2Ulong b)
    {
        return a.X >= b.X && a.Y >= b.Y;
    }
    public static bool operator <=(Vec2Ulong a, Vec2Ulong b)
    {
        return a.X <= b.X && a.Y <= b.Y;
    }
    public static bool operator >(Vec2Ulong a, Vec2Ulong b)
    {
        return a.X > b.X && a.Y > b.Y;
    }
    public static bool operator <(Vec2Ulong a, Vec2Ulong b)
    {
        return a.X < b.X && a.Y < b.Y;
    }
    
    private bool Equals(Vec2Ulong other)
    {
        return X == other.X && Y == other.Y;
    }
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Vec2Ulong)obj);
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
    
    // casts
    
    // to other
    public static implicit operator Vector2(Vec2Ulong a)
    {
        return new Vector2(a.X, a.Y);
    }
    public static implicit operator Vector2i(Vec2Ulong a)
    {
        return new Vector2i((int)a.X, (int)a.Y);
    }
    public static implicit operator Vector2h(Vec2Ulong a)
    {
        return new Vector2h(a.X, a.Y);
    }
    public static implicit operator Vector2d(Vec2Ulong a)
    {
        return new Vector2d(a.X, a.Y);
    }

    
    public static explicit operator Vec2Short(Vec2Ulong a)
    {
        return new Vec2Short((short)a.X, (short)a.Y);
    }
    public static explicit operator Vec2Ushort(Vec2Ulong a)
    {
        return new Vec2Ushort((ushort)a.X, (ushort)a.Y);
    }
    public static explicit operator Vec2Float(Vec2Ulong a)
    {
        return new Vec2Float(a.X, a.Y);
    }
    public static implicit operator Vec2Double(Vec2Ulong a)
    {
        return new Vec2Double(a.X, a.Y);
    }
    
    // from other
    public static explicit operator Vec2Ulong(Vector2 a)
    {
        return new Vec2Ulong((ulong)a.X, (ulong)a.Y);
    }
    public static explicit operator Vec2Ulong(Vector2i a)
    {
        return new Vec2Ulong((ulong)a.X, (ulong)a.Y);
    }
    public static explicit operator Vec2Ulong(Vector2h a)
    {
        return new Vec2Ulong((ulong)a.X, (ulong)a.Y);
    }
    public static explicit operator Vec2Ulong(Vector2d a)
    {
        return new Vec2Ulong((ulong)a.X, (ulong)a.Y);
    }
    public static explicit operator Vec2Ulong(Vec2Float a)
    {
        return new Vec2Ulong((ulong)a.X, (ulong)a.Y);
    }
    public static explicit operator Vec2Ulong(Vec2Double a)
    {
        return new Vec2Ulong((ulong)a.X, (ulong)a.Y);
    }
    


    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}

public readonly struct Vec2Float(float x, float y)
{
    public readonly float X = x;
    public readonly float Y = y;

    public Vec2Float(float a) : this(a, a)
    {
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
    public static Vec2Float operator /(Vec2Float a, float b)
    {
        return new Vec2Float(a.X / b, a.Y / b);
    }
    public static Vec2Float operator *(Vec2Float a, Vec2Float b)
    {
        return new Vec2Float(a.X * b.X, a.Y * b.Y);
    }
    public static Vec2Float operator *(Vec2Float a, float b)
    {
        return new Vec2Float(a.X * b, a.Y * b);
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
    
    // casts
    
    // to other
    public static implicit operator Vector2(Vec2Float a)
    {
        return new Vector2(a.X, a.Y);
    }
    public static implicit operator Vector2i(Vec2Float a)
    {
        return new Vector2i((int)a.X, (int)a.Y);
    }
    public static implicit operator Vector2h(Vec2Float a)
    {
        return new Vector2h(a.X, a.Y);
    }
    public static implicit operator Vector2d(Vec2Float a)
    {
        return new Vector2d(a.X, a.Y);
    }

    
    public static explicit operator Vec2Short(Vec2Float a)
    {
        return new Vec2Short((short)a.X, (short)a.Y);
    }
    public static explicit operator Vec2Ushort(Vec2Float a)
    {
        return new Vec2Ushort((ushort)a.X, (ushort)a.Y);
    }
    public static implicit operator Vec2Double(Vec2Float a)
    {
        return new Vec2Double(a.X, a.Y);
    }
    
    // from other
    public static explicit operator Vec2Float(Vector2 a)
    {
        return new Vec2Float(a.X, a.Y);
    }
    public static explicit operator Vec2Float(Vector2i a)
    {
        return new Vec2Float(a.X, a.Y);
    }
    public static explicit operator Vec2Float(Vector2h a)
    {
        return new Vec2Float(a.X, a.Y);
    }
    public static explicit operator Vec2Float(Vector2d a)
    {
        return new Vec2Float((float)a.X, (float)a.Y);
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

public readonly struct Vec2Double(double x, double y)
{
    public readonly double X = x;
    public readonly double Y = y;

    public Vec2Double(double a) : this(a, a)
    {
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
    
    // casts
    
    // to other
    public static implicit operator Vector2(Vec2Double a)
    {
        return new Vector2((float)a.X, (float)a.Y);
    }
    public static implicit operator Vector2i(Vec2Double a)
    {
        return new Vector2i((int)a.X, (int)a.Y);
    }
    public static implicit operator Vector2h(Vec2Double a)
    {
        return new Vector2h((float)a.X, (float)a.Y);
    }
    public static implicit operator Vector2d(Vec2Double a)
    {
        return new Vector2d(a.X, a.Y);
    }

    
    public static explicit operator Vec2Short(Vec2Double a)
    {
        return new Vec2Short((short)a.X, (short)a.Y);
    }
    public static explicit operator Vec2Ushort(Vec2Double a)
    {
        return new Vec2Ushort((ushort)a.X, (ushort)a.Y);
    }
    
    // from other
    public static explicit operator Vec2Double(Vector2 a)
    {
        return new Vec2Double(a.X, a.Y);
    }
    public static explicit operator Vec2Double(Vector2i a)
    {
        return new Vec2Double(a.X, a.Y);
    }
    public static explicit operator Vec2Double(Vector2h a)
    {
        return new Vec2Double(a.X, a.Y);
    }
    public static explicit operator Vec2Double(Vector2d a)
    {
        return new Vec2Double(a.X, a.Y);
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}
