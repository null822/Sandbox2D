using Math2D.Binary;

namespace Math2D.Quadtree;

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
        var unsigned = new Vec2<ulong>(BitUtil.Unsign(coords.X, maxHeight), BitUtil.Unsign(coords.Y, maxHeight));
        return BitUtil.Interleave(unsigned);
    }
    
    /// <summary>
    /// Converts a Morton Code into 2D coordinates.
    /// </summary>
    /// <param name="zValue">the Morton Code to convert</param>
    /// <param name="maxHeight">the maximum height of the quadtree</param>
    /// <returns>A vector containing the 2D coordinates</returns>
    public static Vec2<long> Deinterleave(UInt128 zValue, int maxHeight)
    {
        var unsigned = BitUtil.Deinterleave(zValue);
        
        return new Vec2<long>(BitUtil.Sign(unsigned.X, maxHeight), BitUtil.Sign(unsigned.Y, maxHeight));
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
        var mask = height == 64 ? 0 : UInt128.MaxValue << (height * 2);
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
        var sizeM1 = BitUtil.Pow2Min1uL(height); 
        var tr = new Vec2<long>((long)((ulong)bl.X + sizeM1), (long)((ulong)bl.Y + sizeM1));
        
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
        
        return (int)BitUtil.TrailingZeros(target) / 2;
    }
    
    /// <summary>
    /// A <see cref="Comparer{T}"/> that allows for sorting <see cref="Range2D"/>s by their <see cref="Range2D.Bl"/>
    /// corner in an increasing, bottom-left to top right-order.
    /// </summary>
    public static readonly Comparer<Modification> RangeBlComparer = Comparer<Modification>.Create((a, b) =>
    {
        var ac = a.Range.Bl;
        var bc = b.Range.Bl;

        if (ac == bc) return 0;
        if (ac <= bc) return -1;
        return 1;
    });
    
}
