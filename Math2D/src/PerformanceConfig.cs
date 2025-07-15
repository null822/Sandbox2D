namespace Math2D;

public static class PerformanceConfig
{
    /// <summary>
    /// If the <see cref="Range2D.Combine"/>d area of 2 modifications is less than <see cref="EagerCompressArea"/>,
    /// <see cref="Math2D.Quadtree.Quadtree{T}.Compress()"/> will always combine the modifications, to reduce compressing
    /// overhead.
    /// </summary>
    public static readonly UInt128 EagerCompressArea = 4096;
    
    /// <summary>
    /// The maximum amount of times a <see cref="Quadtree.Modification"/> can fail to be compressed before it is forced
    /// (prevents <see cref="Quadtree.Modification"/>s from persisting)
    /// </summary>
    public const uint MaxModificationLifetime = 20;
}