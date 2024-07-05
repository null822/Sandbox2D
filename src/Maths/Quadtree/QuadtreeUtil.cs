using System;
using System.Collections.Generic;

namespace Sandbox2D.Maths.Quadtree;

public static class QuadtreeUtil
{
    
    /// <summary>
    /// Calculates the offset of a node within this node relative to this node.
    /// </summary>
    /// <param name="nodeIndex">an index to a node within this node. Range 0-3</param>
    /// <param name="height">the height of this node</param>
    /// <exception cref="nodeIndex">Thrown when <paramref name="nodeIndex"/> is out of range.</exception>
    public static Vec2<long> GetNodeOffset(int nodeIndex, int height)
    {
        var quartWidth = 0x1L << (height - 2);
        return nodeIndex switch
        {
            0 => (-quartWidth, -quartWidth),
            1 => ( quartWidth, -quartWidth),
            2 => (-quartWidth, +quartWidth),
            3 => ( quartWidth, +quartWidth),
            _ => throw new ArgumentOutOfRangeException(nameof(nodeIndex), nodeIndex, "Node index range is 0-3")
        };
    }
    
    /// <summary>
    /// Calculates the index of the sub node that the supplied point resides in.
    /// </summary>
    /// <param name="pos">the position</param>
    /// <param name="center">the center of this node</param>
    public static int GetNodeIndex(Vec2<long> pos, Vec2<long> center)
    {
        var x = pos.X >= center.X;
        var y = pos.Y >= center.Y;
        
        return (x, y) switch
        {
            (false, false) => 0,
            ( true, false) => 1,
            (false,  true) => 2,
            ( true,  true) => 3
        };
    }
    
    /// <summary>
    /// Converts 2D coordinates into a Morton Code.
    /// </summary>
    /// <param name="coords">the coordinates to convert</param>
    /// <param name="maxHeight">the maximum height of the quadtree</param>
    /// <returns>A 128-bit unsigned integer containing the Morton Code</returns>
    public static UInt128 Interleave(Vec2<long> coords, int maxHeight)
    {
        var unsigned = new Vec2<ulong>(Unsign(coords.X, maxHeight), Unsign(coords.Y, maxHeight));
        return Interleave(unsigned);
    }
    
    /// <summary>
    /// Converts a Morton Code into 2D coordinates.
    /// </summary>
    /// <param name="zValue">the Morton Code to convert</param>
    /// <param name="maxHeight">the maximum height of the quadtree</param>
    /// <returns>A vector containing the 2D coordinates</returns>
    public static Vec2<long> Deinterleave(UInt128 zValue, int maxHeight)
    {
        var unsigned = Deinterleave(zValue);
        
        return new Vec2<long>(Sign(unsigned.X, maxHeight), Sign(unsigned.Y, maxHeight));
    }
    
    /// <summary>
    /// Extracts a 2-bit section from a Morton Code, which can be used to index <see cref="QuadtreeNode"/>s.
    /// </summary>
    /// <param name="zValue">the Morton Code to take the 2 bits from</param>
    /// <param name="height">the position of the bits to extract. Equivalent to the height of the child node of the node being indexed</param>
    public static int ZValueIndex(UInt128 zValue, int height)
    {
        return (int)((zValue >> (2 * height)) & 0b11);
    }
    
    /// <summary>
    /// Rounds off a z-value, calculating the z-value of the node containing the specified <see cref="zValue"/>, at a
    /// height of the specified <see cref="height"/>.
    /// </summary>
    /// <param name="zValue">the z-value to round</param>
    /// <param name="height">the height</param>
    /// <returns>The rounded off z-value</returns>
    public static UInt128 RoundZValue(UInt128 zValue, int height)
    {
        var mask = UInt128.MaxValue << (height * 2);
        return zValue & mask;
    }
    
    /// <summary>
    /// Converts the coordinate and height of a node into a <see cref="Range2D"/>.
    /// </summary>
    /// <param name="bl">the <see cref="Range2D.Bl"/> corner of the resulting <see cref="Range2D"/></param>
    /// <param name="height">the height of the node</param>
    /// <returns>The node represented as a <see cref="Range2D"/></returns>
    public static Range2D NodeRangeFromPos(Vec2<long> bl, int height)
    {
        if (height == 64)
        {
            return new Range2D(new Vec2<long>(long.MinValue), new Vec2<long>(long.MaxValue));
        }
        
        var size = ~(~0L << height);
        var tr = bl + new Vec2<long>(size);
        
        return new Range2D(bl, tr);
    }

    /// <summary>
    /// Computes the maximum size of any node at the given z-value provided the maximum height of the entire quadtree
    /// </summary>
    /// <param name="zValue">the z-value of the node in question</param>
    /// <param name="maxHeight">the maximum height of the entire quadtree</param>
    /// <returns>the maximum size expressed as a height measured from the smallest possible node in the quadtree</returns>
    public static int CalculateLargestHeight(UInt128 zValue, int maxHeight)
    {
        // 1 less than the maximum z-value for the quadtree
        var maxZValue = ~(maxHeight == 64 ? 0 : ~(UInt128)0x0 << (2 * maxHeight));
        var target = maxZValue - zValue + 1;
        
        if (target == 0) return 0;
        
        return TrailingZeros(target) / 2;
    }
    
    /// <summary>
    /// A <see cref="Comparer{T}"/> that allows for sorting <see cref="Range2D"/>s by their <see cref="Range2D.Bl"/>
    /// corner in an increasing, bottom-left to top right-order.
    /// </summary>
    public static readonly Comparer<Range2D> RangeBlComparer = Comparer<Range2D>.Create((a, b) =>
    {
        var ac = a.Bl;
        var bc = b.Bl;

        if (ac == bc) return 0;
        if (ac <= bc) return -1;
        return 1;
    });
    
    /// <summary>
    /// Computes the amount of trailing zeros in a given 128-bit unsigned integer
    /// </summary>
    private static int TrailingZeros(UInt128 v)
    {
        if ((v & 0x1) == 0x1)
            return 0;
        
        if (v == 0)
            return 128;
        
        var c = 1;
        if ((v & 0xffffffffffffffffL) == 0)
        {
            v >>= 64;
            c += 64;
        }
        if ((v & 0xffffffffL) == 0)
        {
            v >>= 32;
            c += 32;
        }
        if ((v & 0xffffL) == 0)
        {
            v >>= 16;
            c += 16;
        }
        if ((v & 0xffL) == 0)
        {
            v >>= 8;
            c += 8;
        }
        if ((v & 0b1111L) == 0)
        {
            v >>= 4;
            c += 4;
        }
        if ((v & 0b11L) == 0)
        {
            v >>= 2;
            c += 2;
        }
        c -= (int)(v & 0x1);
        
        return c;
    }
    
    private static readonly UInt128[] Masks = [
        new UInt128(0b0101010101010101010101010101010101010101010101010101010101010101uL, 0b0101010101010101010101010101010101010101010101010101010101010101uL),
        new UInt128(0b0011001100110011001100110011001100110011001100110011001100110011uL, 0b0011001100110011001100110011001100110011001100110011001100110011uL),
        new UInt128(0b0000111100001111000011110000111100001111000011110000111100001111uL, 0b0000111100001111000011110000111100001111000011110000111100001111uL),
        new UInt128(0b0000000011111111000000001111111100000000111111110000000011111111uL, 0b0000000011111111000000001111111100000000111111110000000011111111uL),
        new UInt128(0b0000000000000000111111111111111100000000000000001111111111111111uL, 0b0000000000000000111111111111111100000000000000001111111111111111uL),
        new UInt128(0b0000000000000000000000000000000011111111111111111111111111111111uL, 0b0000000000000000000000000000000011111111111111111111111111111111uL),
        new UInt128(0b0000000000000000000000000000000000000000000000000000000000000000uL, 0b1111111111111111111111111111111111111111111111111111111111111111uL),
    ];

    /// <summary>
    /// Interleaves 2 64-bit unsigned integers, producing an unsigned 128-bit integer. Does the inverse of <see cref="Deinterleave(UInt128)"/>.
    /// </summary>
    /// <param name="vec">the 2 64-bit integers to interleave</param>
    private static UInt128 Interleave(Vec2<ulong> vec)
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
    private static Vec2<ulong> Deinterleave(UInt128 zValue)
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

    /// <summary>
    /// Converts an unsigned 64-bit integer into a signed 64-bit integer such that larger numbers remain larger after signing.
    /// </summary>
    /// <param name="u">the unsigned 64-bit integer to convert</param>
    /// <param name="b">the amount of bits to consider</param>
    /// <returns>A signed 64-bit integer representing the original unsigned version</returns>
    private static long Sign(ulong u, int b)
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
    private static ulong Unsign(long i, int b)
    {
        var mask = b == 64 ? ~0uL : ~(~0uL << b);
        return ((ulong)i & mask) ^ (0x1uL << (b - 1));
    }
    

}
