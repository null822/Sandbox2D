using System;
using System.Runtime.InteropServices;
using Math2D;

namespace Sandbox2D.World;

[StructLayout(LayoutKind.Explicit, Size = 8)]
public readonly struct TileData
{
    /// <summary>
    /// The data of this tile. Consists of 16 bytes of <see cref="Id"/> and 48 bytes of <see cref="Data"/>.
    /// </summary>
    [FieldOffset(0)]
    private readonly ulong _data;
    
    /// <summary>
    /// The id of this tile. 16 bits total.
    /// </summary>
    public ushort Id => (ushort)(_data >> 48);
    /// <summary>
    /// The data of this tile. 48 bits total.
    /// </summary>
    public ulong Data => _data & 0x0000FFFFFFFFFFFF;

    public TileData(Span<byte> bytes)
    {
        _data = BitUtil.GetULong(bytes);
    }
    
    public TileData(ushort id)
    {
        _data = (ulong)id << 48;
    }
    
    public TileData(ushort id, ulong data)
    {
        _data = ((ulong)id << 48) | (data & 0x0000FFFFFFFFFFFF);
    }
    
    public TileData(ulong data)
    {
        _data = data;
    }
    
    public byte[] Serialize()
    {
        return BitUtil.GetBytes(_data);
    }
    
    public bool Equals(TileData t)
    {
        return _data == t._data;
    }
}
