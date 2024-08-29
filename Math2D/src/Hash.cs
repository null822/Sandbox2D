using System.Security.Cryptography;
using System.Text.Json;

namespace Math2D;

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