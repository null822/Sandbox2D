#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sandbox2D.Exceptions;
using Sandbox2D.Graphics.Renderables;
using Sandbox2D.Maths.Quadtree;
using static Sandbox2D.Maths.QuadTree.QuadTreeUtil;

namespace Sandbox2D.Maths.QuadTree;

internal abstract class QuadTreePart<T> where T : class, IQuadTreeValue<T>
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
    internal readonly long Size;
    
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
        Size = (long)0x1 << maxDepth;
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
    /// Serializes the QuadTree into a Linear QuadTree
    /// <param name="lqt">a reference to a list in which the lqt should be added to</param>
    /// <param name="screenRange">the currently visible portion of the world, relative to the world's center</param>
    /// <param name="uploadRange">the (absolute) range of the resulting Linear QuadTree. Used internally</param>
    /// </summary>
    /// <returns>the absolute range of the resulting Linear QuadTree</returns>
    public abstract Range2D SerializeToLinear(ref List<QuadTreeStruct> lqt, Range2D screenRange,
        Range2D uploadRange = default);
    
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

    
}

internal class QuadTree<T> : QuadTreePart<T> where T : class, IQuadTreeValue<T>
{
    /// <summary>
    /// 2D Array containing all of the sub-blocks
    /// </summary>
    private readonly QuadTreePart<T>[] _subParts;

    /// <summary>
    /// Size of one of the contained blocks
    /// </summary>
    private readonly byte _subPartDepth;

    public QuadTree(T defaultValue, byte maxDepth, T? populateValue = null) : base(defaultValue, maxDepth)
    {
        // calculate the new _subPartDepth
        _subPartDepth = (byte)(MaxDepth - 1);
        
        // instantiate _subParts
        _subParts = new QuadTreePart<T>[4];

        // populate the _subParts array with either the defaultValue or the supplied populateValue if it is not null
        var value = populateValue ?? defaultValue;
        
        for (byte x = 0; x < 2; x++)
        {
            for (byte y = 0; y < 2; y++)
            {
                var subPartAbsoluteRange = GetSubPartAbsoluteRange((x, y));
                
                SetSubPart(x, y,
                    new QuadTreeLeaf<T>(defaultValue, _subPartDepth, subPartAbsoluteRange, value));
            }
        }
        
    }
    
    /// <summary>
    /// Creates a new instance of the QuadTree.
    /// </summary>
    /// <param name="defaultValue">the value to set the contents to by default</param>
    /// <param name="maxDepth">the amount of QuadTreeParts deep the quadtree is (eg. a maxDepth of 4 means the width/height of the blockMatrix is 2^4</param>
    /// <param name="absoluteRange">the absolute range of the QuadTree</param>
    /// <param name="populateValue">[optional] the value to populate the QuadTree with</param>
    private QuadTree(T defaultValue, byte maxDepth, Range2D absoluteRange, T? populateValue = null) : base(defaultValue, maxDepth, absoluteRange)
    {
        // calculate the new _subPartDepth
        _subPartDepth = (byte)(MaxDepth - 1);
        
        // instantiate _subParts
        _subParts = new QuadTreePart<T>[4];

        // populate the _subParts array with either the defaultValue or the supplied populateValue if it is not null
        var value = populateValue ?? defaultValue;
        
        for (byte x = 0; x < 2; x++)
        {
            for (byte y = 0; y < 2; y++)
            {
                var subPartAbsoluteRange = GetSubPartAbsoluteRange((x, y));
                
                SetSubPart(x, y, new QuadTreeLeaf<T>(defaultValue, _subPartDepth, subPartAbsoluteRange, value));
            }
        }
    }
    

    private ref QuadTreePart<T> GetSubPart(Vec2<byte> position)
    {
        var index = Index2DTo1D(position);
        CheckIndex1D(index);
        
        return ref _subParts[index];
    }
    
    private ref QuadTreePart<T> GetSubPart(byte x, byte y)
    {
        var index = Index2DTo1D(x, y);
        CheckIndex1D(index);

        return ref _subParts[index];
    }
    
    private void SetSubPart(Vec2<byte> position, QuadTreePart<T> part)
    {
        var index = Index2DTo1D(position);
        CheckIndex1D(index);

        _subParts[index] = part;
    }
    private void SetSubPart(byte x, byte y, QuadTreePart<T> part)
    {
        var index = Index2DTo1D(x, y);
        CheckIndex1D(index);
        
        _subParts[index] = part;
    }
    
    /// <summary>
    /// Gets a value from the matrix
    /// </summary>
    /// <param name="targetPos">the position to get the value from</param>
    /// <returns>the value</returns>
    internal override T Get(Vec2<long> targetPos)
    {
        var nextPartPos = GetIndex2DFromCoords(targetPos, out var newTargetPos);
        var nextPart = GetSubPart(nextPartPos);
        
        return nextPart.Get(newTargetPos);
    }
    
    public T this[long x, long y]
    {
        get => Get(new Vec2<long>(x, y));
        set => Set(new Vec2<long>(x, y), value);
    }
    
    public T this[Vec2<long> pos]
    {
        get => Get(pos);
        set => Set(pos, value);
    }
    
    public T this[Range2D range]
    {
        set => Set(range, value);
    }
    
    internal override bool Set(Range2D targetRange, T value)
    {
        // if the range refers to a single point, use the Set(Vec2<long>, T) method instead since it is more efficient for single positions
        if (targetRange.Area == 1)
            return Set(targetRange.MaxXMaxY, value);
        
        var modified = false;
        
        // for every subPart,
        for (byte x = 0; x < 2; x++)
        {
            for (byte y = 0; y < 2; y++)
            {
                ref var subPart = ref GetSubPart(x, y);
                
                var subPartRange = subPart.AbsoluteRange;

                // if the supplied targetRange does not overlap with this subPart, continue to the next subPart.
                if (!targetRange.Overlaps(subPartRange)) continue;
                
                // get the absolute pos of this subPart
                var subPartAbsoluteRange = GetSubPartAbsoluteRange((x, y));

                // if the supplied targetRange completely contains the subPart,
                if (targetRange.Contains(subPartRange))
                {
                    // set the subPart to a new QuadTreeLeaf, with the supplied value,
                    subPart = new QuadTreeLeaf<T>(DefaultValue, _subPartDepth, subPartAbsoluteRange, value);
                    
                    // and continue to the next subPart
                    modified = true;
                    continue;
                }
                    
                // otherwise, pass the call downwards:
                // if the subPart is a QuadTreeLeaf (that is not fully contained within the targetRange),
                if (subPart is QuadTreeLeaf<T> subPartValue)
                {
                    // split the QuadTreeLeaf into a QuadTree, keeping the same value,
                    SetSubPart(x, y,
                        new QuadTree<T>(DefaultValue, _subPartDepth, subPartAbsoluteRange, subPartValue.GetValue()));
                }

                // pass the call downwards, into it.
                modified |= subPart.Set(targetRange, value);
            }
        }
        
        // run a compression pass over everything that was/could have been changed
        Compress();
        
        return modified;
    }
    
    internal override bool Set(Vec2<long> targetPos, T value)
    {
        var nextIndex = GetIndex2DFromCoords(targetPos, out var newTargetPos);
        var nextPart = GetSubPart(nextIndex);
        
        var subPartAbsoluteRange = GetSubPartAbsoluteRange(nextIndex);
        
        // splitting QuadTreeLeaves apart into 4 QuadTrees
        // if nextPart is a QuadTreeLeaf of maxDepth > 0, change it to a QuadTree populated with the value stored in it
        if (nextPart is QuadTreeLeaf<T> nextPartValue && _subPartDepth > 0)
        {
            nextPart = new QuadTree<T>(DefaultValue, _subPartDepth, subPartAbsoluteRange, nextPartValue.GetValue());
            
            SetSubPart(nextIndex, nextPart);
        }
        
        // recursively add the value
        var success = nextPart.Set(newTargetPos, value);
        
        // compression
        Compress();
        
        return success;
    }
    
    public override bool Invoke(Range2D range, Func<T, Vec2<long>, bool> run, ResultComparison rc, bool excludeDefault = false)
    {
        var retVal = rc.StartingValue;
        
        foreach (var subPart in _subParts)
        {
            var subPartRect = subPart.AbsoluteRange;
            
            if (range.Overlaps(subPartRect))
            {
                retVal = rc.Comparator
                    .Invoke(retVal, subPart.Invoke(range, run, rc, excludeDefault));
            }
            
        }

        return retVal;
    }
    
    public override bool InvokeLeaf(Range2D range, Func<T, Range2D, Vec2<byte>, bool> run, ResultComparison rc, bool excludeDefault = false, Vec2<byte>? index = null)
    {
        var retVal = rc.StartingValue;
        
        foreach (var subPart in _subParts)
        {
            var subPartRect = subPart.AbsoluteRange;
            
            if (range.Overlaps(subPartRect))
            {
                retVal = rc.Comparator
                    .Invoke(retVal, subPart.InvokeLeaf(range, run, rc, excludeDefault, index));
            }
            
        }

        return retVal;
    }
    
    public override Range2D SerializeToLinear(ref List<QuadTreeStruct> lqt, Range2D screenRange, Range2D uploadRange = default)
    {
        if (uploadRange == default)
        {
            var uploadSize = Math.Max((long)Util.NextPowerOf2((ulong)Math.Max(screenRange.Width, screenRange.Height)), 0x1<<Constants.RenderDepth);

            var halfUs = uploadSize / 2;
            
            // calculate the uploadCenter, rounding the screenRange's center to the nearest `uploadSize`
            // note that screenRange.Center (when this method is run on the root QuadTree) is equal to the current translation.
            var uploadCenterF = screenRange.CenterF / halfUs;
            var uploadCenter = new Vec2<long>((long)Math.Round(uploadCenterF.X), (long)Math.Round(uploadCenterF.Y)) * halfUs;
            
            uploadRange = new Range2D(uploadCenter, uploadSize);
        }
        
        // for each subPart
        for (byte index1D = 0; index1D < 4; index1D++)
        {
            var index2D = Index1DTo2D(index1D);
            
            // get its range, relative to this QuadTree
            var subPartRange = GetSubPartRelativeRange(index2D);
            
            // if the subPart is fully or partially on screen
            if (screenRange.Overlaps(subPartRange))
            {
                // get the subPart
                var subPart = GetSubPart(index2D);
                
                // calculate relative screen/upload ranges
                var relScreenRange = GetRange2DAtIndex(screenRange, index2D);
                
                // recursively call this method
                subPart.SerializeToLinear(ref lqt, relScreenRange, uploadRange);
            }
            
        }
        
        // return the uploadRange's center
        return uploadRange;
    }
    
    
    public override StringBuilder GetSvgMap(StringBuilder? nullableSvgString = null)
    {
        const double scale = Constants.QuadTreeSvgScale;
        
        var svgString = nullableSvgString ?? new StringBuilder(
            $"<svg " +
                $"viewBox=\"" +
                    $"{-Size/2f * scale} " +
                    $"{-Size/2f * scale} " +
                    $"{Size/2f * scale} " +
                    $"{Size/2f * scale}" +
                $"\">" +
            $"<svg/>"
            );
        
        var rect = $"<rect style=\"fill:#ffffff;fill-opacity:0;stroke:#000000;stroke-width:{Math.Min(Size / 64d, 1)}\" " +
                   $"width=\"{Size * scale}\" height=\"{Size * scale}\" " +
                   $"x=\"{AbsoluteRange.MinX * scale}\" y=\"{AbsoluteRange.MinY * scale}\"/>";
        
        // insert the rectangle into the svg string, 6 characters before the end (taking into account the closing `</svg>` tag)
        svgString.Insert(svgString.Length-6, rect);
        
        // pass the call downwards
        foreach (var subPart in _subParts)
        {
            svgString = subPart.GetSvgMap(svgString);
        }
        
        return svgString;

    }

    /// <summary>
    /// Serializes the QuadTree into a format described in:
    /// <code>src/QuadTree-Format.md</code>
    /// <param name="stream">the stream to write to</param>
    /// </summary>
    public void Serialize(Stream stream)
    {
        // initialize the tree and data streams as MemoryStreams
        var tree = new MemoryStream();
        var data = new MemoryStream();
        
        // write the DefaultValue into the first position of the data stream
        data.Write(DefaultValue.Serialize());

        // serialize the entire QuadTree, skipping the root block (serialize the subParts of the root block)
        for (byte x = 0; x < 2; x++)
        {
            for (byte y = 0; y < 2; y++)
            {
                GetSubPart(x, y).SerializeQuadTree(tree, data, (x, y));
            }
        }
        
        // write the header values to the output stream
        stream.Write( new []{MaxDepth}); // maxDepth
        stream.Write(BitConverter.GetBytes(T.SerializeLength)); // element size (bytes)
        stream.Write(BitConverter.GetBytes((uint)(tree.Length + 9))); // pointer to the start of the data section
        
        // reset tree/data stream positions
        tree.Position = 0;
        data.Position = 0;
        
        // write the tree/data sections to the output stream
        tree.WriteTo(stream);
        data.WriteTo(stream);
        
        // dispose the original streams
        tree.Dispose();
        data.Dispose();
    }
    
    internal override void SerializeQuadTree(Stream tree, Stream data, Vec2<byte> index)
    {
        // write the identifier for a QuadTree to the tree stream
        tree.Write(new byte[]{ 1 });
        
        // write the default data for a QuadTreePart to the tree stream
        base.SerializeQuadTree(tree, data, index);
        
        // pass the call downwards, causing everything to be serialized in order
        for (byte x = 0; x < 2; x++)
        {
            for (byte y = 0; y < 2; y++)
            {
                GetSubPart(x, y).SerializeQuadTree(tree, data, (x, y));
            }
        }
        
        // write the identifier for the end of the QuadTree to the tree stream
        tree.Write(new byte[]{ 0 });
    }
    
    /// <summary>
    /// Deserializes the supplied stream into a QuadTree and returns it
    /// </summary>
    /// <param name="stream">the stream containing the serialized QuadTree</param>
    /// <returns>the deserialized QuadTree</returns>
    public static QuadTree<T> Deserialize(Stream stream)
    {
        // get the header
        var header = ReadStream(stream, 9, "header");
        
        // get values from the header
        var depth = header[0];
        var elementSize = BitConverter.ToUInt32(header[1..5]);
        var dataPointer = BitConverter.ToUInt32(header[5..9]);
        
        // warn if the element sizes do not match
        if (elementSize != T.SerializeLength)
            Util.Warn($"Element Sizes do not match! File contains element size of {elementSize} " +
                      $"but is being deserialized with element size of {T.SerializeLength}.");
        
        // read the tree and data sections from the stream
        var treeSpan = ReadStream(stream, dataPointer - 9, "tree section");
        var dataSpan = ReadStream(stream, stream.Length - dataPointer, "data section");
        
        // create streams for the sections
        var tree = new MemoryStream();
        var data = new MemoryStream();
        
        // write the sections to their streams
        tree.Write(treeSpan);
        data.Write(dataSpan);

        // reset positions back to 0 within the streams
        tree.Position = 0;
        data.Position = 0;
        
        // get default value from the data stream
        var defaultValueBytes = ReadStream(data, elementSize, "default value");
        var defaultValue = T.Deserialize(defaultValueBytes);
        
        // create the QuadTree
        var blockMatrix = new QuadTree<T>(defaultValue, depth);
        
        // recursively deserialize the blockMatrix
        blockMatrix.DeserializeQuadTree(tree, data);
        
        // dispose the tree and data streams
        tree.Dispose();
        data.Dispose();
    
        // return the QuadTree
        return blockMatrix;
    }
    
    internal override void DeserializeQuadTree(Stream tree, Stream data)
    {
        while (true)
        {
            // if we have reached the end of the tree stream, stop deserializing
            if (tree.Position == tree.Length)
                return;
            
            // read the id
            var id = ReadStream(tree, 1, "id")[0];
            
            // if the id is 0, stop deserializing into this QuadTree
            if (id == 0) return;

            // read the index of the QuadTreePart within its parent _subParts array
            var index1D = ReadStream(tree, 1, "QuadTree structure")[0];
            
            // get the index2D
            var index2D = Index1DTo2D(index1D);
            
            // calculate the absoluteRange of the index
            var indexAbsoluteRange = GetSubPartAbsoluteRange(index2D);

            switch (id)
            {
                // if we are reading into a QuadTree,
                case 1:
                {
                    // create a new QuadTree and add it to the _subParts array
                    SetSubPart(index2D, new QuadTree<T>(DefaultValue, _subPartDepth, indexAbsoluteRange));
                    
                    // and pass the call downwards, into it.
                    GetSubPart(index2D).DeserializeQuadTree(tree, data);
                    
                    break;
                }
                // otherwise, if we are reading into a QuadTreeLeaf,
                case 2:
                {
                    // read the value pointer (uint) from the tree stream
                    var valuePointerBytes = ReadStream(tree, 4, "value pointer");
                    var valuePointer = BitConverter.ToUInt32(valuePointerBytes) * T.SerializeLength;
                    
                    // use that pointer to get the actual value
                    var valueBytes = ReadStream(data, T.SerializeLength, "value", valuePointer);
                    var value = T.Deserialize(valueBytes);
                    
                    // and create a new QuadTreeLeaf and add it to the _subParts array
                    SetSubPart(index2D, new QuadTreeLeaf<T>(DefaultValue, _subPartDepth, indexAbsoluteRange, value));
                    
                    // and finally, continue in the loop without recursively passing a call downwards.
                    break;
                }
            }
            
        }
        
    }

    private void Compress()
    {
        // for each subPart,
        for (byte x = 0; x < 2; x++)
        {
            for (byte y = 0; y < 2; y++)
            {
                // get absolute pos of this block
                var currentBlockAbsoluteRange = GetSubPartAbsoluteRange((x, y));
                
                // if it is a QuadTree,
                if (GetSubPart(x, y) is QuadTree<T> subPartMatrix)
                {
                    var subSubBlocks = subPartMatrix._subParts;

                    var firstValue = subPartMatrix.GetSubPart(0, 0) is QuadTreeLeaf<T> e ? e.GetValue() : null;
                    
                    if (Equals(firstValue, null))
                        continue;
                    
                    // check if all of these subParts are equal
                    var allEqual = true;
                    foreach (var subSubBlock in subSubBlocks)
                    {
                        if (subSubBlock is not QuadTreeLeaf<T> subSubBlockValue)
                        {
                            allEqual = false;
                            break;
                        }
                            
                        allEqual &= subSubBlockValue.GetValue().Equals(firstValue);

                        if (!allEqual)
                        {
                            break;
                        }
                    }
                    
                    // if so,
                    if (allEqual)
                    {
                        // replace the entire subPart with a QuadTreeLeaf of the correct size
                        SetSubPart(x, y, new QuadTreeLeaf<T>(DefaultValue, _subPartDepth, currentBlockAbsoluteRange, firstValue));
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Calculates the index2D of a subPart, based off of coordinates that lie within it.
    /// </summary>
    /// <param name="targetPos">the coords that lie within the subPart</param>
    /// <param name="newTargetPos">[out] the new targetPos, relative to the subPart at the index2D returned</param>
    /// <returns>the index2D of a subPart</returns>
    private Vec2<byte> GetIndex2DFromCoords(Vec2<long> targetPos, out Vec2<long> newTargetPos)
    {
        // throw exception if the targetPos is out of bounds
        if (!RelativeRange.Contains((Vec2<double>)targetPos))
            throw new CoordOutOfBoundsException(targetPos, RelativeRange);
        
        // get the index of the part the targetPos is in
        var index2D = GetIndex2DFromPos(targetPos);
        
        // calculate the sign
        Vec2<long> blockSign = index2D switch
        {
            (0, 0) => (-1,  1),
            (1, 0) => ( 1,  1),
            (0, 1) => (-1, -1),
            (1, 1) => ( 1, -1),
            _ => throw new InvalidIndexException(index2D)
        };
        
        // calculate the new targetPos
        newTargetPos = targetPos - blockSign * new Vec2<long>(Size / 4);
        
        return index2D;
    }

    /// <summary>
    /// Calculates the range relative to a subPart, provided the original range and the subPart's index2D.
    /// </summary>
    /// <remarks>
    /// If the supplied range is fully or partially out of the bounds of the subPart,
    /// the range will NOT get clamped to its boundaries.
    /// </remarks>
    /// <param name="range">the original range</param>
    /// <param name="index2D">the subParts index2D</param>
    /// <returns>the index2D of a subPart</returns>
    private Range2D GetRange2DAtIndex(Range2D range, Vec2<byte> index2D)
    {
        // calculate the sign
        Vec2<long> blockSign = index2D switch
        {
            (0, 0) => (-1,  1),
            (1, 0) => ( 1,  1),
            (0, 1) => (-1, -1),
            (1, 1) => ( 1, -1),
            _ => throw new InvalidIndexException(index2D)
        };
        
        // calculate the new center
        var newCenter = range.Center - blockSign * new Vec2<long>(Size / 4);
        
        // calculate and return the new range
        return new Range2D(newCenter, range.Width, range.Height);
    }
    
    /// <summary>
    /// Returns the range of the subPart at the specified index, relative to the root QuadTree
    /// </summary>
    /// <remarks>
    /// Does not access the subPart, so this method can be used if the subPart is uninitialized.
    /// </remarks>
    private Range2D GetSubPartAbsoluteRange(Vec2<byte> index)
    {
        // calculate the direction of the top left corner of the part at the supplied index,
        // relative to the center of this QuadTree
        Vec2<long> direction = index switch
        {
            (0, 0) => (-1,  1),
            (1, 0) => ( 0,  1),
            (0, 1) => (-1,  0),
            (1, 1) => ( 0,  0),
            _ => throw new InvalidIndexException(index)
        };
        
        // calculate the size of the subPart
        var subSize = Size / 2;
        
        // calculate the top left coordinate of the subPart, from the direction
        var tl = AbsoluteRange.Center + direction * subSize;
        
        // create and return the range, calculating the bottom right coordinate using the subSize
        return new Range2D(tl, tl + (subSize, -subSize));
    }
    
    /// <summary>
    /// Returns the range of the subPart at the specified index, relative to this QuadTreePart
    /// </summary>
    /// <remarks>
    /// Does not access the subPart, so this method can be used if the subPart is uninitialized.
    /// </remarks>
    private Range2D GetSubPartRelativeRange(Vec2<byte> index)
    {
        var halfSize = Size / 2;
        var quartSize = Size / 4;
        
        return index switch
        {
            (0, 0) => new Range2D((-quartSize, quartSize), halfSize),
            (1, 0) => new Range2D((quartSize, quartSize), halfSize),
            (0, 1) => new Range2D((-quartSize, -quartSize), halfSize),
            (1, 1) => new Range2D((quartSize, -quartSize), halfSize),
            _ => throw new InvalidIndexException(index)
        };
        
    }
    
}

internal class QuadTreeLeaf<T> : QuadTreePart<T>
    
    where T : class, IQuadTreeValue<T>
{
    private T _value;
    
    /// <summary>
    /// Constructs a QuadTreeLeaf, provided the defaultValue, a maxDepth, a range, and a value.
    /// </summary>
    internal QuadTreeLeaf(T defaultValue, byte maxDepth, Range2D range, T value) : base(defaultValue, maxDepth, range)
    {
        _value = value;
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
        _value = value;
    }

    internal override bool Set(Vec2<long> targetPos, T? value)
    {
        if (Equals(value, null))
            return false;
        
        _value = value;
        return true;
    }
    
    internal override bool Set(Range2D targetRange, T? value)
    {
        if (Equals(value, null))
            return false;

        if (targetRange.Contains(AbsoluteRange))
        {
            _value = value;
            return true;
        }
        
        return false;
    }

    internal override T Get(Vec2<long> targetPos)
    {
        return _value;
    }
    
    public override bool Invoke(Range2D range, Func<T, Vec2<long>, bool> run, ResultComparison rc, bool excludeDefault = false)
    {
        if (excludeDefault && _value.Equals(DefaultValue)) return false;
        
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
                        run.Invoke(_value, new Vec2<long>(x, y))
                        );
                }
            }
            
            return result;
        }
        
        return false;
    }
    
    public override bool InvokeLeaf(Range2D range, Func<T, Range2D, Vec2<byte>, bool> run, ResultComparison rc, bool excludeDefault = false, Vec2<byte>? index = null)
    {
        if (excludeDefault && _value.Equals(DefaultValue)) return false;
        
        // get the Range2D of this QuadTreeLeaf
        var blockRange = AbsoluteRange;
        
        // if the supplied range does not overlap with blockRange, return false
        if (!range.Overlaps(blockRange)) return false;
        
        // otherwise, get the overlap rectangle of this QuadTreeLeaf and the supplied range
        var overlap = range.Overlap(blockRange);
        
        // invoke the run Func for the overlap area, only once.
        return run.Invoke(_value, overlap, index ?? (0, 0));

    }
    
    
    public override Range2D SerializeToLinear(ref List<QuadTreeStruct> lqt, Range2D screenRange,
        Range2D uploadRange = default)
    {
        var depth = Math.Clamp(Depth - (Constants.WorldDepth - 16), 0, 16);
        
        // calculate the position of the top left corner of this QuadTreeLeaf, relative to the top left of the uploadRange,
        // and flip the Y axis
        var relPos = (AbsoluteRange.Overlap(uploadRange).TopLeft - uploadRange.TopLeft) * (1, -1);
        
        var code = Util.Interleave(relPos);
        
        // assemble a QuadTreeStruct with the code, maxDepth, and id, and add it to the lqt
        lqt.Add(new QuadTreeStruct(code, (byte)depth, _value.LinearSerializeId));
        
        
        // the relative center of this QuadTreeLeaf is always at (0, 0), so return that
        return uploadRange;
    }

    public override StringBuilder GetSvgMap(StringBuilder? nullableSvgString = null)
    {
        const double scale = Constants.QuadTreeSvgScale;

        var svgString = nullableSvgString ?? new StringBuilder(
            $"<svg " +
            $"viewBox=\"" +
            $"{-Size/2f * scale} " +
            $"{-Size/2f * scale} " +
            $"{Size/2f * scale} " +
            $"{Size/2f * scale}" +
            $"\">" +
            $"<svg/>"
        );
        
        
        var fillColor = "#00ff00";

        if (Size == 1) fillColor = "#ffff00";
        if (_value.Equals(DefaultValue)) fillColor = "#ff0000;fill-opacity:0.1";

        var rect = $"<rect style=\"fill:{fillColor};stroke:#000000;stroke-width:{Math.Min(Size / 64d, 1)}\" " +
                   $"width=\"{Size * scale}\" height=\"{Size * scale}\" " +
                   $"x=\"{AbsoluteRange.MinX * scale}\" y=\"{AbsoluteRange.MinY * scale}\"/>";
        
        svgString.Insert(svgString.Length-6, rect);

        return svgString;
    }
    
    internal override void SerializeQuadTree(Stream tree, Stream data, Vec2<byte> index)
    {
        // if the value is default, we don't need to include it in the serialization
        if (_value.Equals(DefaultValue))
            return;
        
        // write the identifier for a QuadTreeLeaf to the tree stream
        tree.Write(new byte[] { 2 });
        
        // write the default data for a QuadTreePart to the tree stream
        base.SerializeQuadTree(tree, data, index);
        
        // write, and get the pointer of, the value in the data stream
        var pointer = (uint)data.Position / T.SerializeLength;
        data.Write(_value.Serialize());
        
        // write the pointer of the value to the tree stream
        tree.Write(BitConverter.GetBytes(pointer));
    }

    public T GetValue()
    {
        return _value;
    }
}

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
        if (index > 3) throw new InvalidIndexException(new Vec2<int>(index % 2, (int)Math.Floor(index / 2f)));
    }
    
    private static void CheckIndex2D(Vec2<byte> index2D)
    {
        if (index2D.X is not (1 or 0) || index2D.Y is not (1 or 0)) throw new InvalidIndexException(index2D);
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
