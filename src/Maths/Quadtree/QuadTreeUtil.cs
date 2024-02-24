using System;
using System.IO;
using Sandbox2D.Exceptions;

namespace Sandbox2D.Maths.Quadtree;

internal static class QuadTreeUtil
{
    internal static Vec2<byte> GetIndex2DFromPos(Vec2<long> localPos)
    {
        var index = new Vec2<byte>(// +   - //
            (byte)(localPos.X > 0 ? 1 : 0),
            (byte)(localPos.Y > 0 ? 0 : 1)
        );
        return index;
    }
    
    internal static void CheckIndex1D(byte index)
    {
        if (index > 3)
            throw new InvalidIndexException(Index1DTo2D(index));
    }
    
    internal static byte Index2DTo1D(Vec2<byte> index2D)
    {
        return (byte)(index2D.X + index2D.Y * 2);
    }
    
    internal static byte Index2DTo1D(byte x, byte y)
    {
        return (byte)(x + y * 2);
    }
    
    internal static Vec2<byte> Index1DTo2D(byte index)
    {
        return new Vec2<byte>((byte)(index % 2), (byte)Math.Floor(index / 2f));
    }
    
    /// <summary>
    /// Reads a stream, advancing the position in the stream by the number of bytes read
    /// </summary>
    /// <param name="stream">the stream to read from</param>
    /// <param name="length">the amount of bytes to read</param>
    /// <param name="name">optional, the name of the object being read</param>
    /// <param name="position">optional, the position within the stream to read from</param>
    /// <returns>a span containing the read bytes</returns>
    internal static ReadOnlySpan<byte> ReadStream(Stream stream, long length, string? name = null, long? position = null)
    {
        // change read position if supplied
        if (position != null)
            stream.Position = (long)position;

        // store the starting position for use in case of an error
        var startingPosition = stream.Position;
        
        // create the output span
        var bytes = new Span<byte>(new byte[length]);
        
        // read from the stream
        var count = stream.Read(bytes);
        
        // error if the stream if the amount of bytes read does not equal the amount of bytes to read in total
        if (count != length)
            Util.Error($"Failed to correctly read {name ?? "stream"}" +
                       $" at position {startingPosition}..{startingPosition + length}" +
                       $" ({count}/{length} bytes read)");
        
        // return the result
        return bytes;
    }
    
}

public static class ResultComparisons
{
    public static readonly ResultComparison Or = new ResultComparisonOr();
    public static readonly ResultComparison And = new ResultComparisonAnd();
}

public abstract class ResultComparison
{
    public abstract Func<bool, bool, bool> Comparator { get; }
    public abstract bool StartingValue { get; }

}

public class ResultComparisonOr : ResultComparison
{
    public override Func<bool, bool, bool> Comparator => (a, b) => a || b;
    public override bool StartingValue => false;
}

public class ResultComparisonAnd : ResultComparison
{
    public override Func<bool, bool, bool> Comparator => (a, b) => a && b;
    public override bool StartingValue => true;
}
