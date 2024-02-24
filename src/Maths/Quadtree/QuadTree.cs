#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sandbox2D.Exceptions;
using Sandbox2D.Graphics.Renderables;
using Sandbox2D.Maths.Quadtree;
using static Sandbox2D.Maths.Quadtree.QuadTreeUtil;

namespace Sandbox2D.Maths.QuadTree;

internal class QuadTree<T, TValue> : QuadTreePart<T, TValue> where T : class where TValue : class, IQuadTreeValue<T>
{
    /// <summary>
    /// 2D Array containing all of the sub-blocks
    /// </summary>
    private readonly QuadTreePart<T, TValue>[] _subParts;
    
    /// <summary>
    /// Size of one of the contained blocks
    /// </summary>
    private readonly byte _subPartDepth;

    public QuadTree(T defaultValue, byte maxDepth, T? populateValue = null) : base(defaultValue, maxDepth)
    {
        // calculate the new _subPartDepth
        _subPartDepth = (byte)(MaxDepth - 1);
        
        // instantiate _subParts
        _subParts = new QuadTreePart<T, TValue>[4];

        // populate the _subParts array with either the defaultValue or the supplied populateValue if it is not null
        var value = populateValue ?? defaultValue;
        
        for (byte x = 0; x < 2; x++)
        {
            for (byte y = 0; y < 2; y++)
            {
                var subPartAbsoluteRange = GetSubPartAbsoluteRange((x, y));
                
                SetSubPart(x, y,
                    new QuadTreeLeaf<T, TValue>(defaultValue, _subPartDepth, subPartAbsoluteRange, value));
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
        _subParts = new QuadTreePart<T, TValue>[4];

        // populate the _subParts array with either the defaultValue or the supplied populateValue if it is not null
        var value = populateValue ?? defaultValue;
        
        for (byte x = 0; x < 2; x++)
        {
            for (byte y = 0; y < 2; y++)
            {
                var subPartAbsoluteRange = GetSubPartAbsoluteRange((x, y));
                
                SetSubPart(x, y, new QuadTreeLeaf<T, TValue>(defaultValue, _subPartDepth, subPartAbsoluteRange, value));
            }
        }
    }
    

    private ref QuadTreePart<T, TValue> GetSubPart(Vec2<byte> position)
    {
        var index = Index2DTo1D(position);
        CheckIndex1D(index);
        
        return ref _subParts[index];
    }
    
    private ref QuadTreePart<T, TValue> GetSubPart(byte x, byte y)
    {
        var index = Index2DTo1D(x, y);
        CheckIndex1D(index);

        return ref _subParts[index];
    }
    
    private void SetSubPart(Vec2<byte> position, QuadTreePart<T, TValue> part)
    {
        var index = Index2DTo1D(position);
        CheckIndex1D(index);

        _subParts[index] = part;
    }
    private void SetSubPart(byte x, byte y, QuadTreePart<T, TValue> part)
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
                    subPart = new QuadTreeLeaf<T, TValue>(DefaultValue, _subPartDepth, subPartAbsoluteRange, value);
                    
                    // and continue to the next subPart
                    modified = true;
                    continue;
                }
                    
                // otherwise, pass the call downwards:
                // if the subPart is a QuadTreeLeaf (that is not fully contained within the targetRange),
                // split the QuadTreeLeaf into a QuadTree, keeping the same value,
                if (subPart is QuadTreeLeaf<T, TValue> subPartValue)
                {
                    var popValue = subPartValue.Value.Get();
                    var newTree = new QuadTree<T, TValue>(DefaultValue, _subPartDepth, subPartAbsoluteRange, popValue);
                    SetSubPart(x, y, newTree);
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
        if (nextPart is QuadTreeLeaf<T, TValue> nextPartValue && _subPartDepth > 0)
        {
            nextPart = new QuadTree<T, TValue>(DefaultValue, _subPartDepth, subPartAbsoluteRange, nextPartValue.Value.Get());
            
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
    
    public override Range2D SerializeToLinear(ref List<QuadTreeStruct> lqt, Range2D screenRange, Range2D renderRange = default)
    {
        // calculate renderRange if it is not set
        if (renderRange == default)
        {
            const ulong full64 = (ulong)long.MaxValue + 1;
            
            // calculate size of the renderRange
            var maxSize = Math.Max(screenRange.Width, screenRange.Height);
            var next2 = Util.NextPowerOf2(maxSize);
            var size = Math.Clamp(next2 == full64 ? next2 : next2 * 2, 0x1L << Constants.RenderDepth, Size);
            var snapDistance = (long)(size / 2);
            
            // calculate the center, snapping it to the nearest `snapDistance`
            var centerF = (Vec2<decimal>)screenRange.Center;
            var center = new Vec2<long>(
                (long)Math.Round(centerF.X / snapDistance) * snapDistance,
                (long)Math.Round(centerF.Y / snapDistance) * snapDistance
            );
            
            // calculate min/max center positions
            var min = RelativeRange.MinX + snapDistance;
            var max = RelativeRange.MaxX - snapDistance;
            
            // clamp the center to remain within the world
            center = new Vec2<long>(Math.Clamp(center.X, min, max), Math.Clamp(center.Y, min, max));
            
            // assemble renderRange, using the `center` and `size`
            renderRange = new Range2D(center, size);
        }
        
        // calculate the depth of the contained subParts
        var subPartDepth = Math.Max(Depth+1 - (Constants.WorldDepth - Constants.RenderDepth), 0);
        
        // if the subParts are too deep to be added to the lqt
        if (subPartDepth > Constants.RenderDepth)
        {
            // get the average of the subPart
            var avg = AverageValue();
            
            // and add it to the lqt
            AddToLinearQuadTree(ref lqt, renderRange, TValue.New(avg));
            
            // exit
            return renderRange;
        }
        
        // for each subPart
        for (byte index1D = 0; index1D < 4; index1D++)
        {
            // get the index2D
            var index2D = Index1DTo2D(index1D);
            
            // get the subPart's range, relative to this QuadTree
            var subPartRange = GetSubPartAbsoluteRange(index2D);
            
            // if the subPart is not fully or partially on screen, ignore it and go to the next one
            if (!screenRange.Overlaps(subPartRange))
            {
                continue;
            }
            
            
            // get the subPart
            var subPart = GetSubPart(index2D);
            
            // recursively call this method
            subPart.SerializeToLinear(ref lqt, screenRange, renderRange);
            
        }
        
        // exit
        return renderRange;
    }

    public override T AverageValue()
    {
        var values = new T[4];
        
        // get all of the values
        for (byte index1D = 0; index1D < 4; index1D++)
        {
            // get the index2D and the subPart
            var index2D = Index1DTo2D(index1D);
            var subPart = GetSubPart(index2D);

            values[index1D] = subPart.AverageValue();
        }
        
        var qty0 = values.Count(s => s == values[0]);
        
        if (qty0 == 4)
            return values[0];
        
        var qty1 = values.Count(s => s == values[1]);
        var qty2 = values.Count(s => s == values[2]);
        var qty3 = values.Count(s => s == values[3]);
        
        var max = new [] { qty0, qty1, qty2, qty3 }.Max();
        
        return values[max];
    }
    
    
    public override StringBuilder GetSvgMap(StringBuilder? nullableSvgString = null)
    {
        const double scale = Constants.QuadTreeSvgScale;

        var halfSize = (long)(Size / 2);
        
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
        data.Write(TValue.New(DefaultValue).Serialize());

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
        stream.Write(BitConverter.GetBytes(TValue.SerializeLength)); // element size (bytes)
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
    public static QuadTree<T, TValue> Deserialize(Stream stream)
    {
        // get the header
        var header = ReadStream(stream, 9, "header");
        
        // get values from the header
        var depth = header[0];
        var elementSize = BitConverter.ToUInt32(header[1..5]);
        var dataPointer = BitConverter.ToUInt32(header[5..9]);
        
        // warn if the element sizes do not match
        if (elementSize != TValue.SerializeLength)
            Util.Warn($"Element Sizes do not match! File contains element size of {elementSize} " +
                      $"but is being deserialized with element size of {TValue.SerializeLength}.");
        
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
        var defaultValue = TValue.Deserialize(defaultValueBytes);
        
        // create the QuadTree
        var blockMatrix = new QuadTree<T, TValue>(defaultValue, depth);
        
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
                    SetSubPart(index2D, new QuadTree<T, TValue>(DefaultValue, _subPartDepth, indexAbsoluteRange));
                    
                    // and pass the call downwards, into it.
                    GetSubPart(index2D).DeserializeQuadTree(tree, data);
                    
                    break;
                }
                // otherwise, if we are reading into a QuadTreeLeaf,
                case 2:
                {
                    // read the value pointer (uint) from the tree stream
                    var valuePointerBytes = ReadStream(tree, 4, "value pointer");
                    var valuePointer = BitConverter.ToUInt32(valuePointerBytes) * TValue.SerializeLength;
                    
                    // use that pointer to get the actual value
                    var valueBytes = ReadStream(data, TValue.SerializeLength, "value", valuePointer);
                    var value = TValue.Deserialize(valueBytes);
                    
                    // and create a new QuadTreeLeaf and add it to the _subParts array
                    SetSubPart(index2D, new QuadTreeLeaf<T, TValue>(DefaultValue, _subPartDepth, indexAbsoluteRange, value));
                    
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
                if (GetSubPart(x, y) is QuadTree<T, TValue> subPartMatrix)
                {
                    var subSubParts = subPartMatrix._subParts;
                    
                    if (subPartMatrix.GetSubPart(0, 0) is not QuadTreeLeaf<T, TValue> firstLeaf)
                        continue;
                    
                    // check if all of these subParts are equal
                    var allEqual = true;
                    foreach (var subSubPart in subSubParts)
                    {
                        if (subSubPart is not QuadTreeLeaf<T, TValue> subSubPartLeaf)
                        {
                            allEqual = false;
                            break;
                        }
                            
                        allEqual &= subSubPartLeaf.Value.Equals(firstLeaf.Value);

                        if (!allEqual)
                        {
                            break;
                        }
                    }
                    
                    // if so,
                    if (allEqual)
                    {
                        // replace the entire subPart with a QuadTreeLeaf of the correct size
                        SetSubPart(x, y, new QuadTreeLeaf<T, TValue>(DefaultValue, _subPartDepth, currentBlockAbsoluteRange, firstLeaf.Value.Get()));
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
        if (!RelativeRange.Contains(targetPos))
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
        newTargetPos = targetPos - blockSign * new Vec2<long>((long)(Size / 4));
        
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
        var newCenter = range.Center - blockSign * new Vec2<long>((long)(Size / 4));
        
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
        var subSize = (long)(Size / 2);
        
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
        var quartSize = (long)(Size / 4);
        
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
