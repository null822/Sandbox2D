#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sandbox2D.Maths.Quadtree;
using static Sandbox2D.Maths.QuadTree.QuadTreeUtil;

namespace Sandbox2D.Maths.QuadTree;

internal abstract class QuadTreePart<T> where T : class, IQuadTreeValue<T>
{
    /// <summary>
    /// The absolute position of the QuadTreePart within the entire tree.
    /// </summary>
    /// <remarks>
    /// This is technically a cache that is very hard to recalculate,
    /// but never has to be updated as the value always stays the same.
    /// </remarks>
    internal readonly Vec2<long> AbsolutePos;
    
    /// <summary>
    /// The size of the QuadTreePart.
    /// </summary>
    internal readonly long Size;
    
    /// <summary>
    /// The depth of the QuadTreePart.
    /// </summary>
    internal readonly byte Depth;
    
    /// <summary>
    /// The default value of the QuadTree (everything is default by default)
    /// </summary>
    protected readonly T DefaultValue;

    protected QuadTreePart(T defaultValue, Vec2<long>? absolutePos, byte depth)
    {
        DefaultValue = defaultValue;
        Depth = depth;
        Size = (long)0x1 << depth;
        
        AbsolutePos = absolutePos ?? -new Vec2<long>(Size)/2;
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
    internal abstract T? Get(Vec2<long> targetPos);
    
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
    /// <returns>the result of comparing all of the results of the run lambdas</returns>
    public abstract bool InvokeLeaf(Range2D range, Func<T, Range2D, bool> run,
        ResultComparison rc, bool excludeDefault = false);

    /// <summary>
    /// Creates an SVG string representing the current structure of the entire QuadTree
    /// </summary>
    /// <param name="nullableSvgString">optional parameter, used internally to pass the SVG recursively. Will likely break if changed.</param>
    /// <returns>the contents of an SVG file, ready to be saved in a .svg file</returns>
    public abstract StringBuilder GetSvgMap(StringBuilder? nullableSvgString = null);
    
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

    /// <summary>
    /// Returns the QuadTreePart represented as a Range2D
    /// </summary>
    internal Range2D GetRange()
    {
        return new Range2D(
            AbsolutePos.X, 
            AbsolutePos.Y,
            AbsolutePos.X + Size,
            AbsolutePos.Y + Size);
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

    
    /// <summary>
    /// Creates a new instance of the QuadTree.
    /// </summary>
    /// <param name="defaultValue">the value to set the contents to by default</param>
    /// <param name="depth">the amount of QuadTreeParts deep the quadtree is (eg. a depth of 4 means the width/height of the blockMatrix is 2^4</param>
    /// <param name="blockAbsolutePos"></param>
    /// <param name="populateValue"></param>
    public QuadTree(T defaultValue, byte depth, Vec2<long>? blockAbsolutePos = null, T? populateValue = null) : base(defaultValue, blockAbsolutePos, depth)
    {
        // calculate the new _subPartDepth
        _subPartDepth = (byte)(Depth - 1);
        
        // instantiate _subParts
        _subParts = new QuadTreePart<T>[4];

        // populate the _subParts array with either the defaultValue or the supplied populateValue if it is not null
        var value = populateValue ?? defaultValue;
        
        for (byte x = 0; x < 2; x++)
        {
            for (byte y = 0; y < 2; y++)
            {
                var subPartAbsolutePos = GetSubPartAbsolutePos((x, y));
                
                SetSubPart(x, y,
                    new QuadTreeLeaf<T>(defaultValue, subPartAbsolutePos, _subPartDepth, value));
            }
        }
    }

    private ref QuadTreePart<T> GetSubPart(Vec2<byte> position)
    {
        var index = Index2DTo1D(position);
        CheckIndex(index);
        
        return ref _subParts[index];
    }
    private ref QuadTreePart<T> GetSubPart(byte x, byte y)
    {
        var index = Index2DTo1D(x, y);
        CheckIndex(index);

        return ref _subParts[index];
    }
    
    private void SetSubPart(Vec2<byte> position, QuadTreePart<T> part)
    {
        var index = Index2DTo1D(position);
        CheckIndex(index);

        _subParts[index] = part;
    }
    private void SetSubPart(byte x, byte y, QuadTreePart<T> part)
    {
        var index = Index2DTo1D(x, y);
        CheckIndex(index);
        
        _subParts[index] = part;
    }

    private static void CheckIndex(byte index)
    {
        if (index > 3) throw new IndexOutOfRangeException(
            $"SubPart coordinate {new Vec2<int>(index % 2, (int)Math.Floor(index / 2f))} out of bounds for size of 2x2.");
    }
    
    /// <summary>
    /// Gets a value from the matrix
    /// </summary>
    /// <param name="targetPos">the position to get the value from</param>
    /// <returns>the value</returns>
    internal override T? Get(Vec2<long> targetPos)
    {
        var nextBlockPos = GetIndex2DFromCoords(targetPos, out var newTargetPos);
        var nextBlock = GetSubPart(nextBlockPos);
        
        return nextBlock.Get(newTargetPos);
    }
    
    public T? this[long x, long y]
    {
        get => Get(new Vec2<long>(x, y));
        set => Set(new Vec2<long>(x, y), value ?? DefaultValue);
    }
    
    public T? this[Vec2<long> pos]
    {
        get => Get(pos);
        set => Set(pos, value ?? DefaultValue);
    }
    
    public T? this[Range2D range]
    {
        set => Set(range, value ?? DefaultValue);
    }
    
    internal override bool Set(Range2D targetRange, T value)
    {
        // if the range refers to a single point, use the Set(Vec2<long>, T) method instead since it is more efficient for single positions
        if (targetRange.Area == 1)
            return Set(targetRange.BottomLeft, value);
        
        var modified = false;
        
        // for every subPart,
        for (byte x = 0; x < 2; x++)
        {
            for (byte y = 0; y < 2; y++)
            {
                ref var subPart = ref GetSubPart(x, y);
                
                var subPartRange = subPart.GetRange();

                // if the supplied targetRange does not overlap with this subPart, continue to the next subPart.
                if (!targetRange.Overlaps(subPartRange)) continue;
                
                // get the absolute pos of this subPart
                var subPartAbsolutePos = GetSubPartAbsolutePos((x, y));

                // if the supplied targetRange completely contains the subPart,
                if (targetRange.Contains(subPartRange))
                {
                    // set the subPart to a new QuadTreeLeaf, with the supplied value,
                    subPart = new QuadTreeLeaf<T>(DefaultValue, subPartAbsolutePos, _subPartDepth, value);
                    
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
                        new QuadTree<T>(DefaultValue, _subPartDepth, subPartAbsolutePos, subPartValue.GetValue()));
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
        var nextBlock = GetSubPart(nextIndex);
        
        var subPartAbsolutePos = GetSubPartAbsolutePos(nextIndex);
        
        // if part is a QuadTreeLeaf of depth > 0, change it to a QuadTree
        if (nextBlock is QuadTreeLeaf<T> nextBlockValue && _subPartDepth > 0)
        {
            nextBlock = new QuadTree<T>(DefaultValue, _subPartDepth, subPartAbsolutePos, nextBlockValue.GetValue());
            
            SetSubPart(nextIndex, nextBlock);
        }
        
        // recursively add the value
        var success = nextBlock.Set(newTargetPos, value);
        
        // compression
        Compress();
        
        return success;
    }
    
    public override bool Invoke(Range2D range, Func<T, Vec2<long>, bool> run, ResultComparison rc, bool excludeDefault = false)
    {
        var retVal = rc.StartingValue;
        
        foreach (var subPart in _subParts)
        {
            var subPartRect = new Range2D(
                subPart.AbsolutePos.X, 
                subPart.AbsolutePos.Y, 
                subPart.AbsolutePos.X + subPart.Size, 
                subPart.AbsolutePos.Y + subPart.Size);
            
            if (range.Overlaps(subPartRect))
            {
                retVal = rc.Comparator
                    .Invoke(retVal, subPart.Invoke(range, run, rc, excludeDefault));
            }
            
        }

        return retVal;
    }
    
    public override bool InvokeLeaf(Range2D range, Func<T, Range2D, bool> run, ResultComparison rc, bool excludeDefault = false)
    {
        var retVal = rc.StartingValue;
        
        foreach (var subPart in _subParts)
        {
            var subPartRect = new Range2D(
                subPart.AbsolutePos.X, 
                subPart.AbsolutePos.Y, 
                subPart.AbsolutePos.X + subPart.Size, 
                subPart.AbsolutePos.Y + subPart.Size);
            
            if (range.Overlaps(subPartRect))
            {
                retVal = rc.Comparator
                    .Invoke(retVal, subPart.InvokeLeaf(range, run, rc, excludeDefault));
            }
            
        }

        return retVal;
    }
    
    private Vec2<byte> GetIndex2DFromCoords(Vec2<long> targetPos, out Vec2<long> newTargetPos)
    {
        var range = GetRange();
        
        // throw exception if the targetPos is out of bounds
        if (!range.Contains((Vec2<double>)targetPos + range.Center))
            throw new IndexOutOfRangeException($"Position {targetPos} is outside QuadTree bounds of {GetRange()}");
        
        // get the index of the block the target is in
        var index2D = GetIndex2DFromPos(targetPos);
        
        // calculate the new targetPos
        var blockSign = new Vec2<long>(
            index2D.X == 0 ? -1 : 1,
            index2D.Y == 0 ? 1 : -1);
        
        newTargetPos = targetPos + new Vec2<long>(Size / 4) * -blockSign;

        return index2D;
    }
    
    public override StringBuilder GetSvgMap(StringBuilder? nullableSvgString = null)
    {
        const double scale = Constants.BlockMatrixSvgScale;
        
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
                   $"x=\"{AbsolutePos.X * scale}\" y=\"{AbsolutePos.Y * scale}\"/>";
        
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
        stream.Write( new []{Depth}); // depth
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
            
            // calculate the absolutePos at the index
            var indexAbsolutePos = GetSubPartAbsolutePos(index2D);

            switch (id)
            {
                // if we are reading into a QuadTree,
                case 1:
                {
                    // create a new QuadTree and add it to the _subParts array
                    SetSubPart(index2D, new QuadTree<T>(DefaultValue, _subPartDepth, indexAbsolutePos));
                    
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
                    SetSubPart(index2D, new QuadTreeLeaf<T>(DefaultValue, indexAbsolutePos, _subPartDepth, value));
                    
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
                var currentBlockAbsolutePos = GetSubPartAbsolutePos((x, y));
                
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
                        SetSubPart(x, y, new QuadTreeLeaf<T>(DefaultValue, currentBlockAbsolutePos, _subPartDepth, firstValue));
                    }
                }
            }
        }
    }

    private Vec2<long> GetSubPartAbsolutePos(Vec2<byte> index)
    {
        index = new Vec2<byte>(index.X, (byte)(1 - index.Y));
        
        return AbsolutePos + (Vec2<long>)index * (Size / 2);
    }
    
    
}

internal class QuadTreeLeaf<T>(T defaultValue, Vec2<long> absolutePos, byte depth, T value) 
    : QuadTreePart<T>(defaultValue, absolutePos, depth)
    where T : class, IQuadTreeValue<T>
{
    private T _value = value;

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

        if (targetRange.Contains(GetRange()))
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
        var blockRange = new Range2D(
            AbsolutePos.X,
            AbsolutePos.Y, 
            AbsolutePos.X + Size, 
            AbsolutePos.Y + Size);
        
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
    
    public override bool InvokeLeaf(Range2D range, Func<T, Range2D, bool> run, ResultComparison rc, bool excludeDefault = false)
    {
        if (excludeDefault && _value.Equals(DefaultValue)) return false;
        
        // get the Range2D of this QuadTreeLeaf
        var blockRange = new Range2D(
            AbsolutePos.X,
            AbsolutePos.Y,
            AbsolutePos.X + Size, 
            AbsolutePos.Y + Size);
        
        // if the supplied range does not overlap with blockRange, return false
        if (!range.Overlaps(blockRange)) return false;
        
        // otherwise, get the overlap rectangle of this QuadTreeLeaf and the supplied range
        var overlap = range.Overlap(blockRange);
            
        // invoke the run Func for the overlap area, only once.
        return run.Invoke(_value, overlap);

    }
    
    public override StringBuilder GetSvgMap(StringBuilder? nullableSvgString = null)
    {
        const double scale = Constants.BlockMatrixSvgScale;

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
                   $"x=\"{AbsolutePos.X * scale}\" y=\"{AbsolutePos.Y * scale}\"/>";
        
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

    public Vec2<long> GetPos()
    {
        return AbsolutePos;
    }
    
    public T GetValue()
    {
        return _value;
    }
    
    // overrides
    public static bool operator ==(QuadTreeLeaf<T> a, QuadTreeLeaf<T> b)
    {
        if (Equals(a, null) || Equals(b, null))
            return false;

        return a._value.Equals(b._value);
    }
    
    public static bool operator !=(QuadTreeLeaf<T> a, QuadTreeLeaf<T> b)
    {
        if (Equals(a, null) || Equals(b, null))
            return false;

        return !a._value.Equals(b._value);
    }
    
    private bool Equals(QuadTreeLeaf<T> other)
    {
        return EqualityComparer<T>.Default.Equals(_value, other._value);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((QuadTreeLeaf<T>)obj);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<T>.Default.GetHashCode(_value);
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
