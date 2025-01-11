using System.Runtime.InteropServices;

namespace Math2D.Binary;

/// <summary>
/// Utilities performing bit/byte manipulation
/// </summary>
public static class BitUtil
{
    #region Other
    
    /// <summary>
    /// Returns 2 ^ <paramref name="v"/>.
    /// </summary>
    /// <param name="v">the value. Maximum is 64</param>
    public static ulong Pow2(int v)
    {
        return ~(v == 64 ? 0 : ~0x0uL << v);
    }

    public static byte Flip(this byte b)
    {
        return (byte)(
            ((b & 0b10000000) >> 7) |
            ((b & 0b01000000) >> 5) |
            ((b & 0b00100000) >> 3) |
            ((b & 0b00010000) >> 1) |
            ((b & 0b00001000) << 1) |
            ((b & 0b00000100) << 3) |
            ((b & 0b00000010) << 5) |
            ((b & 0b00000001) << 7));
    }
    
    #endregion
    
    #region Zero Count
    
    public static uint TrailingZeros(uint v)
    {
        if (v == 0) return 0;
        
        var bits = 0u;
        
        if ((v & 0x0000FFFF) == 0) { bits += 16; v >>= 16; }
        if ((v & 0x000000FF) == 0) { bits +=  8; v >>=  8; }
        if ((v & 0x0000000F) == 0) { bits +=  4; v >>=  4; }
        if ((v & 0x00000003) == 0) { bits +=  2; v >>=  2; }
        if ((v & 0x00000001) == 0) { bits +=  1; }
        
        return bits;
    }
    
    public static uint TrailingZeros(ulong v)
    {
        if (v == 0) return 0;
        
        var bits = 0u;
        
        if ((v & 0x00000000FFFFFFFFuL) == 0) { bits += 32; v >>= 32; }
        if ((v & 0x000000000000FFFFuL) == 0) { bits += 16; v >>= 16; }
        if ((v & 0x00000000000000FFuL) == 0) { bits +=  8; v >>=  8; }
        if ((v & 0x000000000000000FuL) == 0) { bits +=  4; v >>=  4; }
        if ((v & 0x0000000000000003uL) == 0) { bits +=  2; v >>=  2; }
        if ((v & 0x0000000000000001uL) == 0) { bits +=  1; }
        
        return bits;
    }
    
    public static uint TrailingZeros(UInt128 v)
    {
        if (v == 0) return 0;
        
        var bits = 0u;
        
        if ((v & new UInt128(0x0000000000000000, 0xFFFFFFFFFFFFFFFF)) == 0) { bits += 64; v >>= 64; }
        if ((v & new UInt128(0x0000000000000000, 0x00000000FFFFFFFF)) == 0) { bits += 32; v >>= 32; }
        if ((v & new UInt128(0x0000000000000000, 0x000000000000FFFF)) == 0) { bits += 16; v >>= 16; }
        if ((v & new UInt128(0x0000000000000000, 0x00000000000000FF)) == 0) { bits +=  8; v >>=  8; }
        if ((v & new UInt128(0x0000000000000000, 0x000000000000000F)) == 0) { bits +=  4; v >>=  4; }
        if ((v & new UInt128(0x0000000000000000, 0x0000000000000003)) == 0) { bits +=  2; v >>=  2; }
        if ((v & new UInt128(0x0000000000000000, 0x0000000000000001)) == 0) { bits +=  1; }
        
        return bits;
    }
    
    public static uint LeadingZeros(uint v)
    {
        if (v == 0) return 32;
        
        var bits = 0u;
        
        if ((v & 0xFFFF0000) == 0) { bits += 16; v <<= 16; }
        if ((v & 0xFF000000) == 0) { bits +=  8; v <<=  8; }
        if ((v & 0xF0000000) == 0) { bits +=  4; v <<=  4; }
        if ((v & 0xC0000000) == 0) { bits +=  2; v <<=  2; }
        if ((v & 0x80000000) == 0) { bits +=  1; }
        
        return bits;
    }
    
    public static uint LeadingZeros(ulong v)
    {
        if (v == 0) return 64;
        
        var bits = 0u;
        
        if ((v & 0xFFFFFFFF00000000uL) == 0) { bits += 32; v <<= 32; }
        if ((v & 0xFFFF000000000000uL) == 0) { bits += 16; v <<= 16; }
        if ((v & 0xFF00000000000000uL) == 0) { bits +=  8; v <<=  8; }
        if ((v & 0xF000000000000000uL) == 0) { bits +=  4; v <<=  4; }
        if ((v & 0xC000000000000000uL) == 0) { bits +=  2; v <<=  2; }
        if ((v & 0x8000000000000000uL) == 0) { bits +=  1; }
        
        return bits;
    }
    
    public static uint LeadingZeros(UInt128 v)
    {
        if (v == 0) return 128;
        
        var bits = 0u;
        
        if ((v & new UInt128(0xFFFFFFFFFFFFFFFF, 0x0000000000000000)) == 0) { bits += 64; v <<= 64; }
        if ((v & new UInt128(0xFFFFFFFF00000000, 0x0000000000000000)) == 0) { bits += 32; v <<= 32; }
        if ((v & new UInt128(0xFFFF000000000000, 0x0000000000000000)) == 0) { bits += 16; v <<= 16; }
        if ((v & new UInt128(0xFF00000000000000, 0x0000000000000000)) == 0) { bits +=  8; v <<=  8; }
        if ((v & new UInt128(0xF000000000000000, 0x0000000000000000)) == 0) { bits +=  4; v <<=  4; }
        if ((v & new UInt128(0xC000000000000000, 0x0000000000000000)) == 0) { bits +=  2; v <<=  2; }
        if ((v & new UInt128(0x8000000000000000, 0x0000000000000000)) == 0) { bits +=  1; }
        
        return bits;
    }
    
    #endregion
    
    #region Powers of 2
    
    /// <summary>
    /// Computes 2^n - 1, returning the result as a long, and never exceeding the limit of 2^63.
    /// </summary>
    /// <param name="v">n; the value to return 2^n - 1 of</param>
    public static long Pow2Min1L(int v)
    {
        return ~(v == 63 ? 0 : ~0x0L << v);
    }
    
    /// <summary>
    /// Computes 2^n - 1, returning the result as a ulong, and never exceeding the limit of 2^64.
    /// </summary>
    /// <param name="v">n; the value to return 2^n - 1 of</param>
    // ReSharper disable once InconsistentNaming
    public static ulong Pow2Min1uL(int v)
    {
        return ~(v == 64 ? 0 : ~0x0uL << v);
    }
    
    /// <summary>
    /// Computes 2^n - 1, returning the result as a UInt128, and never exceeding the limit of 2^128.
    /// </summary>
    /// <param name="v">n; the value to return 2^n - 1 of</param>
    public static UInt128 Pow2Min1U128(int v)
    {
        return ~(v == 128 ? 0 : ~(UInt128)0x0 << v);
    }

    public static (long Min, long Max) Pow2MinMax(int v)
    {
        var min = -1L << (v - 1);
        var max = -(min + 1);
        
        return (min, max);
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
        if (v == 0) return 0;
        
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
    
    #endregion
    
    #region Interleaving and Deinterleaving
    
    private static readonly UInt128[] Masks = [
        new(0b0101010101010101010101010101010101010101010101010101010101010101uL, 0b0101010101010101010101010101010101010101010101010101010101010101uL),
        new(0b0011001100110011001100110011001100110011001100110011001100110011uL, 0b0011001100110011001100110011001100110011001100110011001100110011uL),
        new(0b0000111100001111000011110000111100001111000011110000111100001111uL, 0b0000111100001111000011110000111100001111000011110000111100001111uL),
        new(0b0000000011111111000000001111111100000000111111110000000011111111uL, 0b0000000011111111000000001111111100000000111111110000000011111111uL),
        new(0b0000000000000000111111111111111100000000000000001111111111111111uL, 0b0000000000000000111111111111111100000000000000001111111111111111uL),
        new(0b0000000000000000000000000000000011111111111111111111111111111111uL, 0b0000000000000000000000000000000011111111111111111111111111111111uL),
        new(0b0000000000000000000000000000000000000000000000000000000000000000uL, 0b1111111111111111111111111111111111111111111111111111111111111111uL),
    ];
    
    /// <summary>
    /// Interleaves 2 64-bit unsigned integers, producing an unsigned 128-bit integer. Does the inverse of <see cref="Deinterleave(UInt128)"/>.
    /// </summary>
    /// <param name="vec">the 2 64-bit integers to interleave</param>
    public static UInt128 Interleave(Vec2<ulong> vec)
    {
        var x = (UInt128)vec.X;
        var y = (UInt128)vec.Y;
        
        x = (x | (x << 32)) & Masks[5];
        x = (x | (x << 16)) & Masks[4];
        x = (x | (x <<  8)) & Masks[3];
        x = (x | (x <<  4)) & Masks[2];
        x = (x | (x <<  2)) & Masks[1];
        x = (x | (x <<  1)) & Masks[0];
        
        y = (y | (y << 32)) & Masks[5];
        y = (y | (y << 16)) & Masks[4];
        y = (y | (y <<  8)) & Masks[3];
        y = (y | (y <<  4)) & Masks[2];
        y = (y | (y <<  2)) & Masks[1];
        y = (y | (y <<  1)) & Masks[0];
        y <<= 1;
        
        return x | y;
    }
    
    /// <summary>
    /// Deinterleaves an unsigned 128-bit integer, producing 2 unsigned 64-bit integers. Does the inverse of <see cref="Interleave(Vec2{ulong})"/>.
    /// </summary>
    /// <param name="zValue">the unsigned 128-bit integer to deinterleave</param>
    /// <returns></returns>
    public static Vec2<ulong> Deinterleave(UInt128 zValue)
    {
        var x = zValue & Masks[0];
        var y = (zValue >> 1) & Masks[0];

        x = (x | (x >> 1)) & Masks[1];
        x = (x | (x >> 2)) & Masks[2];
        x = (x | (x >> 4)) & Masks[3];
        x = (x | (x >> 8)) & Masks[4];
        x = (x | (x >> 16)) & Masks[5];
        x = (x | (x >> 32)) & Masks[6];
        
        y = (y | (y >> 1)) & Masks[1];
        y = (y | (y >> 2)) & Masks[2];
        y = (y | (y >> 4)) & Masks[3];
        y = (y | (y >> 8)) & Masks[4];
        y = (y | (y >> 16)) & Masks[5];
        y = (y | (y >> 32)) & Masks[6];

        return ((ulong)x, (ulong)y);
    }
    
    #endregion
    
    #region Signed and Unsigned Conversion
    
    /// <summary>
    /// Converts an unsigned 64-bit integer into a signed 64-bit integer such that larger numbers remain larger after signing.
    /// </summary>
    /// <param name="u">the unsigned 64-bit integer to convert</param>
    /// <param name="b">the amount of bits to consider</param>
    /// <returns>A signed 64-bit integer representing the original unsigned version</returns>
    public static long Sign(ulong u, int b)
    {
        var s = (long)u ^ (0x1L << (b - 1));
        if (b == 64) return s;
        return (-(s >> (b - 1) & 0x1) << b) | s;
    }
    
    /// <summary>
    /// Converts a signed 64-bit integer into an unsigned 64-bit integer such that larger numbers remain larger after unsigning.
    /// </summary>
    /// <param name="i">the signed 64-bit integer to convert</param>
    /// <param name="b">the amount of bits to consider</param>
    /// <returns>An unsigned 64-bit integer representing the original signed version</returns>
    public static ulong Unsign(long i, int b)
    {
        var mask = b == 64 ? ~0uL : ~(~0uL << b);
        return ((ulong)i & mask) ^ (0x1uL << (b - 1));
    }
    
    #endregion
    
    #region Byte[] Actions
    
    public static byte[] GetBytes<T>(T str) where T : struct
    {
        var size = Marshal.SizeOf(str);
        var bytes = new byte[size];

        var ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, bytes, 0, size);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
        return bytes;
    }
    
    public static byte[] GetBytes(short v)
    {
        return [(byte)((v >> 8) & 0xFF), (byte)(v & 0xFF)];
    }
    
    public static byte[] GetBytes(ushort v)
    {
        return [(byte)((v >> 8) & 0xFF), (byte)(v & 0xFF)];
    }
    
    public static byte[] GetBytes(int v)
    {
        return [(byte)((v >> 24) & 0xFF), (byte)((v >> 16) & 0xFF), (byte)((v >> 8) & 0xFF), (byte)(v & 0xFF)];
    }
    
    public static byte[] GetBytes(uint v)
    {
        return [(byte)((v >> 24) & 0xFF), (byte)((v >> 16) & 0xFF), (byte)((v >> 8) & 0xFF), (byte)(v & 0xFF)];
    }
    
    public static byte[] GetBytes(long v)
    {
        return [
            (byte)((v >> 56) & 0xFF),
            (byte)((v >> 48) & 0xFF),
            (byte)((v >> 40) & 0xFF),
            (byte)((v >> 32) & 0xFF),
            (byte)((v >> 24) & 0xFF),
            (byte)((v >> 16) & 0xFF),
            (byte)((v >>  8) & 0xFF),
            (byte)(v & 0xFF),
        ];
    }
    
    public static byte[] GetBytes(ulong v)
    {
        return [
            (byte)((v >> 56) & 0xFF),
            (byte)((v >> 48) & 0xFF),
            (byte)((v >> 40) & 0xFF),
            (byte)((v >> 32) & 0xFF),
            (byte)((v >> 24) & 0xFF),
            (byte)((v >> 16) & 0xFF),
            (byte)((v >>  8) & 0xFF),
            (byte)(v & 0xFF),
        ];
    }
    
    public static short GetShort(Span<byte> d)
    {
        return (short)((d[0] << 8) | d[1]);
    }
    
    public static ushort GetUShort(Span<byte> d)
    {
        return (ushort)((d[0] << 8) | d[1]);
    }
    
    public static int GetInt(Span<byte> d)
    {
        return (d[0] << 24) |
               (d[1] << 16) |
               (d[2] <<  8) |
               d[3];
    }
    
    public static uint GetUInt(Span<byte> d)
    {
        return (uint)(d[0] << 24) |
               (uint)(d[1] << 16) |
               (uint)(d[2] <<  8) |
               d[3];
    }
    
    public static long GetLong(Span<byte> d)
    {
        return ((long)d[0] << 56) |
               ((long)d[1] << 48) |
               ((long)d[2] << 40) |
               ((long)d[3] << 32) |
               ((long)d[4] << 24) |
               ((long)d[5] << 16) |
               ((long)d[6] <<  8) |
               d[7];
    }
    
    public static ulong GetULong(Span<byte> d)
    {
        return ((ulong)d[0] << 56) |
               ((ulong)d[1] << 48) |
               ((ulong)d[2] << 40) |
               ((ulong)d[3] << 32) |
               ((ulong)d[4] << 24) |
               ((ulong)d[5] << 16) |
               ((ulong)d[6] <<  8) |
               d[7];
    }
    
    public static bool CompareBytes(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;

        return !a.Where((t, i) => t != b[i]).Any();
    }
    
    /// <summary>
    /// Calculates the minimum amount of bytes needed to represent a 64-bit unsigned integer.
    /// </summary>
    /// <param name="v">the 64-bit unsigned integer</param>
    public static uint MinByteCount(ulong v)
    {
        if (v == 0) return 0;
        
        var bytes = 0u;
        
        if ((v & 0xFFFFFFFF00000000uL) == 0) { bytes += 4; v <<= 32; }
        if ((v & 0xFFFF000000000000uL) == 0) { bytes += 2; v <<= 16; }
        if ((v & 0xFF00000000000000uL) == 0) { bytes += 1; }
        
        return 8 - bytes;
    }
    
    #endregion
    
    #region Byte[] Actions Big Endian
    
    public static byte[] GetBytesBe<T>(T str) where T : struct
    {
        var size = Marshal.SizeOf(str);
        var bytes = new byte[size];

        var ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, bytes, 0, size);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
        return bytes.Reverse().ToArray();
    }
    
    public static byte[] GetBytesBe(short v)
    {
        return [(byte)(v & 0xFF), (byte)((v >> 8) & 0xFF)];
    }
    
    public static byte[] GetBytesBe(ushort v)
    {
        return [(byte)(v & 0xFF), (byte)((v >> 8) & 0xFF)];
    }
    
    public static byte[] GetBytesBe(int v)
    {
        return [(byte)(v & 0xFF), (byte)((v >> 8) & 0xFF), (byte)((v >> 16) & 0xFF), (byte)((v >> 24) & 0xFF)];
    }
    
    public static byte[] GetBytesBe(uint v)
    {
        return [(byte)(v & 0xFF), (byte)((v >> 8) & 0xFF), (byte)((v >> 16) & 0xFF), (byte)((v >> 24) & 0xFF)];
    }
    
    public static byte[] GetBytesBe(long v)
    {
        return [
            (byte)(v & 0xFF),
            (byte)((v >>  8) & 0xFF),
            (byte)((v >> 16) & 0xFF),
            (byte)((v >> 24) & 0xFF),
            (byte)((v >> 32) & 0xFF),
            (byte)((v >> 40) & 0xFF),
            (byte)((v >> 48) & 0xFF),
            (byte)((v >> 56) & 0xFF),
        ];
    }
    
    public static byte[] GetBytesBe(ulong v)
    {
        return [
            (byte)(v & 0xFF),
            (byte)((v >>  8) & 0xFF),
            (byte)((v >> 16) & 0xFF),
            (byte)((v >> 24) & 0xFF),
            (byte)((v >> 32) & 0xFF),
            (byte)((v >> 40) & 0xFF),
            (byte)((v >> 48) & 0xFF),
            (byte)((v >> 56) & 0xFF),
        ];
    }
    
    public static short GetShortBe(Span<byte> d)
    {
        return (short)((d[1] << 8) | d[0]);
    }
    
    public static ushort GetUShortBe(Span<byte> d)
    {
        return (ushort)((d[1] << 8) | d[0]);
    }
    
    public static int GetIntBe(Span<byte> d)
    {
        return (d[3] << 24) |
               (d[2] << 16) |
               (d[1] <<  8) |
               d[0];
    }
    
    public static uint GetUIntBe(Span<byte> d)
    {
        return (uint)(d[3] << 24) |
               (uint)(d[2] << 16) |
               (uint)(d[1] <<  8) |
               d[0];
    }
    
    public static long GetLongBe(Span<byte> d)
    {
        return ((long)d[7] << 56) |
               ((long)d[6] << 48) |
               ((long)d[5] << 40) |
               ((long)d[4] << 32) |
               ((long)d[3] << 24) |
               ((long)d[2] << 16) |
               ((long)d[1] <<  8) |
               d[0];
    }
    
    public static ulong GetULongBe(Span<byte> d)
    {
        return ((ulong)d[7] << 56) |
               ((ulong)d[6] << 48) |
               ((ulong)d[5] << 40) |
               ((ulong)d[4] << 32) |
               ((ulong)d[3] << 24) |
               ((ulong)d[2] << 16) |
               ((ulong)d[1] <<  8) |
               d[0];
    }
    
    #endregion
}
