﻿using System;

namespace Sandbox2D.Maths.Quadtree;

public interface IQuadTreeValue<T>
{
    /// <summary>
    /// Creates a new instance.
    /// </summary>
    public static abstract IQuadTreeValue<T> New(T value);
    
    /// <summary>
    /// Checks if this object is equal to the supplied object.
    /// </summary>
    /// <param name="a">the other object</param>
    public bool Equals(IQuadTreeValue<T> a);
    
    /// <summary>
    /// Returns the enclosed value.
    /// </summary>
    public T Get();
    
    /// <summary>
    /// Sets the enclosed value.
    /// </summary>
    public void Set(T value);
    
    /// <summary>
    /// Converts this instance into a span of bytes.
    /// </summary>
    public ReadOnlySpan<byte> Serialize();
    
    /// <summary>
    /// Converts a span of bytes into an instance of IQuadTreeValue.
    /// </summary>
    /// <param name="bytes">the bytes to convert</param>
    public static abstract T Deserialize(ReadOnlySpan<byte> bytes);
    
    /// <summary>
    /// The size, in bytes, of the serialized IQuadTreeValue.
    /// </summary>
    public static abstract uint SerializeLength { get; }
    
    /// <summary>
    /// Returns a uint representing this IQuadTreeValue, ready to be uploaded to the gpu.
    /// </summary>
    public uint LinearSerialize();
}