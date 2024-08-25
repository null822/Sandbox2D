using System;
using System.IO;
using System.Numerics;
using System.Text;
using Sandbox2D.Maths.Quadtree.FeatureNodeTypes;
using static Sandbox2D.Maths.Quadtree.QuadtreeUtil;
using static Sandbox2D.Maths.Quadtree.NodeType;
using static Sandbox2D.Maths.BitUtil;
using Color = Sandbox2D.Graphics.Color;

namespace Sandbox2D.Maths.Quadtree;

/// <summary>
/// Represents a Quadtree, able to store a large amount of 2D data in recursive 4x4 <see cref="QuadtreeNode"/>s.
/// </summary>
/// <typeparam name="T">The type of the data to store. Implementing interfaces in <see cref="FeatureNodeTypes"/> will enable different features.</typeparam>
public class Quadtree<T> : IDisposable where T : IQuadtreeElement<T>
{
    /// <summary>
    /// A <see cref="DynamicArray{T}"/> of all the <see cref="QuadtreeNode"/>s within this <see cref="Quadtree{T}"/>, linking to each other and, for leaf nodes, linking to <see cref="_data"/>.
    /// </summary>
    /// <remarks>The root node of this <see cref="Quadtree{T}"/> is always at index 0, and the subset root is always at
    /// index 1.</remarks>
    private readonly DynamicArray<QuadtreeNode> _tree;
    
    /// <summary>
    /// A <see cref="DynamicArray{T}"/> of all the values of every leaf node. Linked to by every leaf node in <see cref="_tree"/>.
    /// </summary>
    /// <remarks>The default value for unset elements in this <see cref="Quadtree{T}"/> is always stored in index 0.</remarks>
    private readonly DynamicArray<T> _data;
    
    /// <summary>
    /// The modifications that have been done to this <see cref="Quadtree{T}"/> since the last time <see cref="Compress"/> was called.
    /// </summary>
    private readonly DynamicArray<Range2D> _modifications;

    /// <summary>
    /// Equal to the height of the root node, or, one more than the maximum height of any non-root node. Also equal to
    /// the maximum amount of recursive nodes in this <see cref="Quadtree{T}"/>, excluding the root node.
    /// </summary>
    public readonly int MaxHeight;
    
    /// <summary>
    /// The dimensions of the entire quadtree.
    /// </summary>
    public readonly Range2D Dimensions;
    
    /// <summary>
    /// The subset root node.
    /// </summary>
    private QuadtreeNode _subset; // TODO: actually implement the subset
    
    /// <summary>
    /// The dimensions of the subset root node (stored in <see cref="_subset"/>).
    /// </summary>
    private Range2D _subsetDimensions;
    
    /// <summary>
    /// Stores which features have been enabled. Indexed using <see cref="QuadtreeFeature"/>.
    /// </summary>
    private readonly uint _enabledFeatures;
    
    /// <summary>
    /// Constructs a new <see cref="Quadtree{T}"/>.
    /// </summary>
    /// <param name="maxHeight">the maximum height of the quadtree, where width = 2^maxHeight. range: 2-64</param>
    /// <param name="defaultValue">the default value that everything will be initialized to</param>
    /// <exception cref="InvalidMaxHeightException">Thrown when <paramref name="maxHeight"/> is not valid (ie. not 2-64)</exception>
    public Quadtree(int maxHeight, T defaultValue)
    {
        // validate maxHeight
        if (maxHeight is < 2 or > 64)
            throw new InvalidMaxHeightException(maxHeight);
        
        // figure out which features to enable
        if (defaultValue is IFeatureModificationStore)
            _enabledFeatures |= 0x1u << (int)QuadtreeFeature.ModificationStore;
        if (defaultValue is IFeatureFileSerialization<T>)
            _enabledFeatures |= 0x1u << (int)QuadtreeFeature.FileSerialization;
        if (defaultValue is IFeatureElementColor)
            _enabledFeatures |= 0x1u << (int)QuadtreeFeature.ElementColor;
        if (defaultValue is IFeatureCellularAutomata)
            _enabledFeatures |= 0x1u << (int)QuadtreeFeature.CellularAutomata;
        
        var storeModifications = CheckFeatures(QuadtreeFeature.ModificationStore);
        
        // assign maxHeight and dimensions
        MaxHeight = maxHeight;
        var halfSize = -1L << (MaxHeight - 1);
        Dimensions = NodeRangeFromPos(new Vec2<long>(halfSize), MaxHeight);
        
        // create tree and data arrays
        _tree = new DynamicArray<QuadtreeNode>(Constants.QuadtreeArrayLength, storeModifications);
        _data = new DynamicArray<T>(Constants.QuadtreeArrayLength, storeModifications);
        
        // initialize the data section
        _data.Add(defaultValue);
        
        // initialize the tree section
        _tree.Add(new QuadtreeNode(0));
        
        // create modifications array
        if (storeModifications)
            _modifications = new DynamicArray<Range2D>(Constants.QuadtreeArrayLength, false, false);
    }

    #region Get / Set
    
    /// <summary>
    /// Returns the value at the supplied position.
    /// </summary>
    /// <param name="pos">the position</param>
    private T Get(Vec2<long> pos)
    {
        // validate pos parameter
        if (!Dimensions.Contains(pos)) throw new PositionOutOfBoundsException(pos, Dimensions);
        
        // get the targeted node
        var node = GetNodeRef(Interleave(pos, MaxHeight), readOnly: true);
        
        // return the value of the node
        return _data[_tree[node].GetValueRef()];
    }
    
    /// <summary>
    /// Sets a single position within the quadtree to the supplied value.
    /// </summary>
    /// <param name="pos">the position</param>
    /// <param name="value">the value</param>
    private void Set(Vec2<long> pos, T value)
    {
        // validate pos parameter
        if (!Dimensions.Contains(pos)) throw new PositionOutOfBoundsException(pos, Dimensions);
        
        // get the targeted node
        var node = GetNodeRef(Interleave(pos, MaxHeight));
        
        // overwrite the node with the new value
        _tree[node] = new QuadtreeNode(_data.Add(value));
        
        // store the modification
        if (CheckFeatures(QuadtreeFeature.ModificationStore))
            _modifications.Add(NodeRangeFromPos(pos, 0));
    }
    
    /// <summary>
    /// Sets every point in the supplied <see cref="Range2D"/> to the supplied value.
    /// </summary>
    /// <param name="range">the <see cref="Range2D"/> representing the area to set</param>
    /// <param name="value">the value to set</param>
    /// <exception cref="RangeOutOfBoundsException">Thrown when <paramref name="range"/> is partially or completely outside the quadtree</exception>
    private void Set(Range2D range, T value)
    {
        if (!Dimensions.Contains(range))
            throw new RangeOutOfBoundsException(range, Dimensions);
        
        // store the modification
        if (CheckFeatures(QuadtreeFeature.ModificationStore))
            _modifications.Add(range);
        
        // store the `value` and keep a reference to it
        var valueRef = value == null ? 0 : _data.Add(value);
        var valueNode = new QuadtreeNode(valueRef);
        
        var maxZValue = Interleave(range.Tr, MaxHeight); // the z-value of the last node to be traversed
        var path = new long[MaxHeight]; // an array of all the nodes traversed through to get to the current node, indexed using height
        // set up variables, starting at the bottom-left-most point in the `range`
        var nodePos = range.Bl; // the position of the current node
        var zValue = Interleave(range.Bl, MaxHeight); // the z-value of the current node
        var height = CalculateLargestHeight(zValue, MaxHeight); // the height of the current node
        var nodeRef = GetNodeRef(zValue, height, false, path); // a reference to the current node
        
        while (true)
        {
            var nodeRange = NodeRangeFromPos(nodePos, height);
            var node = _tree[nodeRef];
            
            // if the node is only partially contained, step down into it, narrowing the area we can modify
            if (range.Overlaps(nodeRange) && !range.Contains(nodeRange))
            {
                // if it is a leaf node, subdivide it first
                if (node.Type == Leaf)
                {
                    Subdivide(nodeRef);
                    node = _tree[nodeRef]; // refresh `node`
                }
                
                // step down into the node
                nodeRef = node.Ref0;
                height--;
                // update the path to reflect what we just did
                path[height] = nodeRef;
            }
            else
            {
                // if the node is fully contained within the range, overwrite it with `valueNode`, setting the value
                if (range.Contains(nodeRange))
                {
                    // delete the nodes children if we can, freeing up memory and preventing orphaned nodes
                    DeleteChildren(zValue, height, nodeRef);
                    
                    // replace the node with a copy of `valueNode`, containing the value to set
                    _tree[nodeRef] = valueNode;
                }
                
                // jump to the next node
                nodePos = GetNextNodePos(nodePos, zValue, height);
                (zValue, height, nodeRef) = GetNextNode(zValue, height, maxZValue, ref path);
                if (nodeRef == -1)
                    break;
            }
        }
    }
    
    /// <summary>
    /// Removes all elements from the <see cref="Quadtree{T}"/>, resetting it back to its original state.
    /// </summary>
    public void Clear()
    {
        DeleteChildren(0, MaxHeight, 0); // delete the root node's children
        
        // shrink the tree
        _tree.Shrink();
    }
    
    #endregion
    
    #region Compression
    
    /// <summary>
    /// Tries to combine as many nodes as possible in this <see cref="Quadtree{T}"/>, which are referenced in <see cref="_modifications"/>.
    /// </summary>
    public void Compress()
    {
        // if we do not store modifications, compress the entire quadtree
        if (!CheckFeatures(QuadtreeFeature.ModificationStore))
        {
            CompressRange(Dimensions);
        }
        else // otherwise, compress the regions that have been modified since the last compression
        {
            // exit if there have been no modifications since the last time this method was called
            if (_modifications.Length == 0)
                return;
            
            // combine the modifications to reduce the amount of times the same area is compressed multiple times
            _modifications.Sort(RangeBlComparer);
            var combinedMods = new DynamicArray<Range2D>();
            combinedMods.EnsureCapacity(_modifications.Length);
            combinedMods.Add(_modifications[0]);
            for (var i = 1; i < _modifications.Length; i++)
            {
                var modification = _modifications[i];
                var last = combinedMods[combinedMods.Length - 1];
                
                if (modification == last) continue;
                if (last.Overlaps(modification))
                {
                    var comb = new Range2D(last.Bl, modification.Tr);
                    
                    // if combining the modifications doesn't create too much extra area, combine them
                    if (comb.Area <= last.Area + modification.Area - comb.Area)
                    {
                        combinedMods[combinedMods.Length - 1] = comb;
                        continue;
                    }
                }
                
                // otherwise, just add the area separately
                combinedMods.Add(modification);
            }
            
            // compress all the ranges
            for (var i = 0; i < combinedMods.Length; i++)
            {
                CompressRange(combinedMods[i]);
            }
            
            _modifications.Clear();
            combinedMods.Clear();
        }
        
        // shrink the tree section
        _tree.Shrink();
    }
    
    /// <summary>
    /// Tries to combine as many identical nodes as possible in a section of this <see cref="Quadtree{T}"/>.
    /// </summary>
    /// <param name="range">the section to compress</param>
    private void CompressRange(Range2D range)
    {
        var maxZValue = Pow2Min1U128(MaxHeight * 2); // the z-value of the last node to be traversed
        var path = new long[MaxHeight]; // an array of all the nodes traversed through to get to the current node, indexed using height
        
        // set up variables, starting at the bottom-left-most point in the `range`
        var zValue = Interleave(range.Bl, MaxHeight); // the z-value of the current node
        var height = CalculateLargestHeight(zValue, MaxHeight); // the height of the current node
        var nodeRef = GetNodeRef(zValue, height, false, path); // a reference to the current node
        
        while (true)
        {
            var node = _tree[nodeRef];
            
            // if the node is a branch node, step down into it
            if (node.Type == Branch)
            {
                nodeRef = node.Ref0;
                height--;
                // update the path
                path[height] = nodeRef;
            }
            else
            {
                // otherwise, if the node is a leaf node, try to compress as much as possible
                
                // if this is the last node we will compress, force a "full compress", to tie up any loose ends (not
                // fully compressed nodes)
                var trZValue = zValue + ~(height == 64 ? 0 : ~(UInt128)0x0 << (2 * height));
                if (trZValue == maxZValue)
                {
                    CompressNode(zValue, height, path, true);
                    break;
                }
                
                // otherwise, just compress the node
                CompressNode(zValue, height, path);
                
                // jump to the next node
                (zValue, height, nodeRef) = GetNextNode(zValue, height, maxZValue, ref path);
                if (nodeRef == -1) break; // exit if we went through the entire quadtree
            }
        }
    }
    
    /// <summary>
    /// Tries to compress a single node and all of its parents, merging identical siblings.
    /// </summary>
    /// <param name="zValue">the z-value of the node</param>
    /// <param name="height">the height of the node</param>
    /// <param name="path">an array containing all the nodes traversed through to get to this node, indexed using height</param>
    /// <param name="forceFull">whether to compress every node regardless of if it is the last node in its parent</param>
    /// <exception cref="InvalidNodeTypeException">thrown when a node in <paramref name="path"/> has a parent node that is a <see cref="NodeType.Leaf"/> node</exception>
    private void CompressNode(UInt128 zValue, int height, long[] path, bool forceFull = false)
    {
        // try to compress as much as possible
        while (true)
        {
            // don't compress the root node or its immediate children since they are needed for subsets
            if (height >= MaxHeight-1) break;
            
            // if this not is the last node in its parent, don't try to compress it, since it's parents will be reached
            // by CompressRange() later anyway
            if (ZValueIndex(zValue, height) != 3 && !forceFull) return;
            
            // get the parent
            var parentRef = path[height + 1];
            var parent = _tree[parentRef];
            
            // if the parent node is a leaf, something went very wrong
            if (parent.Type == Leaf) throw new InvalidNodeTypeException(Leaf, Branch, "CompressNode/path");
            
            // get the parent's siblings' value refs
            var valueRef = 0L;
            T value = default;
            for (var i = 0; i < 4; i++)
            {
                var node = _tree[parent.GetNodeRef(i)];
                if (node.Type == Branch) return; // exit out of this method if any child is a branch node, since we can compress no further nodes
                var val = node.GetValueRef();
                // store the first value for later use
                if (i == 0)
                {
                    valueRef = val;
                    value = _data[val];
                }
                else if (!value!.CanCombine(_data[val]))
                    return; // exit if we can't combine any node with the first node, since we can compress no further nodes
            }
            
            // if we can compress this node, remove its parent's children
            DeleteChildren(RoundZValue(zValue, height + 1), height + 1, parentRef);
            // set the parent node to a leaf node referencing the value that is common among all 4 of its children
            _tree[parentRef] = new QuadtreeNode(valueRef);
            
            // try to compress the current node's parent
            height++;
        }
    }
    
    #endregion
    
    
    #region Serialization

    private const int HeaderSize = 18;
    
    /// <summary>
    /// Serializes this <see cref="Quadtree{T}"/> into a <see cref="Stream"/>, using the format described in
    /// src/Maths/Quadtree/quadtree-format.md.
    /// </summary>
    /// <param name="stream">the stream into which this <see cref="Quadtree{T}"/> will be serialized into</param>
    public void Serialize(Stream stream)
    {
        RequireFeatures(QuadtreeFeature.FileSerialization);

        var header = new MemoryStream();
        var tree = new MemoryStream();
        var data = new MemoryStream();
        
        var dataRefSize = MinByteCount((ulong)_data.Length); // size of a reference into the data section, in bytes
        
        // create the header (excluding data pointer)
        header.Write([(byte)MaxHeight]); // maxHeight
        header.Write(GetBytes(_enabledFeatures)); // features
        header.Write([(byte)dataRefSize]); // data ref size
        header.Write(GetBytes(((IFeatureFileSerialization<T>)_data[0]).SerializeLength)); // size of T
        
        // serialize the quadtree
        SerializeBody(tree, data, (int)dataRefSize);
        
        header.Write(GetBytes(HeaderSize + tree.Length)); // pointer to data section
        
        // copy the sections into the stream
        header.Position = 0;
        header.CopyTo(stream);
        header.Dispose();
        tree.Position = 0;
        tree.CopyTo(stream);
        tree.Dispose();
        data.Position = 0;
        data.CopyTo(stream);
        data.Dispose();
    }

    /// <summary>
    /// Serializes the body of this <see cref="Quadtree{T}"/> into a <see cref="Stream"/>. See <see cref="Serialize"/>
    /// for more information.
    /// </summary>
    /// <param name="treeStream">the stream which will contain the tree section</param>
    /// <param name="dataStream">the stream which will contain the data section</param>
    /// <param name="dataRefSize">the size (in bytes) of a reference into the data section (<paramref name="dataStream"/>)</param>
    private void SerializeBody(MemoryStream treeStream, MemoryStream dataStream, int dataRefSize)
    {
        var height = MaxHeight;
        UInt128 zValue = 0;
        var path = new long[MaxHeight]; // an array of all the nodes traversed through to get to the current node, indexed using height
        var nodeRef = 0L; // start at the root node
        
        var pathNextSibling = new int[MaxHeight]; // [height] = index (within parent) of the next sibling to be deleted
        
        // add initial value to path
        if (height != MaxHeight) path[height] = nodeRef;
        
        var data = new DynamicArray<byte[]>(Constants.QuadtreeArrayLength, false, false);
        
        while (true)
        {
            var node = _tree[nodeRef];
            
            // add node to stream
            if (node.Type == Branch)
            {
                // start a new branch node if we just entered this branch node for the first time
                if (pathNextSibling[height - 1] == 0)
                    treeStream.Write([0]); // start of a branch node
            }
            else
            {
                treeStream.Write([1]); // leaf node
                var value = ((IFeatureFileSerialization<T>)_data[node.GetValueRef()]).Serialize();
                long i;
                if (data.Contains(v => CompareBytes(v, value)))
                {
                    i = data.IndexOf(v => CompareBytes(v, value));
                }
                else
                {
                    i = data.Length;
                    data.Add(value);
                    dataStream.Write(value); // add serialized value
                }
                treeStream.Write(GetBytes(i).AsSpan()[^dataRefSize..]); // value reference
                
                // if this (leaf) node is the root node, exit since there are no more nodes to serialize
                if (height == MaxHeight) break;
            }
            
            // step down into branch nodes whose children have not been fully serialized
            if (node.Type == Branch && height != 0 && pathNextSibling[height - 1] < 4)
            {
                // step down into the node
                nodeRef = node.GetNodeRef(pathNextSibling[height - 1]);
                height--;
                // update the path to reflect what we just did
                path[height] = nodeRef;
                
                continue;
            }
            
            // increment our next sibling counter
            if (height != MaxHeight) pathNextSibling[height]++;
            
            // if this node's siblings have not been fully serialized, go to it's next sibling
            if (height != MaxHeight && pathNextSibling[height] < 4)
            {
                // calculate the next z-value
                zValue += (UInt128)0x1 << (2 * height);
                
                // get the parent node
                var parentNode = _tree[height == MaxHeight - 1 ? 0 : path[height + 1]];
                
                // get the next sibling's ref
                nodeRef = parentNode.GetNodeRef(pathNextSibling[height]);
                
                // update the path. nodes in the path with lower heights are now irrelevant and will be overridden when the time comes
                path[height] = nodeRef;
                
                // reset the sibling's next child counter
                if (height != 0) pathNextSibling[height - 1] = 0;
                
                continue;
            }
            
            // if we have just handled the last child of the root node, exit
            if (height == MaxHeight - 1 && pathNextSibling[height] == 4)
                break;
            
            // otherwise, reset this node's next sibling counter, increment its parent's, and go to this node's parent
            if (height != 0) pathNextSibling[height - 1] = 0;
            height++;
            nodeRef = path[height];
            zValue = RoundZValue(zValue, height);
        }
        
        data.Dispose();
    }
    
    /// <summary>
    /// Deserializes a <see cref="Quadtree{T}"/> in a <see cref="Stream"/> that has been serialized by
    /// <see cref="Serialize"/>, and returns the deserialized <see cref="Quadtree{T}"/>.
    /// </summary>
    /// <param name="stream">the stream that contains the serialized <see cref="Quadtree{T}"/></param>
    /// <param name="disableFeatureWarning">whether to disable the <see cref="Util.Warn"/> message printed to console if
    /// the feature set of the serialized <see cref="Quadtree"/> does not match the feature set of <see cref="T2"/></param>
    /// <typeparam name="T2">The type of the elements in the returned <see cref="Quadtree{T}"/></typeparam>
    /// <returns>the deserialized quadtree</returns>
    public static Quadtree<T2> Deserialize<T2>(Stream stream, bool disableFeatureWarning = false) where T2 : IQuadtreeElement<T2>, IFeatureFileSerialization<T2>
    {
        var enabledFeatures = 0u;
        if (typeof(T2).GetInterface(nameof(IFeatureModificationStore)) != null)
            enabledFeatures |= 0x1u << (int)QuadtreeFeature.ModificationStore;
        enabledFeatures |= 0x1u << (int)QuadtreeFeature.FileSerialization; // always enable file serialization
        if (typeof(T2).GetInterface(nameof(IFeatureElementColor)) != null)
            enabledFeatures |= 0x1u << (int)QuadtreeFeature.ElementColor;
        if (typeof(T2).GetInterface(nameof(IFeatureCellularAutomata)) != null)
            enabledFeatures |= 0x1u << (int)QuadtreeFeature.CellularAutomata;
        
        // get the header
        var header = new Span<byte>(new byte[HeaderSize]);
        stream.ReadExactly(header);
        
        // read the header
        var maxHeight = header[0]; // maxHeight
        var features = GetUint(header[1..5]); // enabled features
        var dataRefSize = (int)header[5]; // data ref size
        var tSize = GetInt(header[6..10]); // size of T
        var dataStart = GetLong(header[10..18]); // start of data section
        
        // compare the features in the stream to the ones specified in T2
        if (!disableFeatureWarning && enabledFeatures != features)
            Util.Warn($"Quadtree deserialized with feature set {enabledFeatures:b32}, but loading quadtree with " +
                      $"feature set {features:b32}. Overwriting feature set to {enabledFeatures:b32}");
        
        // construct the quadtree
        var q = new Quadtree<T2>(maxHeight, enabledFeatures);
        
        // populate the tree
        q.PopulateTree(stream, dataStart, dataRefSize);
        
        // populate the data
        stream.Position = dataStart;
        var valueBytes = new byte[tSize];
        for (var i = 0; i < stream.Length - dataStart; i+= tSize)
        {
            stream.ReadExactly(valueBytes);
            q._data.Add(T2.Deserialize(valueBytes));
        }
        
        // clean up
        header.Clear();
                
        // return the resulting quadtree
        return q;
    }
    
    /// <summary>
    /// Constructs a new <see cref="Quadtree{T}"/> with explicit maxHeight and features. Does not initialize the
    /// <see cref="_tree"/> or <see cref="_data"/> sections.
    /// </summary>
    private Quadtree(int maxHeight, uint enabledFeatures)
    {
        MaxHeight = maxHeight;
        _enabledFeatures = enabledFeatures;

        var storeModifications = CheckFeatures(QuadtreeFeature.ModificationStore);
        
        var halfSize = -1L << (MaxHeight - 1);
        Dimensions = NodeRangeFromPos(new Vec2<long>(halfSize), MaxHeight);
        
        // create tree and data arrays
        _tree = new DynamicArray<QuadtreeNode>(Constants.QuadtreeArrayLength, storeModifications);
        _data = new DynamicArray<T>(Constants.QuadtreeArrayLength, storeModifications);
        
        // create the modifications array
        if (storeModifications)
            _modifications = new DynamicArray<Range2D>(Constants.QuadtreeArrayLength, false, false);
    }

    /// <summary>
    /// Populates the <see cref="_tree"/> section of this <see cref="Quadtree{T}"/>, given a (correctly formatted)
    /// sequence of bytes.
    /// </summary>
    /// <param name="stream">the sequence of bytes containing at least the entire tree section, with the
    /// <see cref="Stream.Position"/> set to the start of the tree section</param>
    /// <param name="endIndex">the index of the last byte of the tree section in <paramref name="stream"/></param>
    /// <param name="dataRefSize">the size (in bytes) of a reference to a value in the data section of
    /// <paramref name="stream"/></param>
    /// <exception cref="InvalidNodeTypeException">thrown when an invalid <see cref="NodeType"/> is encountered in the
    /// <paramref name="stream"/>.</exception>
    private void PopulateTree(Stream stream, long endIndex, int dataRefSize)
    {
        // extract and populate the tree array
        var path = new long[MaxHeight];
        var pathNextSibling = new int[MaxHeight]; // [height] = index (within parent) of the next sibling to be added
        var height = MaxHeight;
        while (stream.Position <= endIndex)
        { 
            // read the next node's type and increment the position by 1 byte
            var type = (NodeType)stream.ReadByte();
            if ((int)type == -1) throw new InvalidQuadtreeSave("Node Type");
            
            long nodeRef;
            if (type == Branch)
            {
                // add the new branch node
                nodeRef = _tree.Add(new QuadtreeNode(-1, -1, -1, -1));
                
                // add the node to path
                if (height != MaxHeight)
                    path[height] = nodeRef;
            }
            else if (type == Leaf)
            {
                // read the leaf node's value ref and increment the position by `dataRefSize` bytes
                var valueRefB = new byte[dataRefSize];
                if (stream.Read(valueRefB) != dataRefSize) throw new InvalidQuadtreeSave("Value Ref");
                var valueRefBFull = new byte[8];
                valueRefB.CopyTo(valueRefBFull, 8 - valueRefB.Length);
                var valueRef = GetLong(valueRefBFull);
                                
                // create the leaf node
                nodeRef = _tree.Add(new QuadtreeNode(valueRef));
                
                // if this (leaf) node is the root node, exit since there are no more nodes to deserialize
                if (height == MaxHeight) break;
                
                // add the node to path
                if (height != MaxHeight)
                    path[height] = nodeRef;
                
            }
            else 
            {
                throw new InvalidNodeTypeException(type, "PopulateTree()/treeData");
            }
            
            // unless this node is the root node, modify the newly added node's parent to point to its new child
            if (height != MaxHeight)
            {
                // get the parent node
                var parentRef = height == MaxHeight-1 ? 0 : path[height + 1];
                var parent = _tree[parentRef];
                if (parent.Type == Leaf)
                    throw new InvalidNodeTypeException(parent.Type, Branch, "PopulateTree()/path");
                
                // modify its sibling references
                long[] siblings = [parent.Ref0, parent.Ref1, parent.Ref2, parent.Ref3];
                siblings[pathNextSibling[height]] = nodeRef;
                
                // replace the parent node
                _tree[parentRef] = new QuadtreeNode(siblings[0], siblings[1], siblings[2], siblings[3]);
            }
            
            // increment the next sibling counter
            if (height != MaxHeight) pathNextSibling[height]++;
            
            // if we added a branch node, step down into it
            if (type == Branch)
            {
                height--;
                continue;
            }
            
            // otherwise, if we have just added the last node in its parent,
            while (height != MaxHeight && pathNextSibling[height] == 4)
            {
                // reset the current node's next sibling counter
                pathNextSibling[height] = 0;
                
                // step into the parent node
                height++;
            }
        }
    }
    
    #endregion
    
    
    #region Quadtree Traversing

    /// <summary>
    /// Gets the node 4^<paramref name="height"/> spaces further in the Z-Order Curve of this <see cref="Quadtree{T}"/>.
    /// Useful when iterating through the entire quadtree. Updates the z-value, height, and node ref.
    /// </summary>
    /// <param name="zValue">the current z-value</param>
    /// <param name="height">the current height</param>
    /// <param name="maxZValue">the maximum z-value allowed for the next node</param>
    /// <param name="path">a ref to an array containing references to all the nodes traversed through to get to the current node</param>
    /// <returns>the updated <paramref name="zValue"/>, <paramref name="height"/>, and the index of the next node within <see cref="_tree"/></returns>
    private (UInt128 zValue, int height, long nodeRef) GetNextNode(UInt128 zValue, int height, UInt128 maxZValue, ref long[] path)
    {
        if (height == MaxHeight)
            return (0, MaxHeight, -1);
        
        // calculate difference to the next z-value
        var deltaZ = (UInt128)0x1 << (2 * height);

        var diff = maxZValue - zValue;
        
        // if this jump puts the z-value past the maxZValue, exit
        if (deltaZ > diff)
            return (0, 0, -1);
        
        
        // calculate the next z-value
        zValue += deltaZ;
        
        // calculate next height
        height = CalculateLargestHeight(zValue, MaxHeight);
        
        // get the next node's ref
        
        // backtrack to the lowest node that both the current node and the next node reside in. this will always be the
        // parent node of the next node
        // if the next node is one below the root node, its parent will be the root node, which is not in `path`
        var parentNodeRef = height == MaxHeight-1 ? 0 : path[height + 1];
        var parentNode = _tree[parentNodeRef];
        if (parentNode.Type == Leaf)
            throw new InvalidNodeTypeException(Leaf, Branch, "GetNextNode/parent node in path");
        
        // get the last "instruction" in the next z-value, which is the index within its parent node
        var nextNodeIndex = ZValueIndex(zValue, height);
        
        // get the next node's ref
        var nodeRef = parentNode.GetNodeRef(nextNodeIndex);
        
        // update the path. nodes in the path with lower heights are now irrelevant and will be overridden when the time comes
        path[height] = nodeRef;
        
        return (zValue, height, nodeRef);
    }
    
    /// <summary>
    /// Calculates the position of the next node that <see cref="GetNextNode"/> will select.
    /// </summary>
    /// <param name="nodePos">the position of the node</param>
    /// <param name="zValue">the z-value of the node</param>
    /// <param name="height">the height of the node</param>
    /// <returns>the position of the next node</returns>
    private Vec2<long> GetNextNodePos(Vec2<long> nodePos, UInt128 zValue, int height)
    {
        if (height >= MaxHeight)
            return (-1, -1);
        
        // get the index of this node within its parent
        var parentIndex = ZValueIndex(zValue, height);
        
        // get the size of this node
        var nodeSize = 0x1L << height; //TODO: will break with _maxHeight = 64
        
        switch (parentIndex)
        {
            case 0: nodePos += ( nodeSize, 0); break;
            case 1: nodePos += (-nodeSize, nodeSize); break;
            case 2: nodePos += ( nodeSize, 0); break;
            case 3: nodePos = Deinterleave(zValue + ((UInt128)0x1 << (2 * height)), MaxHeight); break;
        }
        
        return nodePos;
    }
    
    /// <summary>
    /// Traverses through the <see cref="Quadtree{T}"/> to find the index of a node within <see cref="_tree"/> that
    /// corresponds to the supplied z-value and height.
    /// </summary>
    /// <param name="zValue">the z-value of the target node</param>
    /// /// <param name="targetHeight">[optional] the height of the returned node. Defaults to 0. If <paramref name="readOnly"/> is set to
    /// true, the returned node may have a higher height</param>
    /// <param name="readOnly">[optional] when set to true, prevents the quadtree from being modified.
    /// If set, it is no longer guaranteed that a reference to a 1x1 node will be returned</param>
    /// <param name="path">[optional] an <see cref="int"/>[<see cref="MaxHeight"/> + 1] into which the nodes traversed
    /// through by this method will be stored, in reference form</param>
    /// <returns>An index within <see cref="_tree"/> that refers to a node of height <paramref name="targetHeight"/> (unless <paramref name="readOnly"/> is set to true)</returns>
    /// <exception cref="PositionOutOfBoundsException">Thrown when the supplied position does not reside within the quadtree</exception>
    private long GetNodeRef(UInt128 zValue, int targetHeight = 0, bool readOnly = false, long[] path = null)
    {
        // validate path parameter
        var usePath = path != null;
        if (usePath && path.Length != MaxHeight)
            throw new Exception($"Invalid path Length: Was: {path.Length}, Required: {MaxHeight}");
        
        // start at root node
        var nodeRef = 0L;
        for (var height = MaxHeight - 1; height >= targetHeight; height--)
        {
            var node = _tree[nodeRef];
            
            if (node.Type == Leaf)
            {
                // if we are not allowed to modify the quadtree, stop descending prematurely
                if (readOnly)
                    break;
                
                // otherwise, subdivide the leaf node and refresh `node`
                Subdivide(nodeRef);
                node = _tree[nodeRef];
            }
            
            // get the next node
            
            // extract the relevant 2-bit section from the z-value
            var zPart = ZValueIndex(zValue, height);
            
            // set the current node to the node within at index `zPart`
            nodeRef = node.GetNodeRef(zPart);
            
            // update the path if we need to
            if (usePath) path[height] = nodeRef;
        }
        
        // return the node
        return nodeRef;
    }
    
    #endregion
    
    #region Node Modification
    
    /// <summary>
    /// Subdivides a leaf node into 4 smaller leaf nodes with the same value, replacing the original node.
    /// </summary>
    /// <param name="nodeIndex">the index (within <see cref="_tree"/>) of the node to subdivide</param>
    /// <exception cref="InvalidNodeTypeException">Thrown when <paramref name="nodeIndex"/> does not reference a leaf node</exception>
    /// <remarks>The supplied node's index within <see cref="_tree"/> does not change.</remarks>
    private void Subdivide(long nodeIndex)
    {
        if (_tree[nodeIndex].Type != Leaf)
            throw new InvalidNodeTypeException(Branch, Leaf);
        
        // get the value of the original node
        var valueRef = _tree[nodeIndex].GetValueRef();
        
        // create 4 new leaf nodes with that value
        var i0 = _tree.Add(new QuadtreeNode(valueRef));
        var i1 = _tree.Add(new QuadtreeNode(valueRef));
        var i2 = _tree.Add(new QuadtreeNode(valueRef));
        var i3 = _tree.Add(new QuadtreeNode(valueRef));
        
        // assign them to the new branch node, replacing the original leaf node
        _tree[nodeIndex] = new QuadtreeNode(i0, i1, i2, i3);
    }
    
    /// <summary>
    /// Deletes all of a node's children.
    /// </summary>
    /// <param name="zValue">the z-value of the target node</param>
    /// <param name="height">the height of the target node</param>
    /// <param name="nodeRef">[optional, will be retrieved if not set] a reference to the node that will have its
    /// children deleted</param>
    /// <remarks>Does not call <see cref="DynamicArray{T}.Shrink()"/> on the <see cref="_tree"/> or <see cref="_data"/></remarks>
    private void DeleteChildren(UInt128 zValue, int height, long nodeRef = -1)
    {
        // nodes at height = 0 have no children
        if (height == 0)
            return;
        
        var maxHeight = height;
        var path = new long[MaxHeight]; // an array of all the nodes traversed through to get to the current node, indexed using height
        if (nodeRef == -1) nodeRef = GetNodeRef(zValue, height); // resolve nodeRef if none was given
        if (_tree[nodeRef].Type == Leaf) return; // if the target node is a leaf, it has no children and therefore none can be deleted
        var targetNode = nodeRef;
        
        var pathNextSibling = new int[MaxHeight]; // [height] = index (within parent) of the next sibling to be deleted
        
        // add initial value to path
        if (height != MaxHeight) path[height] = nodeRef;
        
        while (true)
        {
            var node = _tree[nodeRef];
            
            // step down into branch nodes whose children have not been fully deleted
            if (node.Type == Branch && height != 0 && pathNextSibling[height - 1] < 4)
            {
                // step down into the node
                nodeRef = node.GetNodeRef(pathNextSibling[height - 1]);
                height--;
                // update the path to reflect what we just did
                path[height] = nodeRef;
                
                continue;
            }
            
            // remove the node from _tree, without shrinking (done later, removing multiple calls to DynamicArray.Shrink())
            _tree.Remove(nodeRef, false);
            
            // TODO: remove values (each value contains usage count maybe?)
            
            // increment our next sibling counter
            pathNextSibling[height]++;
            
            // if we are at the target node and have just deleted its last child, exit
            if (height == maxHeight - 1 && pathNextSibling[height] == 4)
                break;
            
            // if this node's siblings have not been fully deleted, go to it's next sibling
            if (pathNextSibling[height] < 4)
            {
                // calculate the next z-value
                zValue += (UInt128)0x1 << (2 * height);
                
                // get the parent node
                var parentNode = _tree[height == MaxHeight - 1 ? 0 : path[height + 1]];
                
                // get the next sibling's ref
                nodeRef = parentNode.GetNodeRef(pathNextSibling[height]);
                
                // update the path. nodes in the path with lower heights are now irrelevant and will be overridden when the time comes
                path[height] = nodeRef;
                
                // reset the sibling's next child counter
                if (height != 0) pathNextSibling[height - 1] = 0;
                
                continue;
            }
            
            // otherwise, reset this node's next sibling counter, increment its parent's, and go to this node's parent
            if (height != 0) pathNextSibling[height - 1] = 0;
            height++;
            nodeRef = path[height];
            zValue = RoundZValue(zValue, height);
        }
        
        // replace the target node with a leaf node pointing to the default value
        _tree[targetNode] = new QuadtreeNode(0);
    }
    
    #endregion
    
    
    #region SVG Export
    
    /// <summary>
    /// Converts this QuadTree into an SVG.
    /// </summary>
    /// <returns>A string containing the SVG</returns>
    public string GetSvgMap()
    {
        var scale = DerivedConstants.QuadTreeSvgScale;
        
        // create the viewbox
        // viewbox is 2x as large as the actual svg
        var minX =   Dimensions.MinX * scale * 2m;
        var maxY = -(Dimensions.MinY * scale * 2m);
        var maxX =   Dimensions.MaxX * scale * 2m;
        var minY = -(Dimensions.MaxY * scale * 2m);
        var w = maxX - minX + 1;
        var h = maxY - minY + 1;
        
        var svgString = new StringBuilder(
            $"<svg viewBox=\"{minX} {minY} {w} {h}\">"
        );
        
        var maxZValue = Pow2Min1U128(MaxHeight * 2);
        UInt128 zValue = 0;
        var pos = Dimensions.Bl;
        var height = MaxHeight;
        var nodeRef = 0L;
        var path = new long[MaxHeight];
        
        while (true)
        {
            var node = _tree[nodeRef];
            
            // if the node is a branch node, step down into it without changing the z-value, and continue
            if (node.Type == Branch)
            {
                nodeRef = node.Ref0;
                height--;
                // update the path
                path[height] = nodeRef;
                
                continue;
            }
            
            // if the node is a leaf node, add it to the svg
            svgString.Append(GetNodeSvgRect(pos, height, nodeRef));
            
            // jump to the next node
            pos = GetNextNodePos(pos, zValue, height);
            (zValue, height, nodeRef) = GetNextNode(zValue, height, maxZValue, ref path);
            if (nodeRef == -1) break; // exit if we went through the entire quadtree
        }
        
        svgString.Append("<svg/>");
        
        var str = svgString.ToString();
        svgString.Clear();
        return str;
    }
    
    /// <summary>
    /// Converts a node (specified by z-value and height) into an SVG rect
    /// </summary>
    /// <param name="pos">the position of the bottom left coordinate of the node</param>
    /// <param name="height">the height of the node</param>
    /// <param name="nodeRef">[optional] a reference within <see cref="_tree"/> to the node</param>
    /// <returns>a string containing the SVG rect</returns>
    private string GetNodeSvgRect(Vec2<long> pos, int height, long nodeRef = 0)
    {
        // get the color/opacity of the node
        var fillColor = Color.White;
        var fillOpacity = 1.0;
        if (nodeRef != 0)
        {
            var value = _data[_tree[nodeRef].GetValueRef()];
            
            if (CheckFeatures(QuadtreeFeature.ElementColor))
            {
                fillColor = ((IFeatureElementColor)value).GetColor();
            }
            else
            {
                fillColor = Color.Lime;
                
                if (value.Equals(_data[0]))
                {
                    fillColor = Color.Red;
                    fillOpacity = 0.1;
                }
            }
        }
        
        var scale = DerivedConstants.QuadTreeSvgScale;
        
        var r = NodeRangeFromPos(pos, height);
        var minX =  r.MinX * scale;
        var minY = -r.MaxY * scale;
        var maxX = ( r.MaxX - 0) * scale;
        var maxY = (-r.MinY - 0) * scale;
        var w = maxX - minX + 1 * scale;
        var h = maxY - minY + 1 * scale;
        
        return $"<rect style=\"" +
               $"fill:{fillColor};fill-opacity:{fillOpacity};" +
               $"stroke:{(fillColor.Grayscale > 0 ? Color.Black : Color.White)};stroke-width:{Math.Min(w / 64m, 1 * scale)}\" " +
               $"width=\"{w}\" height=\"{h}\" " +
               $"x=\"{minX}\" y=\"{minY}\"/>";
    }
    
    #endregion
    
    
    #region Subset
    
    /// <summary>
    /// Creates a new <see cref="QuadtreeNode"/> that acts as a root node for a smaller subset of this entire <see cref="Quadtree{T}"/>.
    /// </summary>
    /// <param name="minRange">a <see cref="Range2D"/> that will always fit into the returned subset unless it goes outside the
    /// bounds of the <see cref="Quadtree{T}"/>, or <see cref="maxHeight"/> is too small. Used to specify position/size of the subset</param>
    /// <param name="maxHeight">[optional] the maximum height of the returned subset. Will be automatically set to the
    /// smallest possible value if not set. If set too low, the returned subset may not fully contain <paramref name="minRange"/></param>
    /// <returns>The resulting <see cref="QuadtreeNode"/> and its dimensions</returns>
    /// <exception cref="InvalidMaxHeightException">Thrown when <paramref name="maxHeight"/> is not valid (i.e. not 2-64)</exception>
    /// <remarks>Automatically prevents the returned subset from extending out of the <see cref="Quadtree{T}"/></remarks>
    public (QuadtreeNode Node, Range2D Range) GetSubset(Range2D minRange, int maxHeight = -1)
    {
        if (!Dimensions.Contains(minRange))
            throw new RangeOutOfBoundsException(minRange, Dimensions);
        
        if (maxHeight > MaxHeight)
            throw new InvalidHeightException(maxHeight, MaxHeight);
        
        // calculate minimum height needed if none is set
        if (maxHeight == -1)
        {
            var maxExt = minRange.MaxExtension;
            var log = BitOperations.Log2(maxExt);
            
            maxHeight = BitOperations.IsPow2(maxExt) ? log : log + 1;
        }
        
        if (maxHeight < 1) throw new InvalidHeightException(maxHeight, MaxHeight);
        
        // calculate the center, snapping it to the nearest `snapDistance`
        var snapDistance = 0x1L << (maxHeight - 1);
        var centerF = minRange.CenterF;
        var center = new Vec2<long>(
            (long)Math.Round(centerF.X / snapDistance) * snapDistance,
            (long)Math.Round(centerF.Y / snapDistance) * snapDistance
        );
        // clamp the center to ensure the resulting range does not go outside the bounds of the quadtree
        var centerMax = (long)(Dimensions.MaxExtension - (ulong)snapDistance);
        center = new Vec2<long>(
            long.Clamp(center.X, -centerMax, centerMax),
            long.Clamp(center.Y, -centerMax, centerMax));
        
        // create the range
        var subsetRange = new Range2D(center, 0x1uL << maxHeight);

        // get the child nodes of the subset root
        var ref0 = GetNodeRef(Interleave(center + (-1, -1), MaxHeight), maxHeight-1, true);
        var ref1 = GetNodeRef(Interleave(center + (+1, -1), MaxHeight), maxHeight-1, true);
        var ref2 = GetNodeRef(Interleave(center + (-1, +1), MaxHeight), maxHeight-1, true);
        var ref3 = GetNodeRef(Interleave(center + (+1, +1), MaxHeight), maxHeight-1, true);
        
        // construct the subset root node
        QuadtreeNode subsetRoot;
        if (ref0 == ref1 && ref0 == ref2 && ref0 == ref3)
            subsetRoot = new QuadtreeNode(_tree[ref0].GetValueRef());
        else
            subsetRoot = new QuadtreeNode(ref0, ref1, ref2, ref3);
        
        return (subsetRoot, subsetRange);
    }
    
    /// <summary>
    /// Updates the internally stored subset. Calling this method on a frequently accessed region
    /// of the <see cref="Quadtree{T}"/> increases read/write speed for that region by acting as a cache.
    /// Moves the subset every time it is called.
    /// </summary>
    public void UpdateSubset(Range2D minRange)
    {
        (_subset, _subsetDimensions) = GetSubset(minRange);
    }
    
    #endregion
    
    #region Indexers
    
    /// <summary>
    /// Gets or sets a single element in the <see cref="Quadtree{T}"/>
    /// </summary>
    /// <param name="pos">the element to get or set</param>
    public T this[Vec2<long> pos]
    {
        get => Get(pos);
        set => Set(pos, value);
    }
    
    /// <summary>
    /// Gets or sets a single element in the <see cref="Quadtree{T}"/>
    /// </summary>
    /// <param name="x">the X coordinate of the element to get or set</param>
    /// <param name="y">the Y coordinate of the element to get or set</param>
    public T this[long x, long y]
    {
        get => Get((x, y));
        set => Set((x, y), value);
    }
    
    /// <summary>
    /// Sets every point in within a <see cref="Range2D"/> to the supplied value.
    /// </summary>
    /// <param name="range">the <see cref="Range2D"/> describing which elements to set</param>
    public T this[Range2D range]
    {
        set => Set(range, value);
    }
    
    #endregion
    
    
    #region Public Util

    /// <summary>
    /// Gets the lengths of the <see cref="_tree"/> and <see cref="_data"/> sections in this <see cref="Quadtree{T}"/>.
    /// </summary>
    public (long treeLength, long dataLength) GetLength()
    {
        return (_tree.ModificationLength, _data.ModificationLength);
    }
    
    /// <summary>
    /// Gets the modifications that have been done to this <see cref="Quadtree{T}"/> since the last time this
    /// method was called.
    /// </summary>
    public void GetModifications(DynamicArray<ArrayModification<QuadtreeNode>> tree, DynamicArray<ArrayModification<T>> data)
    {
        RequireFeatures(QuadtreeFeature.ModificationStore);
        
        _tree.GetModifications(tree);
        _data.GetModifications(data);
    }
    
    /// <summary>
    /// Computes the hash code of this <see cref="Quadtree{T}"/>. Only factors in <see cref="_tree"/> and <see cref="_data"/>.
    /// </summary>
    public override int GetHashCode()
    {
        return _tree.GetHashCode() ^ _data.GetHashCode();
    }
    
    /// <summary>
    /// Frees up the memory consumed by this <see cref="Quadtree{T}"/>.
    /// </summary>
    public void Dispose()
    {
        _tree.Dispose();
        _data.Dispose();
        
        GC.SuppressFinalize(this);
    }
    
    #endregion

    

    #region Private Util
    
    /// <summary>
    /// Checks whether each supplied feature is enabled in this <see cref="Quadtree{T}"/>. If any are not enabled, returns false.
    /// </summary>
    /// <param name="features">all the features that need to be enabled</param>
    private bool CheckFeatures(params QuadtreeFeature[] features)
    {
        foreach (var feature in features)
        {
            if (((_enabledFeatures >> (int)feature) & 0x1) == 0)
                return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Checks whether each supplied feature is enabled in this <see cref="Quadtree{T}"/>. If any are not enabled, throws a <see cref="DisabledFeatureException"/>.
    /// </summary>
    /// <param name="features">all the features that need to be enabled</param>
    private void RequireFeatures(params QuadtreeFeature[] features)
    {
        foreach (var feature in features)
        {
            if (((_enabledFeatures >> (int)feature) & 0x1) == 0)
                throw new DisabledFeatureException(feature);
        }
    }
    
    #endregion
    
    #region Exceptions
    
    private class DisabledFeatureException(QuadtreeFeature requiredFeature) : Exception(
        $"Feature \"{requiredFeature}\" is disabled, but is required for the operation");
    
    private class InvalidMaxHeightException(int maxHeight) : Exception(
        $"Invalid Max Height. Was: {maxHeight}, Range: 2-64");
    
    private class InvalidHeightException(int height, int maxHeight) : Exception(
        $"Invalid Height. Was: {height}, Range: 0-{maxHeight}");
    
    private class RangeOutOfBoundsException(Range2D range, Range2D bound) : Exception(
        $"Range {range} is outside QuadTree bounds of {bound}");
    
    private class PositionOutOfBoundsException(Vec2<long> pos, Range2D bound) : Exception(
        $"Position {pos} is Outside QuadTree Bounds of {bound}");
    
    #endregion
    
    
}

/// <summary>
/// Stores the indexes of bits within <see cref="Quadtree{T}._enabledFeatures"/> that correspond to different features.
/// </summary>
public enum QuadtreeFeature : uint
{
    ModificationStore = 0,
    FileSerialization = 1,
    ElementColor = 2,
    CellularAutomata = 3
}

/// <summary>
/// An exception that is thrown when a <see cref="QuadtreeNode"/>'s <see cref="QuadtreeNode.Type"/> is not as expected.
/// </summary>
public class InvalidNodeTypeException : Exception
{
    public InvalidNodeTypeException(NodeType type, NodeType expectedType) :
        base($"Invalid node type found. Found: {type} Expected: {expectedType}") {}
    
    public InvalidNodeTypeException(NodeType type) : base($"Node Type {type} is not valid") {}
    
    public InvalidNodeTypeException(NodeType type, NodeType expectedType, string location) :
        base($"Invalid node type found. Found a {type} node, but expected a {expectedType} node in {location}") {}
    
    public InvalidNodeTypeException(NodeType type, string location) :
        base($"Node Type {type} is not valid. Found in {location}") {}
}

public class InvalidQuadtreeSave(string value) : Exception($"Unable to fully read {value} from save file");
