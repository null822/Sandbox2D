using System;
using System.Collections.Generic;
using Math2D;
using Sandbox2D.World;

namespace Sandbox2D.Registry;

/// <summary>
/// A delegate that returns a <see cref="Tile"/>, provided a set of bytes.
/// </summary>
public delegate Tile TileConstructor(Span<byte> bytes);

public static class Tiles
{
    private static readonly Dictionary<int, TileConstructor> Values = new();
    
    /// <summary>
    /// Registers a new <see cref="Tile"/>.
    /// </summary>
    /// <param name="tileId">the name of <see cref="Tile"/></param>
    /// <param name="constructor">a <see cref="TileConstructor"/>, used to construct the <see cref="Tile"/>.</param>
    public static void Register(int tileId, TileConstructor constructor)
    {
        Values.Add(tileId, constructor);
    }
    
    /// <summary>
    /// Creates (by running its <see cref="TileConstructor"/>) and returns a new <see cref="Tile"/>.
    /// </summary>
    /// <param name="bytes">the data of the <see cref="Tile"/></param>
    /// <exception cref="MissingTileException">thrown when the <paramref name="bytes"/> do not reference a registered tile.</exception>
    public static Tile Create(Span<byte> bytes)
    {
        var tileId = BitUtil.GetUshort(bytes[..2]);
        
        if (Values.TryGetValue(tileId, out var constructor))
        {
            return constructor.Invoke(bytes);
        }
        
        throw new MissingTileException(tileId);
    }
}

public class MissingTileException(int tileId)
    : Exception($"Tile of ID {tileId} was not found");


public enum TileType : ushort
{
    Air = 0,
    Dirt = 1,
    Stone = 2,
    Paint = 3,
}