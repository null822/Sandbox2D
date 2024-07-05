using System;
using System.Runtime.InteropServices;

namespace Sandbox2D.Maths.Quadtree;


/// <summary>
/// Represents a single node within a <see cref="Quadtree{T}"/>, stored in <see cref="Quadtree{T}._tree"/>.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 20)]
public readonly struct QuadtreeNode
{
    /// <summary>
    /// The type of the node
    /// </summary>
    [FieldOffset(0)]
    public readonly NodeType Type;
    
    /// <summary>
    /// The reference to the (-X +Y), or Top Left, node.
    /// </summary>
    /// <remarks>Also used to store the reference for a leaf node.</remarks>
    [FieldOffset(4)]
    public readonly int Ref0;
    /// <summary>
    /// The reference to the (+X +Y), or Top Right, node.
    /// </summary>
    [FieldOffset(8)]
    public readonly int Ref1;
    /// <summary>
    /// The reference to the (-X -Y), or Bottom Left, node.
    /// </summary>
    [FieldOffset(12)]
    public readonly int Ref2;
    /// <summary>
    /// The reference to the (+X -Y), or Bottom Right, node.
    /// </summary>
    [FieldOffset(16)]
    public readonly int Ref3;
    
    /// <summary>
    /// Constructs a leaf node.
    /// </summary>
    /// <param name="refVal">an index within the data array storing the value of this node</param>
    public QuadtreeNode(int refVal)
    {
        Type = NodeType.Leaf;
        Ref0 = refVal;
    }
    
    /// <summary>
    /// Constructs a branch node.
    /// Accepts 4 references within the containing array that refer to the 4 sub-nodes stored within.
    /// </summary>
    /// <param name="ref0">reference for (-X -Y), or BL, node</param>
    /// <param name="ref1">reference for (+X -Y), or BR, node</param>
    /// <param name="ref2">reference for (-X +Y), or TL, node</param>
    /// <param name="ref3">reference for (+X +Y), or TR, node</param>
    public QuadtreeNode(int ref0, int ref1, int ref2, int ref3)
    {
        Type = NodeType.Branch;
        
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
    public int GetNodeRef(int nodeIndex)
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
    public int GetValueRef()
    {
        if (Type == NodeType.Branch) throw new InvalidNodeTypeException(Type, NodeType.Leaf);
        
        return Ref0;
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
                if (Ref0 == Ref1 && Ref0 == Ref2 && Ref0 == Ref1)
                    return "[ Null ]";
                return $"[Branch] {Ref0} {Ref1} {Ref2} {Ref3}";
            case NodeType.Leaf:
                return $"[ Leaf ] {Ref0}";
            default:
                return $"[ERROR ] {(uint)Type:x8} {Ref0:x8} {Ref1:x8} {Ref2:x8} {Ref3:x8}]";
        }
    }
}

/// <summary>
/// An enum for the 2 types of nodes: <see cref="NodeType.Branch"/> and <see cref="NodeType.Leaf"/>.
/// </summary>
public enum NodeType : uint
{
    /// <summary>
    /// Represents a type of node that contains 4 child nodes (indexes within <see cref="Quadtree{T}._tree"/>), and no
    /// values (indexes within <see cref="Quadtree{T}._data"/>).
    /// </summary>
    Branch = 0,
    /// <summary>
    /// Represents a type of node that contains no child nodes (indexes within <see cref="Quadtree{T}._tree"/>), and one
    /// value (an index within <see cref="Quadtree{T}._data"/>).
    /// </summary>
    Leaf = 1,
}
