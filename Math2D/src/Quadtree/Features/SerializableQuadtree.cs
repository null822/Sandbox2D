using Math2D.Quadtree.FeatureNodeTypes;
using static Math2D.Quadtree.NodeType;
using static Math2D.Quadtree.QuadtreeUtil;
using static Math2D.BitUtil;

namespace Math2D.Quadtree.Features;

public class SerializableQuadtree<T> : QuadtreeFeature<T> where T : IQuadtreeElement<T>, IFeatureFileSerialization<T>
{
    public override Quadtree<T> Base { get; }

    private readonly DynamicArray<QuadtreeNode> _tree;
    private readonly DynamicArray<T> _data;
    
    public SerializableQuadtree(Quadtree<T> quadtree)
    {
        Base = quadtree;
        
        _tree = Tree;
        _data = Data;
    }
    
    private const int HeaderSize = 18;
    
    /// <summary>
    /// Serializes this <see cref="Quadtree{T}"/> into a <see cref="Stream"/>, using the format described in
    /// src/Maths/Quadtree/quadtree-format.md.
    /// </summary>
    /// <param name="stream">the stream into which this <see cref="Quadtree{T}"/> will be serialized into</param>
    public void Serialize(Stream stream)
    {
        var header = new MemoryStream();
        var tree = new MemoryStream();
        var data = new MemoryStream();
        
        var dataRefSize = MinByteCount((ulong)_data.Length); // size of a reference into the data section, in bytes
        
        // create the header (excluding data pointer)
        header.Write([(byte)MaxHeight]); // maxHeight
        header.Write(GetBytes(~0u)); // features // TODO: replace with other information about T (in format)
        header.Write([(byte)dataRefSize]); // data ref size
        header.Write(GetBytes(_data[0].SerializeLength)); // size of T
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
        
        var data = new DynamicArray<byte[]>(65_536 /* each chunk will consume 85kib */, false, false);
        
        var serializeLength = _data[0].SerializeLength;
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
                var serializeInstance = _data[node.GetValueRef()];
                var value = serializeInstance.Serialize();
                if (value.Length != serializeLength)
                    throw new Exception($"Invalid byte count. Expected {serializeLength} but got {value.Length} from {serializeInstance}");
                long i;
                if (data.Contains(v => CompareBytes(v, value))) // TODO: definitely a better solution
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
    /// <param name="storeModifications">whether to store modifications. See <see cref="Quadtree{T}(int,T,bool)"/> for
    /// more information</param>
    /// <typeparam name="T2">The type of the elements in the returned <see cref="Quadtree{T}"/></typeparam>
    /// <returns>the deserialized quadtree</returns>
    public static SerializableQuadtree<T2> Deserialize<T2>(Stream stream, bool storeModifications = false) where T2 : IQuadtreeElement<T2>, IFeatureFileSerialization<T2>
    {
        // get the header
        var header = new Span<byte>(new byte[HeaderSize]);
        stream.ReadExactly(header);
        
        // read the header
        var maxHeight = header[0]; // maxHeight
        var features = GetUint(header[1..5]); // enabled features // TODO: feature field replacement
        var dataRefSize = (int)header[5]; // data ref size
        var tSize = GetInt(header[6..10]); // size of T
        var dataStart = GetLong(header[10..18]); // start of data section
        
        // TODO: feature field replacement
        // compare the features in the stream to the ones specified in T2
        // if (!disableFeatureCheck && enabledFeatures != features)
            // throw new Exception("Mismatching feature set when deserializing Quadtree");
        
        // construct the quadtree
        var qt = new SerializableQuadtree<T2>(new Quadtree<T2>(
            new DynamicArray<QuadtreeNode>(QuadtreeNode.MaxChunkSize, storeModifications),
            new DynamicArray<T2>(T2.MaxChunkSize, storeModifications), maxHeight));
        
        // populate the tree
        qt.Populate_tree(stream, dataStart, dataRefSize);
        
        // populate the data
        stream.Position = dataStart;
        var valueBytes = new byte[tSize];
        for (var i = dataStart; i < stream.Length - (tSize - 1); i+= tSize)
        {
            stream.ReadExactly(valueBytes);
            qt._data.Add(T2.Deserialize(valueBytes));
        }
        
        // clean up
        header.Clear();
                
        // return the resulting quadtree
        return qt;
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
    /// <exception cref="QuadtreeNode.InvalidNodeTypeException">thrown when an invalid <see cref="NodeType"/> is encountered in the
    /// <paramref name="stream"/>.</exception>
    private void Populate_tree(Stream stream, long endIndex, int dataRefSize)
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
                throw new QuadtreeNode.InvalidNodeTypeException(type, "Populate_tree()/tree_data");
            }
            
            // unless this node is the root node, modify the newly added node's parent to point to its new child
            if (height != MaxHeight)
            {
                // get the parent node
                var parentRef = height == MaxHeight-1 ? 0 : path[height + 1];
                var parent = _tree[parentRef];
                if (parent.Type == Leaf)
                    throw new QuadtreeNode.InvalidNodeTypeException(parent.Type, Branch, "Populate_tree()/path");
                
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
    
    public class InvalidQuadtreeSave(string value) : Exception($"Unable to fully read {value} from save file");
}