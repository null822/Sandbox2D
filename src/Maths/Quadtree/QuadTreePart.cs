#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sandbox2D.Graphics.Renderables;
using Sandbox2D.Maths.Quadtree;
using static Sandbox2D.Maths.Quadtree.QuadTreeUtil;

namespace Sandbox2D.Maths.QuadTree;

internal abstract class QuadTreePart<T, TValue> where T : class where TValue : class, IQuadTreeValue<T>
{
   
    /// <summary>
    /// The range this QuadTreePart covers, relative to the center of the entire tree.
    /// </summary>
    internal readonly Range2D AbsoluteRange;

    /// <summary>
    /// The range this QuadTreePart covers, relative to the center of it.
    /// </summary>
    internal Range2D RelativeRange => new Range2D((0, 0), Size);
    
    /// <summary>
    /// The total width/height of the QuadTreePart.
    /// </summary>
    internal readonly ulong Size;
    
    /// <summary>
    /// The maximum depth of parts within this QuadTreePart.
    /// </summary>
    internal readonly byte MaxDepth;
    
    /// <summary>
    /// The depth within the entire QuadTree.
    /// </summary>
    internal byte Depth => (byte)(Constants.WorldDepth - MaxDepth);
    
    /// <summary>
    /// The default value of the QuadTree (everything is default by default)
    /// </summary>
    protected readonly T DefaultValue;
    
    protected QuadTreePart(T defaultValue, byte maxDepth, Range2D absoluteRange)
    {
        DefaultValue = defaultValue;
        Size = absoluteRange.Width;
        MaxDepth = maxDepth;
        
        AbsoluteRange = absoluteRange;
    }
    
    protected QuadTreePart(T defaultValue, Range2D absoluteRange)
    {
        DefaultValue = defaultValue;
        Size = absoluteRange.Width;

        byte msb = 0;
        
        while (Size >> msb != 0) {
            msb++;
        }
        
        MaxDepth = msb;

        AbsoluteRange = absoluteRange;
    }
    
    /// <summary>
    /// Constructs a QuadTreePart from only a defaultValue and a maxDepth
    /// </summary>
    /// <remarks>
    /// This constructor should only be used when creating an entirely new QuadTree
    /// </remarks>
    protected QuadTreePart(T defaultValue, byte maxDepth)
    {
        DefaultValue = defaultValue;
        Size = 0x1ul << maxDepth;
        MaxDepth = maxDepth;
        
        AbsoluteRange = new Range2D((0, 0), Size);
    }
    
    /// <summary>
    /// Sets a single value in the QuadTree
    /// </summary>
    /// <param name="targetPos">the position of the value to set, relative to the QuadTree called in</param>
    /// <param name="value">the value to set</param>
    /// <remarks>Not optimized for modifying large areas of the quadtree</remarks>
    /// <returns>a bool describing if the QuadTree was changed (ignoring compression)</returns>
    internal abstract bool Set(Vec2<long> targetPos, T value);

    /// <summary>
    /// Sets an area of values to one single value in the QuadTree
    /// </summary>
    /// <param name="targetRange">the area of values to set, relative to the QuadTree called in</param>
    /// <param name="value">the value to set</param>
    /// <remarks>Optimized for setting a large area to one value</remarks>
    /// <returns>a bool describing if the QuadTree was changed (ignoring compression)</returns>
    internal abstract bool Set(Range2D targetRange, T value);

    /// <summary>
    /// Gets a value from the QuadTree
    /// </summary>
    /// <param name="targetPos">the position to get the value from</param>
    /// <returns>the value at that position</returns>
    internal abstract T Get(Vec2<long> targetPos);
    
    /// <summary>
    /// Runs the specified lambda for each element residing in the supplied range.
    /// </summary>
    /// <param name="range">range of positions of elements to run the lambda for</param>
    /// <param name="run">the lambda to run at each element</param>
    /// <param name="rc">a ResultComparison to compare the results</param>
    /// <param name="excludeDefault">whether to exclude all elements with the default value</param>
    /// <remarks>Ignores the nature of a quadtree and runs for each element individually. Use InvokeLeaf if this is not necessary.</remarks>
    /// <returns>the result of comparing all of the results of the run lambdas</returns>
    public abstract bool Invoke(Range2D range, Func<T, Vec2<long>, bool> run,
        ResultComparison rc, bool excludeDefault = false);

    /// <summary>
    /// Runs the specified lambda for each element residing in the supplied range, running only once for each QuadTreeLeaf
    /// </summary>
    /// <param name="range">range of positions of elements to run the lambda for</param>
    /// <param name="run">the lambda to run at each element</param>
    /// <param name="rc">a ResultComparison to compare the results</param>
    /// <param name="excludeDefault">whether to exclude all elements with the default value</param>
    /// <param name="index">the index of the QuadTreePart within the parent QuadTree</param>
    /// <returns>the result of comparing all of the results of the run lambdas</returns>
    public abstract bool InvokeLeaf(Range2D range, Func<T, Range2D, Vec2<byte>, bool> run,
        ResultComparison rc, bool excludeDefault = false, Vec2<byte>? index = null);

    /// <summary>
    /// Creates an SVG string representing the current structure of the entire QuadTree
    /// </summary>
    /// <param name="nullableSvgString">optional parameter, used internally to pass the SVG recursively. Will likely break if changed.</param>
    /// <returns>the contents of an SVG file, ready to be saved in a .svg file</returns>
    public abstract StringBuilder GetSvgMap(StringBuilder? nullableSvgString = null);
    
    /// <summary>
    /// Serializes the QuadTree into a Linear QuadTree.
    /// <param name="lqt">a reference to a list in which the lqt should be added to</param>
    /// <param name="screenRange">the currently visible portion of the world, relative to the world's center</param>
    /// <param name="renderRange">the (absolute) range of the resulting Linear QuadTree. Used internally</param>
    /// </summary>
    /// <returns>the absolute range of the resulting Linear QuadTree</returns>
    public abstract Range2D SerializeToLinear(ref List<QuadTreeStruct> lqt, Range2D screenRange,
        Range2D renderRange = default);
    
    /// <summary>
    /// Returns the most common value stored within the QuadTreePart.
    /// </summary>
    /// <returns>the absolute range of the resulting Linear QuadTree</returns>
    public abstract T AverageValue();
    
    
    /// <summary>
    /// Deserializes a serialized QuadTreePart. See:
    /// <code>src/QuadTree/QuadTree-Format.md</code>
    /// </summary>
    /// <param name="tree">a stream containing the tree section</param>
    /// <param name="data">a stream containing the data section</param>
    internal virtual void DeserializeQuadTree(Stream tree, Stream data)
    {
        
    }

    /// <summary>
    /// Deserializes a serialized QuadTreePart. See:
    /// <code>src/QuadTree-Format.md</code>
    /// </summary>
    /// <param name="tree">a stream containing the tree section</param>
    /// <param name="data">a stream containing the data section</param>
    /// <param name="index">the index within _subParts of the element to serialize</param>
    internal virtual void SerializeQuadTree(Stream tree, Stream data, Vec2<byte> index)
    {
        // write the index to the tree
        tree.Write(new []{Index2DTo1D(index)});
    }

    protected void AddToLinearQuadTree(ref List<QuadTreeStruct> lqt, Range2D renderRange, IQuadTreeValue<T> value)
    {
        // if this QuadTreePart is not in the renderRange, don't try to add it to the lqt
        if (!AbsoluteRange.Overlaps(renderRange))
            return;
        
        // calculate the depth within the lqt of the new QuadTreeStruct
        var depth = (byte)Math.Clamp(Depth - (Constants.WorldDepth - Constants.RenderDepth), 1, Constants.RenderDepth);
        
        // clamp the AbsoluteRange to be within the renderRange
        var partRange = renderRange.Overlap(AbsoluteRange);
        
        // if this single QuadTreeStruct covers the entire lqt, split it into quarters
        // otherwise, split it into as many squares as needed
        var parts = partRange == renderRange ? partRange.SplitIntoQuarters() : partRange.SplitIntoSquares();
        
        // add each square to the lqt
        // note that all of the squares will be of the same size due to the nature of the dimensions of `partRange`
        foreach (var part in parts)
        {
            AddToLinearQuadTree(ref lqt, renderRange, part, depth, value);
        }
        
    }

    private static void AddToLinearQuadTree(ref List<QuadTreeStruct> lqt, Range2D renderRange, Range2D partRange, byte depth, IQuadTreeValue<T> value)
    {
        // calculate the position of the top left corner of this QuadTreeLeaf, relative to the top left of the renderRange,
        // and flip the Y axis
        var relPosTl = (partRange.TopLeft - renderRange.TopLeft) * (1, -1);
        
        // scale the position to always fit into the lqt (required for extremely large-spanning lqts)
        var renderScale = (float)renderRange.Width / (0x1 << Constants.RenderDepth);
        var renderPos = (Vec2<uint>)((Vec2<decimal>)relPosTl / (decimal)renderScale);
        
        // calculate the code
        var code = Util.Interleave(renderPos, depth);
        
        // assemble a QuadTreeStruct with the code, depth, and id, and add it to the lqt
        lqt.Add(new QuadTreeStruct(code, depth, value.LinearSerialize()));
    }
    
}
