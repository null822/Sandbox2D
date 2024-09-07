using System.Runtime.InteropServices;

namespace Math2D;

/// <summary>
/// Represents a 2-dimensional rectangle, with all corner coordinates being inclusive.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Range2D : IEquatable<Range2D>
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
    
    /// <summary>
    /// The bottom left corner.
    /// </summary>
    public Vec2<long> Bl => MinXMinY;
    /// <summary>
    /// The top left corner.
    /// </summary>
    public Vec2<long> Tl => MinXMaxY;
    /// <summary>
    /// The bottom right corner.
    /// </summary>
    public Vec2<long> Br => MaxXMinY;
    /// <summary>
    /// The top right corner.
    /// </summary>
    public Vec2<long> Tr => MaxXMaxY;
    
    public Vec2<long> MinXMinY => new(MinX, MinY);
    public Vec2<long> MinXMaxY => new(MinX, MaxY);
    public Vec2<long> MaxXMinY => new(MaxX, MinY);
    public Vec2<long> MaxXMaxY => new(MaxX, MaxY);
    
    public ulong HalfWidth  => (ulong)(MaxX/2 - MinX/2) + 1;
    public ulong HalfHeight => (ulong)(MaxY/2 - MinY/2) + 1;
    
    public ulong Width  => (ulong)(MaxX - MinX) + 1;
    public ulong Height => (ulong)(MaxY - MinY) + 1;
    
    /// <summary>
    /// The area of the <see cref="Range2D"/>, in units^2.
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
    /// Returns the maximum distance that any point on the X and Y axes within this <see cref="Range2D"/> can be from the center.
    /// </summary>
    public ulong MaxExtension => ulong.Max(HalfWidth, HalfHeight);
    
    /// <summary>
    /// This <see cref="Range2D"/>, centered at (0, 0).
    /// </summary>
    public Range2D Normalized => new((0, 0), Width, Height);
    
    
    /// <summary>
    /// A copy of this <see cref="Range2D"/> whith the X axis flipped/negated
    /// </summary>
    public Range2D FlipX => new Range2D(-MinX, MinY, -MaxX, MaxY);
    
    /// <summary>
    /// A copy of this <see cref="Range2D"/> whith the Y axis flipped/negated
    /// </summary>
    public Range2D FlipY => new Range2D(MinX, -MinY, MaxX, -MaxY);

    
    /// <summary>
    /// Constructs a <see cref="Range2D"/>, given 2 X and 2 Y coordinates.
    /// </summary>
    public Range2D(long x1, long y1, long x2, long y2)
    {
        MinX = long.Min(x1, x2);
        MinY = long.Min(y1, y2);
        MaxX = long.Max(x1, x2);
        MaxY = long.Max(y1, y2);
    }
    
    /// <summary>
    /// Constructs a <see cref="Range2D"/> given a two corner coordinates.
    /// </summary>
    public Range2D(Vec2<long> a, Vec2<long> b)
    {
        MinX = long.Min(a.X, b.X);
        MinY = long.Min(a.Y, b.Y);
        MaxX = long.Max(a.X, b.X);
        MaxY = long.Max(a.Y, b.Y);
    }
    
    /// <summary>
    /// Constructs a <see cref="Range2D"/> given a two corner coordinates.
    /// </summary>
    public Range2D(Vec2<long> v)
    {
        MinX = v.X;
        MinY = v.Y;
        MaxX = v.X;
        MaxY = v.Y;
    }
    
    /// <summary>
    /// Constructs a <see cref="Range2D"/> given a Center coordinate and a width and height.
    /// </summary>
    public Range2D(Vec2<long> center, ulong width, ulong height)
    {
        long wMin;
        long wMax;
        
        if (width % 2 == 0)
        {
            wMin = wMax = (long)(width >> 1);
        }
        else
        {
            var s = width / 2m;

            wMin = (long)decimal.Floor(s);
            wMax = (long)decimal.Ceiling(s);
        }
        
        long hMin;
        long hMax;
        
        if (height % 2 == 0)
        {
            hMin = hMax = (long)(height >> 1);
        }
        else
        {
            var s = height / 2m;

            hMin = (long)decimal.Floor(s);
            hMax = (long)decimal.Ceiling(s);
        }
        
        MinX = center.X - wMin;
        MinY = center.Y - hMin;
        MaxX = center.X + wMax;
        MaxY = center.Y + hMax;
    }
    
    /// <summary>
    /// Constructs a <see cref="Range2D"/> given a center coordinate and a width/height (or size).
    /// </summary>
    /// <remarks>The <see cref="Range2D"/> from this constructor will always be square.</remarks>
    public Range2D(Vec2<long> center, ulong size)
    {
        long sMin;
        long sMax;
        
        if (size % 2 == 0)
        {
            sMin = sMax = (long)(size >> 1);
        }
        else
        {
            var s = size / 2m;

            sMin = (long)decimal.Floor(s);
            sMax = (long)decimal.Ceiling(s);
        }
        
        MinX = center.X - sMin;
        MinY = center.Y - sMin;
        MaxX = center.X + sMax;
        MaxY = center.Y + sMax;
    }
    
    
    /// <summary>
    /// Returns true if this <see cref="Range2D"/> fully contains the supplied <see cref="Range2D"/>.
    /// </summary>
    /// <param name="range">the supplied <see cref="Range2D"/></param>
    public bool Contains(Range2D range)
    {
        return Contains(range.MinXMinY) && Contains(range.MaxXMaxY);
    }
    
    /// <summary>
    /// Returns true if the supplied point resides within the <see cref="Range2D"/>
    /// </summary>
    /// <param name="point">the point to check</param>
    public bool Contains(Vec2<long> point)
    {
        return XContains(point.X) && YContains(point.Y);
    }

    /// <summary>
    /// Returns true if any part of the supplied <see cref="Range2D"/> overlaps with this <see cref="Range2D"/>.
    /// </summary>
    /// <param name="range">the supplied <see cref="Range2D"/></param>
    public bool Overlaps(Range2D range)
    {
        var xOverlap = MinX <= range.MaxX && MaxX >= range.MinX;
        var yOverlap = MinY <= range.MaxY && MaxY >= range.MinY;
        
        return xOverlap && yOverlap;
    }
    
    /// <summary>
    /// Returns the overlap of this <see cref="Range2D"/> and the supplied <see cref="Range2D"/> as a new <see cref="Range2D"/>.
    /// </summary>
    /// <param name="range">the supplied <see cref="Range2D"/></param>
    public Range2D Overlap(Range2D range)
    {
        if (!Overlaps(range))
            return new Range2D(0, 0, 0, 0);
        
        var minX = long.Max(MinX, range.MinX);
        var minY = long.Max(MinY, range.MinY);
        var maxX = long.Min(MaxX, range.MaxX);
        var maxY = long.Min(MaxY, range.MaxY);
        
        return new Range2D(minX, minY, maxX, maxY);
    }
    
    
    /// <summary>
    /// Determines whether the supplied X coordinate is contained within the X coordinates of this <see cref="Range2D"/>
    /// </summary>
    /// <param name="x">the X coordinate</param>
    public bool XContains(long x)
    {
        return x >= MinX && x <= MaxX;
    }
    
    /// <summary>
    /// Determines whether the supplied Y coordinate is contained within the Y coordinates of this <see cref="Range2D"/>
    /// </summary>
    /// <param name="y">the Y coordinate</param>
    public bool YContains(long y)
    {
        return y >= MinY && y <= MaxY;
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
                break;
            }
            
            var size = Math.Min(w, h);
            var center = Bl + new Vec2<long>((long)(size / 2));
            
            var square = new Range2D(center, size);
            
            squares.Add(square);
            
            remainder = w > h ?
                new Range2D(MinX + (long)size, MinY, MaxX, MaxY) :
                new Range2D(MinX, MinY + (long)size, MaxX, MaxY);
            
        } while (!(remainder.Width == 0 || remainder.Height == 0));
        
        var squaresArray = squares.ToArray();
        squares.Clear();
        return squaresArray;
    }
    
    public Range2D Scale(decimal scale)
    {
        return new Range2D(Center, (ulong)(Width * scale), (ulong)(Height * scale));
    }
    
    /// <summary>
    /// Returns a string representing this <see cref="Range2D"/>, in the format (bottom left coord)..(top right coord).
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
    
    public static Range2D operator *(Range2D range, decimal scale)
    {
        return new Range2D((long)(range.MinX * scale), (long)(range.MinY * scale), (long)(range.MaxX * scale), (long)(range.MaxY * scale));
    }
    
    public bool Equals(Range2D other)
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