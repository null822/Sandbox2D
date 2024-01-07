using System;

namespace Sandbox2D.Maths;

public readonly struct Range2D
{
    /// <summary>
    /// Minimum X, or Left
    /// </summary>
    public readonly long MinX;
    /// <summary>
    /// Minimum Y, or Bottom
    /// </summary>
    public readonly long MinY;
    /// <summary>
    /// Maximum X, or Right
    /// </summary>
    public readonly long MaxX;
    /// <summary>
    /// Maximum Y, to Top
    /// </summary>
    public readonly long MaxY;

    public Range2D(long minX, long minY, long maxX, long maxY)
    {
        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
    }
    
    public Range2D(Vec2Long tl, Vec2Long br)
    {
        MinX = tl.X;
        MinY = tl.Y;
        MaxX = br.X;
        MaxY = br.Y;
    }
    
    /// <summary>
    /// Returns true if any part of the supplied range overlaps with this range
    /// </summary>
    /// <param name="range">the supplied range</param>
    /// <returns></returns>
    public bool Overlaps(Range2D range)
    {
        return MinX < range.MaxX && MaxX > range.MinX && MaxY > range.MinY && MinY < range.MaxY;
    }
    
    /// <summary>
    /// Returns the overlap of this range and the supplied range
    /// </summary>
    /// <param name="range">the supplied range</param>
    /// <returns></returns>
    public Range2D Overlap(Range2D range)
    {
        var x1 = Math.Max(MinX, range.MinX);
        var y1 = Math.Max(MinY, range.MinY);
        var x2 = Math.Min(MaxX, range.MaxX);
        var y2 = Math.Min(MaxY, range.MaxY);
        
        return new Range2D(x1, y1, x2, y2);
    }
    
    /// <summary>
    /// Returns true if this range fully contains the supplied range
    /// </summary>
    /// <param name="range">the supplied range</param>
    /// <returns></returns>
    public bool Contains(Range2D range)
    {
        // true if the overlap of this range and the supplied range equals the supplied range
        return Overlap(range) == range;
    }

    public long GetArea()
    {
        return (MaxX - MinX) * (MaxY - MinY);
    }

    public long GetWidth()
    {
        return MaxX - MinX;
    }

    public long GetHeight()
    {
        return MaxY - MinY;
    }

    /// <summary>
    /// Returns the width and height of the Range2D
    /// </summary>
    public Vec2Long GetSize()
    {
        return new Vec2Long(GetWidth(), GetHeight());
    }

    /// <summary>
    /// Returns the position of the Range2D (min X and min Y coords)
    /// </summary>
    public Vec2Long GetPos()
    {
        return new Vec2Long(MinX, MinY);
    }
    
    /// <summary>
    /// Returns a string representing this range
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return Constants.Range2DStringFormat ? $"({MinX}, {MinY})..({MaxX}, {MaxY})" : $"({MinX}..{MaxX}, {MinY}..{MaxY})";
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