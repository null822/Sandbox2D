#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sandbox2D.Graphics.Renderables;
using Sandbox2D.Maths.Quadtree;
using Sandbox2D.World;

namespace Sandbox2D.Maths.QuadTree;

internal class QuadTreeLeaf<T, TValue> : QuadTreePart<T, TValue> where T : class where TValue : class, IQuadTreeValue<T>
{
    /// <summary>
    /// The value enclosed in this Leaf.
    /// </summary>
    public IQuadTreeValue<T> Value { get; private set; }
    
    /// <summary>
    /// Constructs a QuadTreeLeaf, provided the defaultValue, a maxDepth, a range, and a value.
    /// </summary>
    internal QuadTreeLeaf(T defaultValue, byte maxDepth, Range2D range, T value) : base(defaultValue, maxDepth, range)
    {
        Value = TValue.New(value);
    }
    
    /// <summary>
    /// Constructs a QuadTreeLeaf, provided the defaultValue, a range, and a value.
    /// </summary>
    /// <remarks>
    /// This constructor is slower than the QuadTreeLeaf(T, byte, Range2D, T) constructor,
    /// due to having to calculate the maxDepth from the range.
    /// </remarks>
    internal QuadTreeLeaf(T defaultValue, Range2D range, T value) : base(defaultValue, range)
    {
        Value = TValue.New(value);
    }

    internal override bool Set(Vec2<long> targetPos, T? value)
    {
        if (Equals(value, null))
            return false;
        
        Value = TValue.New(value);
        return true;
    }
    
    internal override bool Set(Range2D targetRange, T? value)
    {
        if (Equals(value, null))
            return false;

        if (targetRange.Contains(AbsoluteRange))
        {
            Value = TValue.New(value);
            return true;
        }
        
        return false;
    }

    internal override T Get(Vec2<long> targetPos)
    {
        return Value.Get();
    }
    
    public override bool Invoke(Range2D range, Func<T, Vec2<long>, bool> run, ResultComparison rc, bool excludeDefault = false)
    {
        if (excludeDefault && Value.Equals(TValue.New(DefaultValue))) return false;
        
        // get the Range2D of this QuadTreeLeaf
        var blockRange = AbsoluteRange;
        
        // if the supplied range overlaps with blockRange,
        if (range.Overlaps(blockRange))
        {
            // get the overlap rectangle of this QuadTreeLeaf, and the supplied range
            var overlap = range.Overlap(blockRange);
            
            // invoke the run Func for every discrete position in the overlap

            var result = rc.StartingValue;
            
            for (var x = overlap.MinX; x < overlap.MaxX; x++)
            {
                for (var y = overlap.MinY; y < overlap.MaxY; y++)
                {
                    result = rc.Comparator.Invoke(
                        result,
                        run.Invoke(Value.Get(), new Vec2<long>(x, y))
                        );
                }
            }
            
            return result;
        }
        
        return false;
    }
    
    public override bool InvokeLeaf(Range2D range, Func<T, Range2D, Vec2<byte>, bool> run, ResultComparison rc, bool excludeDefault = false, Vec2<byte>? index = null)
    {
        if (excludeDefault && Value.Equals(TValue.New(DefaultValue))) return false;
        
        // get the Range2D of this QuadTreeLeaf
        var blockRange = AbsoluteRange;
        
        // if the supplied range does not overlap with blockRange, return false
        if (!range.Overlaps(blockRange)) return false;
        
        // otherwise, get the overlap rectangle of this QuadTreeLeaf and the supplied range
        var overlap = range.Overlap(blockRange);
        
        // invoke the run Func for the overlap area, only once.
        return run.Invoke(Value.Get(), overlap, index ?? (0, 0));

    }
    
    
    public override Range2D SerializeToLinear(ref List<QuadTreeStruct> lqt, Range2D screenRange,
        Range2D renderRange = default)
    {
        // add this QuadTreeLeaf to the lqt
        AddToLinearQuadTree(ref lqt, renderRange, Value);
        
        // return the renderRange
        return renderRange;
    }

    public override StringBuilder GetSvgMap(StringBuilder? nullableSvgString = null)
    {
        const double scale = Constants.QuadTreeSvgScale;

        var halfSize = (long)(Size/2);
        
        var svgString = nullableSvgString ?? new StringBuilder(
            $"<svg " +
            $"viewBox=\"" +
            $"{-halfSize * scale} " +
            $"{-halfSize * scale} " +
            $"{ halfSize * scale} " +
            $"{ halfSize * scale}" +
            $"\">" +
            $"<svg/>"
        );
        
        
        var fillColor = "#00ff00";

        if (Size == 1) fillColor = "#ffff00";
        if (Value.Equals(TValue.New(DefaultValue))) fillColor = "#ff0000;fill-opacity:0.1";

        var rect = $"<rect style=\"fill:{fillColor};stroke:#000000;stroke-width:{Math.Min(Size / 64d, 1)}\" " +
                   $"width=\"{Size * scale}\" height=\"{Size * scale}\" " +
                   $"x=\"{AbsoluteRange.MinX * scale}\" y=\"{AbsoluteRange.MinY * scale}\"/>";
        
        svgString.Insert(svgString.Length-6, rect);

        return svgString;
    }
    
    internal override void SerializeQuadTree(Stream tree, Stream data, Vec2<byte> index)
    {
        // if the value is default, we don't need to include it in the serialization
        if (Value.Equals(TValue.New(DefaultValue)))
            return;
        
        // write the identifier for a QuadTreeLeaf to the tree stream
        tree.Write(new byte[] { 2 });
        
        // write the default data for a QuadTreePart to the tree stream
        base.SerializeQuadTree(tree, data, index);
        
        // write, and get the pointer of, the value in the data stream
        var pointer = (uint)data.Position / TValue.SerializeLength;
        data.Write(Value.Serialize());
        
        // write the pointer of the value to the tree stream
        tree.Write(BitConverter.GetBytes(pointer));
    }
    
    public override T AverageValue()
    {
        return Value.Get();
    }
}
