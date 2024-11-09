using System.Runtime.InteropServices;
using Math2D.Binary;

namespace Math2D.Quadtree;


/// <summary>
/// Represents a single node within a <see cref="Quadtree{T}"/>, stored in <see cref="Quadtree{T}.Tree"/>.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 36)]
public readonly struct QuadtreeNode : IByteSerializable, IByteDeserializable<QuadtreeNode>
{
    /// <summary>
    /// The maximum length of a single array of <see cref="QuadtreeNode"/>s, intended to prevent
    /// <see cref="DynamicArray{T}"/>s of <see cref="QuadtreeNode"/>s from being allocated to the LOH
    /// </summary>
    public const int MaxChunkSize = 2048;
    
    /// <summary>
    /// The type of the node
    /// </summary>
    [FieldOffset(0)]
    public readonly NodeType Type;
    
    /// <summary>
    /// The reference to the value of this node, if <see cref="Type"/> = <see cref="NodeType.Leaf"/>
    /// </summary>
    /// <remarks>Also used to store the reference for a leaf node.</remarks>
    [FieldOffset(4)]
    public readonly long LeafRef;
    
    /// <summary>
    /// The reference to the (-X -Y), or Bottom Left, node.
    /// </summary>
    /// <remarks>Also used to store the reference for a leaf node.</remarks>
    [FieldOffset(4)]
    public readonly long Ref0;
    /// <summary>
    /// The reference to the (+X -Y), or Bottom Right, node.
    /// </summary>
    [FieldOffset(12)]
    public readonly long Ref1;
    /// <summary>
    /// The reference to the (-X +Y), or Top Left, node.
    /// </summary>
    [FieldOffset(20)]
    public readonly long Ref2;
    /// <summary>
    /// The reference to the (+X +Y), or Top Right, node.
    /// </summary>
    [FieldOffset(28)]
    public readonly long Ref3;
    
    /// <summary>
    /// Constructs a leaf node.
    /// </summary>
    /// <param name="refVal">an index within the data array storing the value of this node</param>
    public QuadtreeNode(long refVal)
    {
        Type = NodeType.Leaf;
        LeafRef = refVal;
    }
    
    /// <summary>
    /// Constructs a branch node.
    /// Accepts 4 references within the containing array that refer to the 4 sub-nodes stored within.
    /// </summary>
    /// <param name="ref0">reference for (-X -Y), or BL, node</param>
    /// <param name="ref1">reference for (+X -Y), or BR, node</param>
    /// <param name="ref2">reference for (-X +Y), or TL, node</param>
    /// <param name="ref3">reference for (+X +Y), or TR, node</param>
    public QuadtreeNode(long ref0, long ref1, long ref2, long ref3)
    {
        Type = NodeType.Branch;
        
        Ref0 = ref0;
        Ref1 = ref1;
        Ref2 = ref2;
        Ref3 = ref3;
    }

    private QuadtreeNode(uint type, long ref0, long ref1, long ref2, long ref3)
    {
        Type = (NodeType)type;
        
        Ref0 = ref0;
        Ref1 = ref1;
        Ref2 = ref2;
        Ref3 = ref3;
    }
    
    /// <summary>
    /// Returns the reference to a node within this node.
    /// </summary>
    /// <param name="nodeIndex">the index of the node to get. Range 0-3:<br></br>
    /// 0 = (-X -Y), or BL<br></br>
    /// 1 = (+X -Y), or BR<br></br>
    /// 2 = (-X +Y), or TL<br></br>
    /// 3 = (+X +Y), or TR<br></br>
    /// </param>
    /// <exception cref="InvalidNodeTypeException">Thrown when this node is not the correct type (if it is a leaf node).</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="nodeIndex"/> is out of range.</exception>
    public long GetNodeRef(int nodeIndex)
    {
        if (Type == NodeType.Leaf) throw new InvalidNodeTypeException(Type, NodeType.Branch);
        
        return nodeIndex switch
        {
            0 => Ref0,
            1 => Ref1,
            2 => Ref2,
            3 => Ref3,
            _ => throw new ArgumentOutOfRangeException(nameof(nodeIndex), nodeIndex, $"Node index range is 0-3, received {nodeIndex}")
        };
    }
    
    /// <summary>
    /// Returns the reference to the value of this node
    /// </summary>
    /// <exception cref="InvalidNodeTypeException">Thrown when this node is not the correct type (if it is a branch node).</exception>
    public long GetValueRef()
    {
        if (Type == NodeType.Branch) throw new InvalidNodeTypeException(Type, NodeType.Leaf);
        
        return LeafRef;
    }
    
    
    public static int SerializeLength => 36;
    
    public byte[] Serialize(bool bigEndian = false)
    {
        if (bigEndian)
        {
            return [
                ..BitUtil.GetBytesBe((uint)Type),
                ..BitUtil.GetBytesBe(Ref0),
                ..BitUtil.GetBytesBe(Ref1),
                ..BitUtil.GetBytesBe(Ref2),
                ..BitUtil.GetBytesBe(Ref3)
            ];
        }
        
        return [
            ..BitUtil.GetBytes((uint)Type),
            ..BitUtil.GetBytes(Ref0),
            ..BitUtil.GetBytes(Ref1),
            ..BitUtil.GetBytes(Ref2),
            ..BitUtil.GetBytes(Ref3)
        ];
    }
    
    public static QuadtreeNode Deserialize(byte[] data, bool bigEndian = false)
    {
        var dataSpan = data.AsSpan();

        if (bigEndian)
        {
            return new QuadtreeNode(
                BitUtil.GetUIntBe(dataSpan[  .. 4]),
                BitUtil.GetLongBe(dataSpan[ 4..12]),
                BitUtil.GetLongBe(dataSpan[12..20]),
                BitUtil.GetLongBe(dataSpan[20..28]),
                BitUtil.GetLongBe(dataSpan[28..36]));
        }
        
        return new QuadtreeNode(
            BitUtil.GetUInt(dataSpan[  .. 4]),
            BitUtil.GetLong(dataSpan[ 4..12]),
            BitUtil.GetLong(dataSpan[12..20]),
            BitUtil.GetLong(dataSpan[20..28]),
            BitUtil.GetLong(dataSpan[28..36]));
    }
    
    /// <summary>
    /// Converts this <see cref="QuadtreeNode"/> into a readable string containing all the data held within this struct.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        switch (Type)
        {
            case NodeType.Branch:
                if (Ref0 == 0 && Ref1 == 0 && Ref2 == 0 && Ref3 == 0)
                    return "[ Null ]";
                return $"[Branch] {Ref0} {Ref1} {Ref2} {Ref3}";
            case NodeType.Leaf:
                return $"[ Leaf ] {LeafRef}";
            default:
                return $"[ERROR ] {(uint)Type:x8} {Ref0:x16} {Ref1:x16} {Ref2:x16} {Ref3:x16}]";
        }
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
}

/// <summary>
/// An enum for the 2 types of nodes: <see cref="NodeType.Branch"/> and <see cref="NodeType.Leaf"/>.
/// </summary>
public enum NodeType : uint
{
    /// <summary>
    /// Represents a type of node that contains 4 child nodes (indexes within <see cref="Quadtree{T}.Tree"/>), and no
    /// values (indexes within <see cref="Quadtree{T}.Data"/>).
    /// </summary>
    Branch = 0,
    /// <summary>
    /// Represents a type of node that contains no child nodes (elements within <see cref="Quadtree{T}.Tree"/>), and one
    /// value (an index within <see cref="Quadtree{T}.Data"/>).
    /// </summary>
    Leaf = 1
}
