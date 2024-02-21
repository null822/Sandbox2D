#nullable enable
using System;
using System.Collections.Generic;

namespace Sandbox2D.Maths;

public readonly struct Range2D
{
    /// <summary>
    /// Left coordinate.
    /// </summary>
    public readonly long MinX;
    /// <summary>
    /// Bottom coordinate.
    /// </summary>
    public readonly long MinY;
    /// <summary>
    /// Right coordinate.
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
    
    public ulong Width => (ulong)(MaxX - MinX);
    public ulong Height => (ulong)(MaxY - MinY);
    
    public Vec2<ulong> Dimensions => new(Width, Height);

    /// <summary>
    /// The area of the Range2D, in units^2.
    /// </summary>
    public UInt128 Area => (UInt128)Width * Height;
    
    /// <summary>
    /// The point that resides in the center of the range, returned as an integer.
    /// </summary>
    public Vec2<long> Center => (MinX / 2 + MaxX / 2, MinY / 2 + MaxY / 2);
    
    /// <summary>
    /// The point that resides in the center of the range, returned as a floating-point integer.
    /// </summary>
    public Vec2<decimal> CenterF => (MinX / (decimal)2 + MaxX / (decimal)2, MinY / (decimal)2 + MaxY / (decimal)2);
    
    /// <summary>
    /// Returns the maximum distance that any point within this Range2D can be from the the center.
    /// </summary>
    public ulong MaxExtension => Math.Max(Width, Height) / 2;
    
    
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
    public Range2D(Vec2<long> center, ulong width, ulong height)
    {
        var w = (long)(width / 2);
        var h = (long)(height / 2);
        
        MinX = center.X - w;
        MinY = center.Y - h;
        MaxX = center.X + w;
        MaxY = center.Y + h;
    }
    
    /// <summary>
    /// Constructs a Range2D given a Center coordinate and a width/height (or diameter).
    /// </summary>
    /// <remarks>The Range2D from this constructor will always be square.</remarks>
    public Range2D(Vec2<long> center, ulong size)
    {
        var s = (long)(size / 2);
        
        MinX = center.X - s;
        MinY = center.Y - s;
        MaxX = center.X + s;
        MaxY = center.Y + s;
    }
    
    /// <summary>
    /// Returns true if this Range2D fully contains the supplied Range2D
    /// </summary>
    /// <param name="range">the supplied Range2D</param>
    public bool Contains(Range2D range)
    {
        return Contains(range.MinXMinY) && Contains(range.MaxXMaxY);
    }
    
    /// <summary>
    /// Returns true if the supplied point resides within the Range2D
    /// </summary>
    /// <param name="point">the point to check</param>
    public bool Contains(Vec2<long> point)
    {
        var xContain = (point.X >= MinX && point.X <= MaxX) || (MinX == long.MinValue || MaxX == long.MaxValue);
        var yContain = (point.Y >= MinY && point.Y <= MaxY) || (MinY == long.MinValue || MaxY == long.MaxValue);
        
        return xContain && yContain;
    }
    
    
    /// <summary>
    /// Returns true if any part of the supplied Range2D overlaps with this Range2D
    /// </summary>
    /// <param name="range">the supplied Range2D</param>
    public bool Overlaps(Range2D range)
    {
        var xOverlap = (MinX < range.MaxX && MaxX > range.MinX) || (MinX == long.MinValue || MaxX == long.MaxValue);
        var yOverlap = (MaxY > range.MinY && MinY < range.MaxY) || (MinY == long.MinValue || MaxY == long.MaxValue);
        
        return xOverlap && yOverlap;
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
        
        return new Range2D(minX, minY, maxX, maxY);
    }

    public Range2D[] SplitIntoQuarters()
    {
        var halfSize = new Vec2<ulong>(Width / 2, Height / 2);
        var quartSize = new Vec2<long>((long)Width / 4, (long)Height / 4);
        
        return
        [
            new Range2D(Center + quartSize * (-1,  1), halfSize.X, halfSize.Y),
            new Range2D(Center + quartSize * ( 1,  1), halfSize.X, halfSize.Y),
            new Range2D(Center + quartSize * (-1, -1), halfSize.X, halfSize.Y),
            new Range2D(Center + quartSize * ( 1, -1), halfSize.X, halfSize.Y)
        ];
    }


    public Range2D[] SplitIntoSquares()
    {
        var squares = new List<Range2D>();
        
        // set the remainder to ourselves
        var remainder = this;
        
        do
        {
            var w = remainder.Width;
            var h = remainder.Height;
            
            // if the remainder is a perfect square, add it to the squares and return
            if (w == h)
            {
                squares.Add(remainder);
                return squares.ToArray();
            }
            
            var size = Math.Min(w, h);
            var center = BottomLeft + new Vec2<long>((long)(size / 2));
            
            var square = new Range2D(center, size);
            
            squares.Add(square);
            
            remainder = w > h ?
                new Range2D(MinX + (long)size, MinY, MaxX, MaxY) :
                new Range2D(MinX, MinY + (long)size, MaxX, MaxY);
            
        } while (!(remainder.Width == 0 || remainder.Height == 0));
        
        return squares.ToArray();

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