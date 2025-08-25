using System;
using OpenTK.Graphics.OpenGL4;

namespace Sandbox2D.Graphics.Buffers;

public interface IGpuBuffer : IDisposable
{
    /// <summary>
    /// The handle of the buffer
    /// </summary>
    public int Handle { get; }
    
    
    /// <summary>
    /// Binds the buffer.
    /// </summary>
    /// <param name="target">the <see cref="BufferTarget"/> to bind to</param>
    public void Bind(BufferTarget target);

    /// <summary>
    /// Binds the buffer.
    /// </summary>
    /// <param name="target">the <see cref="BufferRangeTarget"/> to bind to</param>
    /// <param name="index">the index of the binding point</param>
    public void Bind(BufferRangeTarget target, int index);
    
    /// <summary>
    /// Writes a set of bytes into a specific position of the <see cref="GpuBuffer"/>.
    /// </summary>
    /// <param name="data">the bytes to write</param>
    /// <param name="index">the starting position within the <see cref="GpuBuffer"/> to write to</param>
    public void Write(byte[] data, int index);
    
    /// <summary>
    /// Clears the buffer
    /// </summary>
    public void Clear();
}