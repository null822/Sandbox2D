using System.Numerics;
using Math2D.Binary;
using Math2D.Quadtree.Features;
using static Math2D.Quadtree.QuadtreeUtil;
using static Math2D.Quadtree.NodeType;
using static Math2D.Binary.BitUtil;

namespace Math2D.Quadtree;

/// <summary>
/// Represents a Quadtree, able to store a large amount of 2D data in recursive 2x2 <see cref="QuadtreeNode"/>s.<br></br>
/// Fully extensible by using the <see cref="QuadtreeFeature{T}"/> system. See <see cref="SerializableQuadtree{T}"/> for an example.
/// </summary>
/// <typeparam name="T">The type of the data to store.</typeparam>
public sealed class Quadtree<T> : IDisposable where T : IQuadtreeElement<T>
{
    // TODO: implement subsets
    // TODO: implement line setting
    // TODO: implement Quadtree pasting
    // TODO: implement Cellular Automata

    /// <summary>
    /// A <see cref="DynamicArray{T}"/> of all the <see cref="QuadtreeNode"/>s within this <see cref="Quadtree{T}"/>, linking to each other and, for leaf nodes, linking to <see cref="Data"/>.
    /// </summary>
    /// <remarks>The root node of this <see cref="Quadtree{T}"/> is always at index 0, and the subset root is always at
    /// index 1.</remarks>
    /// 
    internal DynamicArray<QuadtreeNode> Tree { get; }
    /// <summary>
    /// A <see cref="DynamicArray{T}"/> of all the values of every leaf node. Linked to by every leaf node in <see cref="Tree"/>.
    /// </summary>
    /// <remarks>The default value for unset elements in this <see cref="Quadtree{T}"/> is always stored in index 0.</remarks>
    internal DynamicArray<T> Data { get; }
    
    /// <summary>
    /// Equal to the height of the root node, or, one more than the maximum height of any non-root node. Also equal to
    /// the maximum amount of recursive nodes in this <see cref="Quadtree{T}"/>, excluding the root node.
    /// </summary>
    public int MaxHeight { get; }
    
    /// <summary>
    /// The dimensions of the entire quadtree.
    /// </summary>
    public Range2D Dimensions { get; }
    
    /// <summary>
    /// The modifications that have been done to this <see cref="Quadtree{T}"/> since the last time <see cref="Compress"/> was called.
    /// </summary>
    private readonly DynamicArray<Range2D> _modifications = null!;
    
    /// <summary>
    /// Whether modifications are stored.
    /// </summary>
    public readonly bool StoreModifications;
    
    /// <summary>
    /// Constructs a new <see cref="Quadtree{T}"/>.
    /// </summary>
    /// <param name="maxHeight">the maximum height of the <see cref="Quadtree{T}"/>, where width = 2^<paramref name="maxHeight"/>. Range: 2-64</param>
    /// <param name="defaultValue">the default value that everything will be initialized to</param>
    /// <param name="storeModifications">whether to store modifications. <see cref="GetModifications"/> will not work
    /// without this, and <see cref="Compress"/> will be more optimised (skips unchanged areas)</param>
    /// <exception cref="InvalidMaxHeightException">thrown when <paramref name="maxHeight"/> is not in the
    /// acceptable range.</exception>
    public Quadtree(int maxHeight, T defaultValue, bool storeModifications = false)
    {
        // validate maxHeight
        if (maxHeight is < 2 or > 64)
            throw new InvalidMaxHeightException(maxHeight);
        
        StoreModifications = storeModifications;
        
        // assign maxHeight and dimensions
        MaxHeight = maxHeight;
        var bl = -1L << (MaxHeight - 1);
        Dimensions = NodeRangeFromPos(new Vec2<long>(bl), MaxHeight);
        
        // create tree and data arrays
        Tree = new DynamicArray<QuadtreeNode>(QuadtreeNode.MaxChunkSize, storeModifications);
        Data = new DynamicArray<T>(T.MaxChunkSize, storeModifications);
        
        // initialize the data section
        Data.Add(defaultValue);
        
        // initialize the tree section
        Tree.Add(new QuadtreeNode(0));
        
        // create modifications array
        if (storeModifications)
            _modifications = new DynamicArray<Range2D>(25_00 /* 80kb per chunk */, false, false);
    }
    
    /// <summary>
    /// Constructs a new <see cref="Quadtree{T}"/> from its base properties. Intended for use by feature classes
    /// (see <see cref="Quadtree{T}"/>) when creating new <see cref="Quadtree{T}"/>s.
    /// Use <see cref="Quadtree{T}(int,T,bool)"/> if this is not your intention.
    /// </summary>
    /// <param name="tree">the tree section of the <see cref="Quadtree{T}"/></param>
    /// <param name="data">the tree section of the <see cref="Quadtree{T}"/></param>
    /// <param name="maxHeight">the maximum height of the <see cref="Quadtree{T}"/></param>
    /// <exception cref="Quadtree{T}.InvalidMaxHeightException">thrown when <paramref name="maxHeight"/> is not in the
    /// acceptable range.</exception>
    public Quadtree(DynamicArray<QuadtreeNode> tree, DynamicArray<T> data, int maxHeight)
    {
        // validate maxHeight
        if (maxHeight is < 2 or > 64)
            throw new InvalidMaxHeightException(maxHeight);
        
        Tree = tree;
        Data = data;
        MaxHeight = maxHeight;
        var halfSize = -1L << (MaxHeight - 1);
        Dimensions = NodeRangeFromPos(new Vec2<long>(halfSize), MaxHeight);
        StoreModifications = tree.StoreModifications && data.StoreModifications;
        if (StoreModifications)
            _modifications = new DynamicArray<Range2D>(25_00 /* 80kb per chunk */, false, false);
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
        return Data[Tree[node].GetValueRef()];
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
        Tree[node] = new QuadtreeNode(Data.Add(value));
        
        // store the modification
        if (StoreModifications)
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
        if (StoreModifications)
            _modifications.Add(range);
        
        // store the `value` and keep a reference to it
        var valueRef = Data.Add(value);
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
            var node = Tree[nodeRef];
            
            // if the node is only partially contained, step down into it, narrowing the area we can modify
            if (range.Overlaps(nodeRange) && !range.Contains(nodeRange))
            {
                // if it is a leaf node, subdivide it first
                if (node.Type == Leaf)
                {
                    Subdivide(nodeRef);
                    node = Tree[nodeRef]; // refresh `node`
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
                    Tree[nodeRef] = valueNode;
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
        Tree.Shrink(); // shrink the tree
    }
    
    #endregion
    
    #region Compression
    
    // TODO: [PERFORMANCE] this takes a long time to complete sometimes
    /// <summary>
    /// Tries to combine as many nodes as possible in this <see cref="Quadtree{T}"/>, which are referenced in <see cref="_modifications"/>.
    /// </summary>
    public void Compress()
    {
        // if we do not store modifications, compress the entire quadtree
        if (!StoreModifications)
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
        Tree.Shrink();
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
            var node = Tree[nodeRef];
            
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
    /// <param name="zValue">the z-value of the initial node to compress</param>
    /// <param name="height">the height of the initial node to compress</param>
    /// <param name="path">an array containing all the nodes traversed through to get to this node, indexed using height</param>
    /// <param name="forceFull">whether to compress every node regardless of if it is the last node in its parent</param>
    /// <exception cref="QuadtreeNode.InvalidNodeTypeException">thrown when a node in <paramref name="path"/> has a parent node that is a <see cref="NodeType.Leaf"/> node</exception>
    private void CompressNode(UInt128 zValue, int height, long[] path, bool forceFull = false)
    {
        // try to compress as much as possible
        while (true)
        {
            // don't compress the root node or its immediate children since they are needed for subsets
            if (height >= MaxHeight) break;
            
            // if this node is not the last node in its parent, don't try to compress it, since it's parent will be
            // reached by CompressRange() later anyway
            if (ZValueIndex(zValue, height) != 3 && !forceFull) return;
            
            // get this node's parent
            var parentRef = height == MaxHeight - 1 ? 0 : path[height + 1];
            var parent = Tree[parentRef];
            
            // if the parent node is a leaf, something went very wrong
            if (parent.Type == Leaf)
                throw new QuadtreeNode.InvalidNodeTypeException(Leaf, Branch, "CompressNode/path");
            
            // get the parent's siblings' value refs
            var valueRef = 0L;
            T? value = default;
            for (var i = 0; i < 4; i++)
            {
                var node = Tree[parent.GetNodeRef(i)];
                // exit out of this method if any child is a branch node, since we can compress no further nodes
                if (node.Type == Branch) return;
                var val = node.GetValueRef();
                // store the first value for later use
                if (i == 0)
                {
                    valueRef = val;
                    value = Data[val];
                }
                else if (!value!.CanCombine(Data[val]))
                    return; // exit if we can't combine any node with the first node, since we can compress no further nodes
            }
            
            // if we can compress this node, remove its parent's children
            DeleteChildren(RoundZValue(zValue, height + 1), height + 1, parentRef);
            // set the parent node to a leaf node referencing the value that is common among all 4 of its children
            Tree[parentRef] = new QuadtreeNode(valueRef);
            
            // try to compress the current node's parent
            height++;
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
    /// <returns>the updated <paramref name="zValue"/>, <paramref name="height"/>, and the index of the next node within <see cref="Tree"/></returns>
    internal (UInt128 zValue, int height, long nodeRef) GetNextNode(UInt128 zValue, int height, UInt128 maxZValue, ref long[] path)
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
        var parentNode = Tree[parentNodeRef];
        if (parentNode.Type == Leaf)
            throw new QuadtreeNode.InvalidNodeTypeException(Leaf, Branch, "GetNextNode/parent node in path");
        
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
    internal Vec2<long> GetNextNodePos(Vec2<long> nodePos, UInt128 zValue, int height)
    {
        if (height >= MaxHeight)
            return (-1, -1);
        
        // get the index of this node within its parent
        var parentIndex = ZValueIndex(zValue, height);
        
        // get the size of this node
        var nodeSize = 0x1L << height;
        
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
    /// Traverses through the <see cref="Quadtree{T}"/> to find the index of a node within <see cref="Tree"/> that
    /// corresponds to the supplied z-value and height.
    /// </summary>
    /// <param name="zValue">the z-value of the target node</param>
    /// <param name="targetHeight">[optional] the height of the returned node. Defaults to 0. If <paramref name="readOnly"/> is set to
    /// true, the returned node may have a higher height</param>
    /// <param name="readOnly">[optional] when set to true, prevents the quadtree from being modified.
    /// If set, it is no longer guaranteed that a reference to a 1x1 node will be returned</param>
    /// <param name="path">[optional] an <see cref="int"/>[<see cref="MaxHeight"/> + 1] into which the nodes traversed
    /// through by this method will be stored, in reference form</param>
    /// <returns>An index within <see cref="Tree"/> that refers to a node of height <paramref name="targetHeight"/> (unless <paramref name="readOnly"/> is set to true)</returns>
    /// <exception cref="PositionOutOfBoundsException">Thrown when the supplied position does not reside within the quadtree</exception>
    internal long GetNodeRef(UInt128 zValue, int targetHeight = 0, bool readOnly = false, long[]? path = null)
    {
        // validate path parameter
        var usePath = path != null;
        if (usePath && path!.Length != MaxHeight)
            throw new Exception($"Invalid path Length: Was: {path.Length}, Required: {MaxHeight}");
        
        // start at root node
        var nodeRef = 0L;
        for (var height = MaxHeight - 1; height >= targetHeight; height--)
        {
            var node = Tree[nodeRef];
            
            if (node.Type == Leaf)
            {
                // if we are not allowed to modify the quadtree, stop descending prematurely
                if (readOnly)
                    break;
                
                // otherwise, subdivide the leaf node and refresh `node`
                Subdivide(nodeRef);
                node = Tree[nodeRef];
            }
            
            // get the next node
            
            // extract the relevant 2-bit section from the z-value
            var zPart = ZValueIndex(zValue, height);
            
            // set the current node to the node within at index `zPart`
            nodeRef = node.GetNodeRef(zPart);
            
            // update the path if we need to
            if (usePath) path![height] = nodeRef;
        }
        
        // return the node
        return nodeRef;
    }
    
    #endregion
    
    #region Node Modification
    
    /// <summary>
    /// Subdivides a leaf node into 4 smaller leaf nodes with the same value, replacing the original node.
    /// </summary>
    /// <param name="nodeIndex">the index (within <see cref="Tree"/>) of the node to subdivide</param>
    /// <exception cref="QuadtreeNode.InvalidNodeTypeException">Thrown when <paramref name="nodeIndex"/> does not reference a leaf node</exception>
    /// <remarks>The supplied node's index within <see cref="Tree"/> does not change.</remarks>
    internal void Subdivide(long nodeIndex)
    {
        if (Tree[nodeIndex].Type != Leaf)
            throw new QuadtreeNode.InvalidNodeTypeException(Branch, Leaf);
        
        // get the value of the original node
        var valueRef = Tree[nodeIndex].GetValueRef();
        
        // create 4 new leaf nodes with that value
        var i0 = Tree.Add(new QuadtreeNode(valueRef));
        var i1 = Tree.Add(new QuadtreeNode(valueRef));
        var i2 = Tree.Add(new QuadtreeNode(valueRef));
        var i3 = Tree.Add(new QuadtreeNode(valueRef));
        
        // assign them to the new branch node, replacing the original leaf node
        Tree[nodeIndex] = new QuadtreeNode(i0, i1, i2, i3);
    }
    
    /// <summary>
    /// Deletes all of a node's children.
    /// </summary>
    /// <param name="zValue">the z-value of the target node</param>
    /// <param name="height">the height of the target node</param>
    /// <param name="nodeRef">[optional, will be retrieved if not set] a reference to the node that will have its
    /// children deleted</param>
    /// <remarks>Does not call <see cref="DynamicArray{T}.Shrink()"/> on the <see cref="Tree"/> or <see cref="Data"/></remarks>
    internal void DeleteChildren(UInt128 zValue, int height, long nodeRef = -1)
    {
        // nodes at height = 0 have no children
        if (height == 0)
            return;
        
        var maxHeight = height;
        var path = new long[MaxHeight]; // an array of all the nodes traversed through to get to the current node, indexed using height
        if (nodeRef == -1) nodeRef = GetNodeRef(zValue, height); // resolve nodeRef if none was given
        if (Tree[nodeRef].Type == Leaf) return; // if the target node is a leaf, it has no children and therefore none can be deleted
        var targetNode = nodeRef;
        
        var pathNextSibling = new int[MaxHeight]; // [height] = index (within parent) of the next sibling to be deleted
        
        // add initial value to path
        if (height != MaxHeight) path[height] = nodeRef;
        
        while (true)
        {
            var node = Tree[nodeRef];
            
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
            Tree.Remove(nodeRef, false);
            
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
                var parentNode = Tree[height == MaxHeight - 1 ? 0 : path[height + 1]];
                
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
        Tree[targetNode] = new QuadtreeNode(0);
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
        
        
        // create the subset root
        QuadtreeNode subsetRoot;
        
        // get the child nodes of the subset root
        var ref0 = GetNodeRef(Interleave(center + (-1, -1), MaxHeight), maxHeight-1, true);
        var ref1 = GetNodeRef(Interleave(center + (+1, -1), MaxHeight), maxHeight-1, true);
        var ref2 = GetNodeRef(Interleave(center + (-1, +1), MaxHeight), maxHeight-1, true);
        var ref3 = GetNodeRef(Interleave(center + (+1, +1), MaxHeight), maxHeight-1, true);
        
        // construct the subset root node
        if (ref0 == ref1 && ref0 == ref2 && ref0 == ref3)
            subsetRoot = new QuadtreeNode(Tree[ref0].GetValueRef());
        else
            subsetRoot = new QuadtreeNode(ref0, ref1, ref2, ref3);
        
        // create the subset range
        var distanceRange = Pow2MinMax(MaxHeight);
        var subsetRange = new Range2D(distanceRange.Min, distanceRange.Min, distanceRange.Max, distanceRange.Max);
        
        return (subsetRoot, subsetRange);
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
    /// Gets the lengths of the <see cref="Tree"/> and <see cref="Data"/> sections in this <see cref="Quadtree{T}"/>.
    /// </summary>
    public (long treeLength, long dataLength) GetLength()
    {
        return (Tree.ModificationLength, Data.ModificationLength);
    }
    
    /// <summary>
    /// Retrieves every modification that has been done to this <see cref="Quadtree{T}"/> since the last time
    /// <see cref="ClearModifications()"/> was called, and copies them into <paramref name="tree"/> and <paramref name="data"/>.
    /// </summary>
    /// <param name="tree">the buffer into which to copy the tree modifications</param>
    /// <param name="data">the buffer into which to copy the data modifications</param>
    /// <returns>The amount of modifications that were copied into <paramref name="tree"/> and <paramref name="data"/></returns>
    public (long Tree, long Data) GetModifications(
        DynamicArray<ArrayModification<QuadtreeNode>> tree, DynamicArray<ArrayModification<T>> data)
    {
        if (!StoreModifications)
            throw new StoredModificationsException();

        var treeCount = Tree.GetModifications(tree);
        var dataCount = Data.GetModifications(data);

        return (treeCount, dataCount);
    }
    
    
    /// <summary>
    /// Resets the internally stored modifications.
    /// </summary>
    public void ClearModifications()
    {
        if (!StoreModifications)
            throw new StoredModificationsException();
        Tree.ClearModifications();
        Data.ClearModifications();
    }
    
    /// <summary>
    /// Computes the hash code of this <see cref="Quadtree{T}"/>. Only factors in <see cref="Tree"/> and <see cref="Data"/>.
    /// </summary>
    public override int GetHashCode()
    {
        return Tree.GetHashCode() ^ Data.GetHashCode();
    }
    
    /// <summary>
    /// Frees up the memory consumed by this <see cref="Quadtree{T}"/>.
    /// </summary>
    public void Dispose()
    {
        Tree.Dispose();
        Data.Dispose();
    }
    
    #endregion
    
    #region Exceptions
    
    private class InvalidMaxHeightException(int maxHeight) : Exception(
        $"Invalid Max Height. Was: {maxHeight}, Range: 2-64");
    
    private class InvalidHeightException(int height, int maxHeight) : Exception(
        $"Invalid Height. Was: {height}, Range: 0-{maxHeight}");
    
    private class RangeOutOfBoundsException(Range2D range, Range2D bound) : Exception(
        $"Range {range} is outside QuadTree bounds of {bound}");
    
    private class PositionOutOfBoundsException(Vec2<long> pos, Range2D bound) : Exception(
        $"Position {pos} is Outside QuadTree Bounds of {bound}");
    
    public class StoredModificationsException() :
        Exception("Unable to retrieve modifications from Quadtree: Modification Storing is disabled");
    
    #endregion
    
}
