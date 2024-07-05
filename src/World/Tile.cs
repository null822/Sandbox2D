using System;

namespace Sandbox2D.World;

public readonly struct Tile
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
    
    
    public Tile()
    {
        _data = 0;
    }
    
    public Tile(ushort id)
    {
        _data = (ulong)id << 48;
    }
    
    public Tile(ushort id, ulong data)
    {
        _data = ((ulong)id << 48) | data;
    }
    
    public bool Equals(Tile t)
    {
        return _data == t._data;
    }
}
