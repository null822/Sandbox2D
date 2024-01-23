using System;

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
    
    public Vec2<long> BottomLeft => new(MinX, MinY);
    public Vec2<long> TopLeft => new(MinX, MaxY);
    public Vec2<long> BottomRight => new(MaxX, MinY);
    public Vec2<long> TopRight => new(MaxX, MaxY);

    public long Width => MaxX - MinX;
    public long Height => MaxY - MinY;

    /// <summary>
    /// The area of the Range2D, in units^2.
    /// </summary>
    public long Area => Width * Height;
    
    /// <summary>
    /// The point that resides in the center of the range.
    /// </summary>
    public Vec2<double> Center => ((MinX + MaxX) / 2d, (MinY + MaxY) / 2d);


    public Range2D(long x1, long y1, long x2, long y2)
    {
        MinX = Math.Min(x1, x2);
        MinY = Math.Min(y1, y2);
        MaxX = Math.Max(x1, x2);
        MaxY = Math.Max(y1, y2);
    }
    
    public Range2D((long X, long Y) min, (long X, long Y) max)
    {
        MinX = min.X;
        MinY = min.Y;
        MaxX = max.X;
        MaxY = max.Y;
    }
    
    public Range2D(Vec2<long> tl, Vec2<long> br)
    {
        MinX = tl.X;
        MinY = tl.Y;
        MaxX = br.X;
        MaxY = br.Y;
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
        var x1 = Math.Max(MinX, range.MinX);
        var y1 = Math.Max(MinY, range.MinY);
        var x2 = Math.Min(MaxX, range.MaxX);
        var y2 = Math.Min(MaxY, range.MaxY);
        
        return new Range2D(x1, y1, x2, y2);
    }
    
    /// <summary>
    /// Returns true if this Range2D fully contains the supplied Range2D
    /// </summary>
    /// <param name="range">the supplied Range2D</param>
    public bool Contains(Range2D range)
    {
        // true if the overlap of this range and the supplied range equals the supplied range
        return Overlap(range) == range;
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
    /// Returns a string representing this Range2D, in the format (top left coord)..(bottom right coord).
    /// </summary>
    public override string ToString()
    {
        return $"{TopLeft}..{BottomRight}";
    }

    public static bool operator ==(Range2D r1, Range2D r2)
    {
        return r1.MinX == r2.MinX && r1.MinY == r2.MinY && r1.MaxX == r2.MaxX && r1.MaxY == r2.MaxY;
    }
    
    public static bool operator !=(Range2D r1, Range2D r2)
    {
        return r1.MinX != r2.MinX || r1.MinY != r2.MinY || r1.MaxX != r2.MaxX || r1.MaxY != r2.MaxY;
    }

    
    private bool Equals(Range2D other)
    {
        return MinX == other.MinX && MinY == other.MinY && MaxX == other.MaxX && MaxY == other.MaxY;
    }

    public override bool Equals(object obj)
    {
        return obj is Range2D other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(MinX, MinY, MaxX, MaxY);
    }
}