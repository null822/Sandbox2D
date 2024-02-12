
using System;
using SixLabors.ImageSharp;

namespace Sandbox2D.Maths;

public readonly struct Range2D
{
    /// <summary>
    /// Left coordinate.
    /// </summary>
    public readonly long MinX;
    /// <summary>
    /// Right coordinate.
    /// </summary>
    public readonly long MinY;
    /// <summary>
    /// Bottom coordinate.
    /// </summary>
    public readonly long MaxX;
    /// <summary>
    /// Top coordinate.
    /// </summary>
    public readonly long MaxY;
    
    public Vec2<long> BottomLeft => MinXMinY;
    public Vec2<long> TopLeft => MinXMaxY;
    public Vec2<long> BottomRight => MaxXMinY;
    public Vec2<long> TopRight => MaxXMaxY;
        
    public Vec2<long> MinXMinY => new(MinX, MinY);
    public Vec2<long> MinXMaxY => new(MinX, MaxY);
    public Vec2<long> MaxXMinY => new(MaxX, MinY);
    public Vec2<long> MaxXMaxY => new(MaxX, MaxY);
    
    public long Width => MaxX - MinX;
    public long Height => MaxY - MinY;
    
    /// <summary>
    /// The area of the Range2D, in units^2.
    /// </summary>
    public long Area => Width * Height;
    
    /// <summary>
    /// The point that resides in the center of the range, returned as an integer.
    /// </summary>
    public Vec2<long> Center => ((MinX + MaxX) / 2, (MinY + MaxY) / 2);
    
    /// <summary>
    /// The point that resides in the center of the range, returned as a floating-point integer.
    /// </summary>
    public Vec2<decimal> CenterF => ((MinX + MaxX) / (decimal)2, (MinY + MaxY) / (decimal)2);
    
    /// <summary>
    /// Returns the maximum distance that any point within this Range2D can be from the the center.
    /// </summary>
    public long MaxExtension => Math.Max(Width, Height) / 2;
    
    /// <summary>
    /// This Range2D, centered at (0, 0).
    /// </summary>
    public Range2D SizeOnly => new Range2D((0, 0), Width, Height);

    /// <summary>
    /// Constructs a Range2D, given 2 X and 2 Y coordinates.
    /// </summary>
    public Range2D(long x1, long y1, long x2, long y2)
    {
        MinX = Math.Min(x1, x2);
        MinY = Math.Min(y1, y2);
        MaxX = Math.Max(x1, x2);
        MaxY = Math.Max(y1, y2);
    }
    
    /// <summary>
    /// Constructs a Range2D, each side edge.
    /// </summary>
    /// <remarks>
    /// Does not sort the parameters to make sure they are correct.
    /// </remarks>
    private Range2D(long minX, long minY, long maxX, long maxY, bool _)
    {
        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
    }
    
    /// <summary>
    /// Constructs a Range2D given a two corner coordinates.
    /// </summary>
    public Range2D(Vec2<long> a, Vec2<long> b)
    {
        MinX = Math.Min(a.X, b.X);
        MinY = Math.Min(a.Y, b.Y);
        MaxX = Math.Max(a.X, b.X);
        MaxY = Math.Max(a.Y, b.Y);
    }
    
    /// <summary>
    /// Constructs a Range2D given a Center coordinate and a width and height.
    /// </summary>
    public Range2D(Vec2<long> center, long width, long height)
    {
        var w = width / 2;
        var h = height / 2;
        
        MinX = center.X - w;
        MinY = center.Y - h;
        MaxX = center.X + w;
        MaxY = center.Y + h;
    }
    
    /// <summary>
    /// Constructs a Range2D given a Center coordinate and a width/height (or diameter).
    /// </summary>
    /// <remarks>The Range2D from this constructor will always be square.</remarks>
    public Range2D(Vec2<long> center, long size)
    {
        var s = size / 2;
        
        MinX = center.X - s;
        MinY = center.Y - s;
        MaxX = center.X + s;
        MaxY = center.Y + s;
    }
    
    
    /// <summary>
    /// Returns true if any part of the supplied Range2D overlaps with this Range2D
    /// </summary>
    /// <param name="range">the supplied Range2D</param>
    public bool Overlaps(Range2D range)
    {
        return MinX < range.MaxX && MaxX > range.MinX && MaxY > range.MinY && MinY < range.MaxY;
    }
    
    /// <summary>
    /// Returns the overlap of this Range2D and the supplied Range2D as a new Range2D
    /// </summary>
    /// <param name="range">the supplied Range2D</param>
    public Range2D Overlap(Range2D range)
    {
        if (!Overlaps(range))
            return new Range2D(0, 0, 0, 0);
        
        var minX = Math.Max(MinX, range.MinX);
        var minY = Math.Max(MinY, range.MinY);
        var maxX = Math.Min(MaxX, range.MaxX);
        var maxY = Math.Min(MaxY, range.MaxY);
        
        return new Range2D(minX, minY, maxX, maxY, true);
    }
    
    /// <summary>
    /// Returns true if this Range2D fully contains the supplied Range2D
    /// </summary>
    /// <param name="range">the supplied Range2D</param>
    public bool Contains(Range2D range)
    {
        var x1 = MinX <= range.MinX;
        var y1 = MinY <= range.MinY;
        var x2 = MaxX >= range.MaxX;
        var y2 = MaxY >= range.MaxY;
        
        
        // true if the coordinates of this range are all further from (0, 0) than the coordinates of the supplied range
        return x1 && y1 && x2 && y2;
    }
    
    /// <summary>
    /// Returns true if the supplied point resides within the Range2D
    /// </summary>
    /// <param name="point">the point to check</param>
    public bool Contains(Vec2<long> point)
    {
        return point.X >= MinX && point.X <= MaxX && point.Y >= MinY && point.Y <= MaxY;
    }
    
    /// <summary>
    /// Returns a string representing this Range2D, in the format (bottom left coord)..(top right coord).
    /// </summary>
    public override string ToString()
    {
        return $"{MinXMinY}..{MaxXMaxY}";
    }
    
    public static bool operator ==(Range2D r1, Range2D r2)
    {
        return r1.MinX == r2.MinX && r1.MinY == r2.MinY && r1.MaxX == r2.MaxX && r1.MaxY == r2.MaxY;
    }
    
    public static bool operator !=(Range2D r1, Range2D r2)
    {
        return r1.MinX != r2.MinX || r1.MinY != r2.MinY || r1.MaxX != r2.MaxX || r1.MaxY != r2.MaxY;
    }

    public static Range2D operator +(Range2D a, Range2D b)
    {
        return new Range2D(a.MinX + b.MinX, a.MinY + b.MinY, a.MaxX + b.MaxY, a.MaxY + b.MaxY);
    }
    
    public static Range2D operator +(Range2D a, Vec2<long> b)
    {
        return new Range2D(a.MinX + b.X, a.MinY + b.Y, a.MaxX + b.X, a.MaxY + b.Y);
    }
    
    public static Range2D operator -(Range2D a, Range2D b)
    {
        return new Range2D(a.MinX - b.MinX, a.MinY - b.MinY, a.MaxX - b.MaxY, a.MaxY - b.MaxY);
    }
    
    public static Range2D operator -(Range2D a, Vec2<long> b)
    {
        return new Range2D(a.MinX - b.X, a.MinY - b.Y, a.MaxX - b.X, a.MaxY - b.Y);
    }
    
    
    private bool Equals(Range2D other)
    {
        return MinX == other.MinX && MinY == other.MinY && MaxX == other.MaxX && MaxY == other.MaxY;
    }

    public override bool Equals(object? obj)
    {
        return obj is Range2D other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(MinX, MinY, MaxX, MaxY);
    }
}