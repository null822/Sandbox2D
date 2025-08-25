using System;
using OpenTK.Graphics.OpenGL4;

namespace Sandbox2D.Graphics.Buffers;

public class GpuBuffer : IGpuBuffer
{
    public int Handle { get; }
    private readonly BufferUsageHint _hint;
    
    public GpuBuffer(int length, bool clean = false, BufferUsageHint hint = BufferUsageHint.StaticDraw)
    {
        _hint = hint;
        Handle = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.CopyWriteBuffer, Handle);
        GL.BufferData(BufferTarget.CopyWriteBuffer, length, clean ? new byte[length] : [], _hint);
        GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
    }
    
    public void Bind(BufferRangeTarget target, int index)
    {
        GL.BindBufferBase(target, index, Handle);
    }
    
    public void Bind(BufferTarget target)
    {
        GL.BindBuffer(target, Handle);
    }
    
    public void Write(byte[] data, int index)
    {
        GL.BindBuffer(BufferTarget.CopyWriteBuffer, Handle);
        
        GL.BufferSubData(
            BufferTarget.CopyWriteBuffer,
            index,
            (IntPtr)data.Length,
            data);
        
        GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
    }
    
    public void Clear()
    {
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, Handle);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, 0, Array.Empty<byte>(), _hint);
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
    }
    
    public void Dispose()
    {
        GL.DeleteBuffer(Handle);
    }
}