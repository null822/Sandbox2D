
using Sandbox2D.Maths.Quadtree;

namespace Sandbox2D.World;

public readonly struct TileData
{
    /// <summary>
    /// The data of this tile. Consists of 16 bytes of <see cref="Id"/> and 48 bytes of <see cref="Data"/>.
    /// </summary>
    private readonly ulong _data;
    
    /// <summary>
    /// The id of this tile. 16 bits total.
    /// </summary>
    public ushort Id => (ushort)(_data >> 48);
    /// <summary>
    /// The data of this tile. 48 bits total.
    /// </summary>
    public ulong Data => _data & 0x0000ffffffffffff;

    public const int Size = sizeof(ulong);
    
    public TileData(ushort id)
    {
        _data = (ulong)id << 48;
    }
    
    public TileData(ushort id, ulong data)
    {
        _data = ((ulong)id << 48) | data;
    }
    
    public TileData(ulong data)
    {
        _data = data;
    }
    
    public byte[] Serialize()
    {
        return QuadtreeUtil.GetBytes(_data);
    }
    
    public bool Equals(TileData t)
    {
        return _data == t._data;
    }
}
