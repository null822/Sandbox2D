using System.Runtime.InteropServices;

namespace Math2D;

/// <summary>
/// Represents a 2-dimensional rectangle, with all corner coordinates being inclusive.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Range2Df : IEquatable<Range2Df>
{
    public static double Tolerance = 1e-15;
    
    /// <summary>
    /// Left coordinate.
    /// </summary>
    public readonly double MinX;
    /// <summary>
    /// Bottom coordinate.
    /// </summary>
    public readonly double MinY;
    /// <summary>
    /// Right coordinate.
    /// </summary>
    public readonly double MaxX;
    /// <summary>
    /// Top coordinate.
    /// </summary>
    public readonly double MaxY;
    
    /// <summary>
    /// The bottom left corner.
    /// </summary>
    public Vec2<double> Bl => MinXMinY;
    /// <summary>
    /// The top left corner.
    /// </summary>
    public Vec2<double> Tl => MinXMaxY;
    /// <summary>
    /// The bottom right corner.
    /// </summary>
    public Vec2<double> Br => MaxXMinY;
    /// <summary>
    /// The top right corner.
    /// </summary>
    public Vec2<double> Tr => MaxXMaxY;
    
    public Vec2<double> MinXMinY => new(MinX, MinY);
    public Vec2<double> MinXMaxY => new(MinX, MaxY);
    public Vec2<double> MaxXMinY => new(MaxX, MinY);
    public Vec2<double> MaxXMaxY => new(MaxX, MaxY);
    
    public double HalfWidth  => MaxX/2 - MinX/2 + 1;
    public double HalfHeight => MaxY/2 - MinY/2 + 1;
    
    public double Width  => MaxX - MinX;
    public double Height => MaxY - MinY;
    
    /// <summary>
    /// The area of the <see cref="Range2Df"/>, in units^2.
    /// </summary>
    public double Area => Width * Height;

    /// <summary>
    /// The dimensions (width, height) of this <see cref="Range2Df"/>
    /// </summary>
    public Vec2<double> Dimensions => new(Width, Height);
    
    /// <summary>
    /// The point that resides in the center of the range, returned as an integer.
    /// </summary>
    public Vec2<double> Center => (MinX / 2 + MaxX / 2, MinY / 2 + MaxY / 2);
    
    /// <summary>
    /// The point that resides in the center of the range, returned as a floating-point integer.
    /// </summary>
    public Vec2<double> CenterF => (MinX / 2 + MaxX / 2, MinY / 2 + MaxY / 2);
    
    /// <summary>
    /// Returns the maximum distance that any point on the X and Y axes within this <see cref="Range2Df"/> can be from the center.
    /// </summary>
    public double MaxExtension => double.Max(HalfWidth, HalfHeight);
    
    /// <summary>
    /// This <see cref="Range2Df"/>, centered at (0, 0).
    /// </summary>
    public Range2Df Normalized => new((0, 0), Width, Height);
    
    
    /// <summary>
    /// A copy of this <see cref="Range2Df"/> whith the X axis flipped/negated
    /// </summary>
    public Range2Df FlipX => new Range2Df(-MinX, MinY, -MaxX, MaxY);
    
    /// <summary>
    /// A copy of this <see cref="Range2Df"/> whith the Y axis flipped/negated
    /// </summary>
    public Range2Df FlipY => new Range2Df(MinX, -MinY, MaxX, -MaxY);

    
    /// <summary>
    /// Constructs a <see cref="Range2Df"/>, given 2 X and 2 Y coordinates.
    /// </summary>
    public Range2Df(double x1, double y1, double x2, double y2)
    {
        MinX = double.Min(x1, x2);
        MinY = double.Min(y1, y2);
        MaxX = double.Max(x1, x2);
        MaxY = double.Max(y1, y2);
    }
    
    /// <summary>
    /// Constructs a <see cref="Range2Df"/> given a two corner coordinates.
    /// </summary>
    public Range2Df(Vec2<double> a, Vec2<double> b)
    {
        MinX = double.Min(a.X, b.X);
        MinY = double.Min(a.Y, b.Y);
        MaxX = double.Max(a.X, b.X);
        MaxY = double.Max(a.Y, b.Y);
    }
    
    /// <summary>
    /// Constructs a <see cref="Range2Df"/> given a two corner coordinates.
    /// </summary>
    public Range2Df(Vec2<double> v)
    {
        MinX = v.X;
        MinY = v.Y;
        MaxX = v.X;
        MaxY = v.Y;
    }
    
    /// <summary>
    /// Constructs a <see cref="Range2Df"/> given a Center coordinate and a width and height.
    /// </summary>
    public Range2Df(Vec2<double> center, double width, double height)
    {
        double wMin;
        double wMax;
        
        if (width % 2 == 0)
        {
            wMin = wMax = width / 2d;
        }
        else
        {
            var s = width / 2d;

            wMin = double.Floor(s);
            wMax = double.Ceiling(s);
        }
        
        double hMin;
        double hMax;
        
        if (height % 2 == 0)
        {
            hMin = hMax = height / 2d;
        }
        else
        {
            var s = height / 2d;

            hMin = double.Floor(s);
            hMax = double.Ceiling(s);
        }
        
        MinX = center.X - wMin;
        MinY = center.Y - hMin;
        MaxX = center.X + wMax;
        MaxY = center.Y + hMax;
    }
    
    /// <summary>
    /// Constructs a <see cref="Range2Df"/> given a center coordinate and a width/height (or size).
    /// </summary>
    /// <remarks>The <see cref="Range2Df"/> from this constructor will always be square.</remarks>
    public Range2Df(Vec2<double> center, double size)
    {
        double sMin;
        double sMax;
        
        if (size % 2 == 0)
        {
            sMin = sMax = size / 2d;
        }
        else
        {
            var s = size / 2d;
            
            sMin = double.Floor(s);
            sMax = double.Ceiling(s);
        }
        
        MinX = center.X - sMin;
        MinY = center.Y - sMin;
        MaxX = center.X + sMax;
        MaxY = center.Y + sMax;
    }
    
    
    /// <summary>
    /// Returns true if this <see cref="Range2Df"/> fully contains the supplied <see cref="Range2Df"/>.
    /// </summary>
    /// <param name="range">the supplied <see cref="Range2Df"/></param>
    public bool Contains(Range2Df range)
    {
        return Contains(range.MinXMinY) && Contains(range.MaxXMaxY);
    }
    
    /// <summary>
    /// Returns true if the supplied point resides within the <see cref="Range2Df"/>
    /// </summary>
    /// <param name="point">the point to check</param>
    public bool Contains(Vec2<double> point)
    {
        return XContains(point.X) && YContains(point.Y);
    }

    /// <summary>
    /// Returns true if any part of the supplied <see cref="Range2Df"/> overlaps with this <see cref="Range2Df"/>.
    /// </summary>
    /// <param name="range">the supplied <see cref="Range2Df"/></param>
    public bool Overlaps(Range2Df range)
    {
        var xOverlap = MinX <= range.MaxX && MaxX >= range.MinX;
        var yOverlap = MinY <= range.MaxY && MaxY >= range.MinY;
        
        return xOverlap && yOverlap;
    }
    
    /// <summary>
    /// Returns the overlap of this <see cref="Range2Df"/> and the supplied <see cref="Range2Df"/> as a new <see cref="Range2Df"/>.
    /// </summary>
    /// <param name="range">the supplied <see cref="Range2Df"/></param>
    public Range2Df Overlap(Range2Df range)
    {
        if (!Overlaps(range))
            return new Range2Df(0, 0, 0, 0);
        
        var minX = double.Max(MinX, range.MinX);
        var minY = double.Max(MinY, range.MinY);
        var maxX = double.Min(MaxX, range.MaxX);
        var maxY = double.Min(MaxY, range.MaxY);
        
        return new Range2Df(minX, minY, maxX, maxY);
    }
    
    
    /// <summary>
    /// Determines whether the supplied X coordinate is contained within the X coordinates of this <see cref="Range2Df"/>
    /// </summary>
    /// <param name="x">the X coordinate</param>
    public bool XContains(double x)
    {
        return x >= MinX && x <= MaxX;
    }
    
    /// <summary>
    /// Determines whether the supplied Y coordinate is contained within the Y coordinates of this <see cref="Range2Df"/>
    /// </summary>
    /// <param name="y">the Y coordinate</param>
    public bool YContains(double y)
    {
        return y >= MinY && y <= MaxY;
    }
    
    public Range2Df[] SplitIntoQuarters()
    {
        var halfSize = new Vec2<double>(Width / 2, Height / 2);
        var quartSize = new Vec2<double>(Width / 4, Height / 4);
        
        return
        [
            new Range2Df(Center + quartSize * (-1,  1), halfSize.X, halfSize.Y),
            new Range2Df(Center + quartSize * ( 1,  1), halfSize.X, halfSize.Y),
            new Range2Df(Center + quartSize * (-1, -1), halfSize.X, halfSize.Y),
            new Range2Df(Center + quartSize * ( 1, -1), halfSize.X, halfSize.Y)
        ];
    }


    public Range2Df[] SplitIntoSquares()
    {
        var squares = new List<Range2Df>();
        
        // set the remainder to ourselves
        var remainder = this;
        
        do
        {
            var w = remainder.Width;
            var h = remainder.Height;
            
            // if the remainder is a perfect square, add it to the squares and return
            if (Math.Abs(w - h) < Tolerance)
            {
                squares.Add(remainder);
                break;
            }
            
            var size = Math.Min(w, h);
            var center = Bl + new Vec2<double>(size / 2);
            
            var square = new Range2Df(center, size);
            
            squares.Add(square);
            
            remainder = w > h ?
                new Range2Df(MinX + size, MinY, MaxX, MaxY) :
                new Range2Df(MinX, MinY + size, MaxX, MaxY);
            
        } while (!(remainder.Width == 0 || remainder.Height == 0));
        
        var squaresArray = squares.ToArray();
        squares.Clear();
        return squaresArray;
    }
    
    public Range2Df Scale(double scale)
    {
        return new Range2Df(Center, Width * scale, Height * scale);
    }
    
    /// <summary>
    /// Returns a string representing this <see cref="Range2Df"/>, in the format (bottom left coord)..(top right coord).
    /// </summary>
    public override string ToString()
    {
        return $"{MinXMinY}..{MaxXMaxY}";
    }
    
    public static bool operator ==(Range2Df r1, Range2Df r2)
    {
        return Math.Abs(r1.MinX - r2.MinX) < Tolerance &&
               Math.Abs(r1.MinY - r2.MinY) < Tolerance &&
               Math.Abs(r1.MaxX - r2.MaxX) < Tolerance &&
               Math.Abs(r1.MaxY - r2.MaxY) < Tolerance;
    }
    
    public static bool operator !=(Range2Df r1, Range2Df r2)
    {
        return Math.Abs(r1.MinX - r2.MinX) > Tolerance ||
               Math.Abs(r1.MinY - r2.MinY) > Tolerance ||
               Math.Abs(r1.MaxX - r2.MaxX) > Tolerance ||
               Math.Abs(r1.MaxY - r2.MaxY) > Tolerance;
    }
    
    public static Range2Df operator +(Range2Df a, Range2Df b)
    {
        return new Range2Df(a.MinX + b.MinX, a.MinY + b.MinY, a.MaxX + b.MaxY, a.MaxY + b.MaxY);
    }
    
    public static Range2Df operator +(Range2Df a, Vec2<double> b)
    {
        return new Range2Df(a.MinX + b.X, a.MinY + b.Y, a.MaxX + b.X, a.MaxY + b.Y);
    }
    
    public static Range2Df operator -(Range2Df a, Range2Df b)
    {
        return new Range2Df(a.MinX - b.MinX, a.MinY - b.MinY, a.MaxX - b.MaxY, a.MaxY - b.MaxY);
    }
    
    public static Range2Df operator -(Range2Df a, Vec2<double> b)
    {
        return new Range2Df(a.MinX - b.X, a.MinY - b.Y, a.MaxX - b.X, a.MaxY - b.Y);
    }
    
    public static Range2Df operator *(Range2Df range, double scale)
    {
        return new Range2Df(range.MinX * scale, range.MinY * scale, range.MaxX * scale, range.MaxY * scale);
    }
    
    public bool Equals(Range2Df other)
    {
        return Math.Abs(MinX - other.MinX) < Tolerance &&
               Math.Abs(MinY - other.MinY) < Tolerance &&
               Math.Abs(MaxX - other.MaxX) < Tolerance &&
               Math.Abs(MaxY - other.MaxY) < Tolerance;
    }
    
    public override bool Equals(object? obj)
    {
        return obj is Range2Df other && Equals(other);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(MinX, MinY, MaxX, MaxY);
    }
}