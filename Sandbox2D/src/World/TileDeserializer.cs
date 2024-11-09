using System;
using System.Collections.Generic;
using Math2D;
using Math2D.Binary;

namespace Sandbox2D.World;

/// <summary>
/// A delegate that returns a <see cref="Tile"/>, provided a set of bytes.
/// </summary>
public delegate Tile TileConstructor(Span<byte> bytes);

/// <summary>
/// Deserializes a set of bytes into a <see cref="Tile"/>.
/// </summary>
public static class TileDeserializer
{
    private static readonly Dictionary<int, TileConstructor> Tiles = new();
    
    /// <summary>
    /// Registers a new <see cref="Tile"/>.
    /// </summary>
    /// <param name="tileId">the name of <see cref="Tile"/></param>
    /// <param name="constructor">a <see cref="TileConstructor"/>, used to construct the <see cref="Tile"/>.</param>
    public static void Register(int tileId, TileConstructor constructor)
    {
        Tiles.Add(tileId, constructor);
    }

    /// <summary>
    /// Creates a new <see cref="Tile"/> (by running its corresponding <see cref="TileConstructor"/>).
    /// </summary>
    /// <param name="bytes">the data of the <see cref="Tile"/></param>
    /// <param name="bigEndian">whether the data in <paramref name="bytes"/> is in big endian form</param>
    /// <exception cref="MissingTileException">
    /// thrown when the <paramref name="bytes"/> do not reference a registered <see cref="TileConstructor"/>.
    /// </exception>
    public static Tile Create(Span<byte> bytes, bool bigEndian = false)
    {
        var tileId = (ushort)((bigEndian ? BitUtil.GetULongBe(bytes) : BitUtil.GetULong(bytes)) >> 48);
        
        if (Tiles.TryGetValue(tileId, out var constructor))
        {
            return constructor.Invoke(bytes);
        }
        
        throw new MissingTileException(tileId);
    }
}

public class MissingTileException(int tileId)
    : Exception($"Tile of ID {tileId} was not found");