using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Sandbox2D.Maths.Quadtree.FeatureNodeTypes;
using static Sandbox2D.Maths.Quadtree.QuadtreeUtil;
using static Sandbox2D.Maths.Quadtree.NodeType;
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
    private readonly int _maxHeight;
    
    /// <summary>
    /// The dimensions of the entire quadtree.
    /// </summary>
    public readonly Range2D Dimensions;
    
    /// <summary>
    /// The dimensions of the subset node (stored in <see cref="_tree"/>[1]).
    /// </summary>
    private Range2D _subsetDimensions; // TODO: actually implement the subset
    
    /// <summary>
    /// Stores which features have been enabled. Indexed using <see cref="QuadtreeFeature"/>.
    /// </summary>
    private readonly bool[] _enabledFeatures;


    /// <summary>
    /// Constructs a new <see cref="Quadtree{T}"/>.
    /// </summary>
    /// <param name="maxHeight">the maximum height of the quadtree, where width = 2^maxHeight. range: 2-64</param>
    /// <param name="defaultValue">the default value that everything will be initialized to</param>
    /// <param name="storeModifications">[optional] whether to store modifications. Calling <see cref="GetModifications"/> will
    /// throw an exception if this is disabled</param>
    /// <exception cref="InvalidMaxHeightException">Thrown when <paramref name="maxHeight"/> is not valid (ie. not 2-64)</exception>
    public Quadtree(int maxHeight, T defaultValue, bool storeModifications = false)
    {
        if (maxHeight is < 2 or > 64)
            throw new InvalidMaxHeightException(maxHeight);
        
        _maxHeight = maxHeight;
        
        var halfSize = 0x1L << (maxHeight - 1); //TODO: will break if _maxHeight = 64
        Dimensions = NodeRangeFromPos((-halfSize, -halfSize), _maxHeight); //TODO: don't like 64-part of this NodeRangeFromPos() implementation
        Console.WriteLine(Dimensions);
        Console.WriteLine(Dimensions.MaxExtension);
        
        const int arrLen = 2048;
        
        _tree = new DynamicArray<QuadtreeNode>(arrLen, storeModifications);
        _data = new DynamicArray<T>(arrLen, storeModifications);
        
        _data.Add(defaultValue);
        
        // initialize the tree
        var rootNode = new QuadtreeNode(0);
        _tree.Add(rootNode); // create the root
        _tree.Add(rootNode); // create a space for the subset root
        Subdivide(0); // subdivide root
        _tree[1] = _tree[0]; // clone root and put it into the space for subset root

        _modifications = new DynamicArray<Range2D>(arrLen, false, false);
        
        // figure out which features to enable
        
        _enabledFeatures = new bool[Enum.GetNames(typeof(QuadtreeFeature)).Length];
        
        if (defaultValue is IFeatureCellularAutomata)
            _enabledFeatures[(int)QuadtreeFeature.CellularAutomata] = true;
        if (defaultValue is IFeatureFileSerialization)
            _enabledFeatures[(int)QuadtreeFeature.FileSerialization] = true;
        if (defaultValue is IFeatureElementColor)
            _enabledFeatures[(int)QuadtreeFeature.ElementColor] = true;

    }
    
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
    
    /// <summary>
    /// Returns the value at the supplied position.
    /// </summary>
    /// <param name="pos">the position</param>
    private T Get(Vec2<long> pos)
    {
        var node = GetNodeRef(pos, true);
        
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
        var node = GetNodeRef(pos);
        
        // overwrite the node with the new value
        _tree[node] = new QuadtreeNode(_data.Add(value));
        
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
        
        _modifications.Add(range);
        
        // store the `value` and keep a reference to it
        var valueRef = value == null ? 0 : _data.Add(value);
        var valueNode = new QuadtreeNode(valueRef);
        
        var maxZValue = Interleave(range.Tr, _maxHeight); // the z-value of the last node to be traversed
        var path = new int[_maxHeight]; // an array of all the nodes traversed through to get to the current node, indexed using height
        // set up variables, starting at the bottom-left-most point in the `range`
        var nodePos = range.Bl; // the position of the current node
        var zValue = Interleave(range.Bl, _maxHeight); // the z-value of the current node
        var height = CalculateLargestHeight(zValue, _maxHeight); // the height of the current node
        var nodeRef = GetNodeRef(nodePos, false, height, path); // a reference to the current node
        
        while (true)
        {
            // if we have gone through the entire `range`, exit the loop
            // if (zValue > maxZValue) break;
            
            var nodeRange = NodeRangeFromPos(nodePos, height);
            var node = _tree[nodeRef];
            
            // if the node is only partially contained or is the root node, step down into it, narrowing the area we can modify
            if ((range.Overlaps(nodeRange) && !range.Contains(nodeRange)) || height == _maxHeight)
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
                    if (node.Type == Branch)
                    {
                        DeleteChildren(nodeRef);
                    }
                    // replace the node with a copy of `valueNode`, containing the value to set
                    _tree[nodeRef] = valueNode;
                }
                
                // calculate the position of the next node
                nodePos = GetNextNodePos(nodePos, zValue, height);
                
                // jump to the next node
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
        Set(Dimensions, _data[0]);
        _modifications.Clear();
    }
    
    
    /// <summary>
    /// Tries to combine as many nodes as possible in this <see cref="Quadtree{T}"/>, which are referenced in <see cref="_modifications"/>.
    /// </summary>
    public void Compress()
    {
        // exit if there have been no modifications since the last time this method was called
        if (_modifications.Length == 0)
            return;
        
        _modifications.Sort(RangeBlComparer);
        
        // combine the modification to reduce the amount of times the same area is compressed multiple times
        var combinedMods = new List<Range2D>(_modifications.Length) { _modifications[0] };
        for (var i = 1; i < _modifications.Length; i++)
        {
            var modification = _modifications[i];
            
            var last = combinedMods[^1];
            if (last.Overlaps(modification))
            {
                var comb = new Range2D(last.Bl, modification.Tr);
                
                // if combining the modifications doesn't create too much extra area, combine them
                if (comb.Area <= last.Area + modification.Area - comb.Area)
                {
                    combinedMods[^1] = comb;
                    continue;
                }
            }
            
            // otherwise, just add the area separately
            combinedMods.Add(modification);
        }
        
        foreach (var modification in combinedMods)
        {
            CompressRange(modification);
        }
        
        _modifications.Clear();
        combinedMods.Clear();
    }
    
    /// <summary>
    /// Tries to combine as many identical nodes as possible in a section of this <see cref="Quadtree{T}"/>.
    /// </summary>
    /// <param name="range">the section to compress</param>
    private void CompressRange(Range2D range)
    {
        var maxZValue = ~(_maxHeight == 64 ? 0 : ~(UInt128)0x0 << (2 * _maxHeight)); // the z-value of the last node to be traversed
        var path = new int[_maxHeight]; // an array of all the nodes traversed through to get to the current node, indexed using height
        
        // set up variables, starting at the bottom-left-most point in the `range`
        var zValue = Interleave(range.Bl, _maxHeight); // the z-value of the current node
        var height = CalculateLargestHeight(zValue, _maxHeight); // the height of the current node
        var nodeRef = GetNodeRef(range.Bl, false, height, path); // a reference to the current node
        
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
    /// Tries to compress a single node and all of its parents.
    /// </summary>
    /// <param name="zValue">the z-value of the node</param>
    /// <param name="height">the height of the node</param>
    /// <param name="path">an array containing all the nodes traversed through to get to this node, indexed using height</param>
    /// <param name="forceFull">whether to compress every node regardless of if it is the last node in its parent</param>
    /// <exception cref="InvalidNodeTypeException">thrown when a node in <paramref name="path"/> has a parent node that is a <see cref="NodeType.Leaf"/> node</exception>
    private void CompressNode(UInt128 zValue, int height, int[] path, bool forceFull = false)
    {
        // try to compress as much as possible
        while (true)
        {
            // don't compress the root node or its immediate children since they are needed for subsets
            if (height >= _maxHeight-1) break;
            
            // if this not is the last node in its parent, don't try to compress it, since it's parents will be reached
            // by CompressRange() later anyway
            if (ZValueIndex(zValue, height) != 3 && !forceFull) return;
            
            // get the parent
            var parentRef = path[height + 1];
            var parent = _tree[parentRef];
            
            // if the parent node is a leaf, something went very wrong
            if (parent.Type == Leaf) throw new InvalidNodeTypeException(Leaf, Branch, "CompressNode/path");
            
            // get the node's siblings' value refs
            var valueRef = 0;
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
                    continue;
                }
                if (value == null) return;
                if (!value.CanCombine(_data[val])) return; // exit if we can't combine any node with the first node, since we can compress no further nodes
            }
            
            // if we can compress this node, remove its children
            DeleteChildren(parentRef);
            // set the parent node to a leaf node referencing the value that is common among all 4 of its children
            _tree[parentRef] = new QuadtreeNode(valueRef);
            
            // try to compress the current node's parent
            height++;
        }
    }
    
    
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
        
        if (maxHeight > _maxHeight)
            throw new InvalidHeightException(maxHeight, _maxHeight);
        
        // calculate minimum height needed if none is set
        if (maxHeight == -1)
        {
            var maxExt = minRange.MaxExtension;
            var log = BitOperations.Log2(maxExt);
            
            maxHeight = BitOperations.IsPow2(maxExt) ? log : log + 1;
        }
        
        if (maxHeight < 1) throw new InvalidHeightException(maxHeight, _maxHeight);
        
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
        var ref0 = GetNodeRef(center + (-1, -1), true, maxHeight-1);
        var ref1 = GetNodeRef(center + (+1, -1), true, maxHeight-1);
        var ref2 = GetNodeRef(center + (-1, +1), true, maxHeight-1);
        var ref3 = GetNodeRef(center + (+1, +1), true, maxHeight-1);
        
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
        (_tree[1], _subsetDimensions) = GetSubset(minRange);
    }
    
    /// <summary>
    /// Gets the lengths of the <see cref="_tree"/> and <see cref="_data"/> sections in this <see cref="Quadtree{T}"/>.
    /// </summary>
    public (int treeLength, int dataLength) GetLength()
    {
        return (_tree.ModificationLength, _data.ModificationLength);
    }
    
    /// <summary>
    /// Gets the modifications that have been done to this <see cref="Quadtree{T}"/> since the last time this
    /// method was called.
    /// </summary>
    /// <returns>a <see cref="QuadtreeModifications{T}"/> struct</returns>
    public QuadtreeModifications<T> GetModifications()
    {
        return new QuadtreeModifications<T>(_tree.GetModifications(), _data.GetModifications());
    }
    
    /// <summary>
    /// NYI
    /// </summary>
    /// <param name="file"></param>
    /// <exception cref="NotImplementedException">thrown.</exception>
    public void Serialize(FileStream file)
    {
        RequireFeatures(QuadtreeFeature.FileSerialization);
        
        throw new NotImplementedException();
    }
    
    /// <summary>
    /// NYI
    /// </summary>
    /// <param name="file"></param>
    /// <param name="defaultValue"></param>
    /// <param name="storeModifications"></param>
    /// <returns></returns>
    /// <exception cref="Quadtree{T}.DisabledFeatureException"></exception>
    /// <exception cref="NotImplementedException">thrown.</exception>
    public static Quadtree<T> Deserialize(FileStream file, T defaultValue, bool storeModifications)
    {
        var enabledFeatures = new bool[Enum.GetNames(typeof(QuadtreeFeature)).Length];
        if (defaultValue is IFeatureCellularAutomata)
            enabledFeatures[(int)QuadtreeFeature.CellularAutomata] = true;
        if (defaultValue is IFeatureFileSerialization)
            enabledFeatures[(int)QuadtreeFeature.FileSerialization] = true;
        
        if (!enabledFeatures[(int)QuadtreeFeature.FileSerialization])
            throw new DisabledFeatureException(QuadtreeFeature.FileSerialization);
        
        throw new NotImplementedException();
    }
    
    /// <summary>
    /// Converts this QuadTree into an SVG.
    /// </summary>
    /// <returns>A string containing the SVG</returns>
    public string GetSvgMap()
    {
        const decimal scale = DerivedConstants.QuadTreeSvgScale;
        
        // create the viewbox
        // viewbox is 2x as large as the actual svg
        var minX =  Dimensions.MinX * scale * 2m;
        var maxY = -Dimensions.MinY * scale * 2m;
        var maxX =  Dimensions.MaxX * scale * 2m;
        var minY = -Dimensions.MaxY * scale * 2m;
        var w = maxX - minX + 1;
        var h = maxY - minY + 1;
        
        var svgString = new StringBuilder(
            $"<svg viewBox=\"{minX} {minY} {w} {h}\">"
        );

        var maxZValue = (UInt128)0x1 << (2 * _maxHeight); //TODO: will break if _maxHeight = 64
        UInt128 zValue = 0;
        var pos = Dimensions.Bl;
        var height = _maxHeight;
        var nodeRef = 0;
        var path = new int[_maxHeight];
        
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
    private string GetNodeSvgRect(Vec2<long> pos, int height, int nodeRef = 0)
    {
        // get the color/opacity of the node
        var fillColor = Color.White;
        var fillOpacity = 1.0;
        if (nodeRef != 0)
        {
            var value = _data[_tree[nodeRef].GetValueRef()];
            
            if (_enabledFeatures[(int)QuadtreeFeature.ElementColor])
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
        
        const decimal scale = DerivedConstants.QuadTreeSvgScale;
        
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

    /// <summary>
    /// Gets the node 4^<paramref name="height"/> spaces further in the Z-Order Curve of this <see cref="Quadtree{T}"/>.
    /// Useful when iterating through the entire quadtree. Updates the z-value, height, and node ref.
    /// </summary>
    /// <param name="zValue">the current z-value</param>
    /// <param name="height">the current height</param>
    /// <param name="maxZValue">the maximum z-value allowed for the next node</param>
    /// <param name="path">a ref to an array containing references to all the nodes traversed through to get to the current node</param>
    /// <returns>the updated <paramref name="zValue"/>, <paramref name="height"/>, and the index of the next node within <see cref="_tree"/></returns>
    private (UInt128 zValue, int height, int nodeRef) GetNextNode(UInt128 zValue, int height, UInt128 maxZValue, ref int[] path)
    {
        if (height == _maxHeight)
            return (0, _maxHeight, -1);
        
        // calculate difference to the next z-value
        var deltaZ = (UInt128)0x1 << (2 * height);
        
        // if this jump puts the z-value past the maxZValue, exit
        if (deltaZ >= maxZValue - zValue)
            return (0, 0, -1);
        
        // calculate the next z-value
        zValue += deltaZ;
        
        // calculate next height
        height = CalculateLargestHeight(zValue, _maxHeight);
        
        // get the next node's ref
        
        // backtrack to the lowest node that both the current node and the next node reside in. this will always be the
        // parent node of the next node
        // if the next node is one below the root node, its parent will be the root node, which is not in `path`
        var parentNodeRef = height == _maxHeight-1 ? 0 : path[height + 1];
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
        if (height >= _maxHeight)
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
            case 3: nodePos = Deinterleave(zValue + ((UInt128)0x1 << (2 * height)), _maxHeight); break;
        }
        
        return nodePos;
    }

    /// <summary>
    /// Traverses through the <see cref="Quadtree{T}"/> to find the index of the node within <see cref="_tree"/> that
    /// corresponds to the supplied position.
    /// </summary>
    /// <param name="pos">the supplied position</param>
    /// <param name="readOnly">[optional] when set to true, prevents the quadtree from being modified.
    /// If set, it is no longer guaranteed that a reference to a 1x1 node will be returned</param>
    /// <param name="targetHeight">[optional] the height of the returned node. Defaults to 0. If <paramref name="readOnly"/> is set to
    /// true, the returned node may have a higher height</param>
    /// <param name="path">[optional] an <see cref="int"/>[<see cref="_maxHeight"/> + 1] into which the nodes traversed
    /// through by this method will be stored, in reference form</param>
    /// <returns>An index within <see cref="_tree"/> that refers to a node of height <paramref name="targetHeight"/> (unless <paramref name="readOnly"/> is set to true)</returns>
    /// <exception cref="PositionOutOfBoundsException">Thrown when the supplied position does not reside within the quadtree</exception>
    private int GetNodeRef(Vec2<long> pos, bool readOnly = false, int targetHeight = 0, int[] path = null)
    {
        // validate pos parameter
        if (!Dimensions.Contains(pos)) throw new PositionOutOfBoundsException(pos, Dimensions);
        
        // validate path parameter
        var usePath = path != null;
        if (usePath && path.Length != _maxHeight)
            throw new Exception($"Invalid path Length: Was: {path.Length}, Required: {_maxHeight}");
        
        // calculate the position's z-value
        var zValue = Interleave(pos, _maxHeight);
        
        // start at root node
        var nodeRef = 0;
        for (var height = _maxHeight - 1; height >= targetHeight; height--)
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
    
    /// <summary>
    /// Subdivides a leaf node into 4 smaller leaf nodes with the same value, replacing the original node.
    /// </summary>
    /// <param name="nodeIndex">the index (within <see cref="_tree"/>) of the node to subdivide</param>
    /// <exception cref="InvalidNodeTypeException">Thrown when <paramref name="nodeIndex"/> does not reference a leaf node</exception>
    /// <remarks>The supplied node's index within <see cref="_tree"/> does not change.</remarks>
    private void Subdivide(int nodeIndex)
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
    /// Removes a node from the <see cref="Quadtree{T}"/>
    /// </summary>
    /// <param name="nodeRef">a reference to the node to delete</param>
    /// <param name="recursive">whether to recursively delete all the nodes children</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private void DeleteNode(int nodeRef, bool recursive = false)
    {
        var node = _tree[nodeRef];
        
        if (recursive && node.Type == Branch)
            DeleteChildren(nodeRef);

        // TODO: remove values (each value contains usage count maybe?)
        _tree.Remove(nodeRef);
    }
    
    /// <summary>
    /// Deletes all of a node's children.
    /// </summary>
    /// <param name="parentNode">a reference to the node that will have its children deleted</param>
    private void DeleteChildren(int parentNode)
    {
        var nodeRef = parentNode; // a reference to the current node
        var height = _maxHeight; // the current height, relative to the parentNode parameter
        
        var path = new int[_maxHeight + 1]; // an array of all the nodes traversed through to get to the current node, indexed using height. Includes the `parentNode`
        var nextChildIndexes = new int[_maxHeight + 1]; // an array containing, for each branch node in `path`, the index of the next child to be deleted
        
        while (true)
        {
            // exit if we are at the root node, and we have fully explored all of its children
            if (height == _maxHeight && nextChildIndexes[height] == 4)
                break;
            
            path[height] = nodeRef;
            var node = _tree[nodeRef];
            
            switch (node.Type)
            {
                // if the node is a branch node that has not been fully deleted, step down into it
                case Branch when nextChildIndexes[height] < 4:
                    nodeRef = node.GetNodeRef(nextChildIndexes[height]);
                    height--;
                    continue;
                // if the node is a leaf node that is below the root node and its immediate children, delete it
                case Leaf when height < _maxHeight-2:
                    DeleteNode(nodeRef);
                    break;
            }
            
            // update child indexes
            nextChildIndexes[height+1]++;
            nextChildIndexes[height] = 0;
            
            // step up into the parent node
            height++;
            nodeRef = path[height];
        }
    }
    
    
    /// <summary>
    /// Checks whether each supplied feature is enabled in this <see cref="Quadtree{T}"/>. If any are not enabled, throws a <see cref="DisabledFeatureException"/>.
    /// </summary>
    /// <param name="features">all the features that need to be enabled</param>
    private void RequireFeatures(params QuadtreeFeature[] features)
    {
        foreach (var feature in features)
        {
            if (!_enabledFeatures[(int)feature])
                throw new DisabledFeatureException(feature);
        }
    }
    
    
    /// <summary>
    /// Computes the hash code of this <see cref="Quadtree{T}"/>. Only factors in <see cref="_tree"/> and <see cref="_data"/>.
    /// </summary>
    public override int GetHashCode()
    {
        return _tree.GetHashCode() ^ _data.GetHashCode();
    }
    
    /// <summary>
    /// Computes the hash code of the <see cref="_tree"/> section of this <see cref="Quadtree{T}"/>.
    /// </summary>
    public Hash GetTreeHash()
    {
        return _tree.Hash(2);
    }
    
    /// <summary>
    /// Computes the hash code of the <see cref="_data"/> section of this <see cref="Quadtree{T}"/>.
    /// </summary>
    public Hash GetDataHash()
    {
        return _data.Hash(2);
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
    
    private enum QuadtreeFeature
    {
        FileSerialization = 0,
        ElementColor = 1,
        CellularAutomata = 2
    }
    
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
}


/// <summary>
/// A struct containing 2 arrays of <see cref="ArrayModification{T}"/>s for the
/// <see cref="Quadtree{T}._tree"/>/<see cref="Quadtree{T}._data"/> sections of a <see cref="Quadtree{T}"/>.
/// </summary>
/// <param name="tree">the modifications to the tree section</param>
/// <param name="data">the modifications of the data section</param>
/// <typeparam name="T">the type of the elements in the data section</typeparam>
public struct QuadtreeModifications<T> (ArrayModification<QuadtreeNode>[] tree, ArrayModification<T>[] data)
{
    public readonly ArrayModification<QuadtreeNode>[] Tree = tree;
    public readonly ArrayModification<T>[] Data = data;
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
}