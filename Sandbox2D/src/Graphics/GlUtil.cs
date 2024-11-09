using System;
using OpenTK.Graphics.OpenGL4;

namespace Sandbox2D.Graphics;

public static class GlUtil
{
    /// <summary>
    /// Resizes an OpenGL buffer by creating a new buffer and copying the data over.
    /// </summary>
    /// <param name="buffer">the handle of the buffer to resize</param>
    /// <param name="currentLength">the current length of the buffer</param>
    /// <param name="newLength">the new length of the buffer</param>
    /// <param name="hint">the <see cref="BufferUsageHint"/> of the new buffer</param>
    /// <param name="clean">whether to set all newly allocated bits to 0</param>
    /// <returns>the new buffer's handle</returns>
    public static int ResizeBuffer(
        int buffer, int currentLength, int newLength, BufferUsageHint hint, bool clean = false)
    {
        // allocate data for a new buffer
        var newBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.CopyWriteBuffer, newBuffer);
        var data = clean ? new byte[newLength] : []; // TODO: large memory allocation
        GL.BufferData(BufferTarget.CopyWriteBuffer, newLength, data, hint);
        
        // TODO: performance issue when copying from gpu to cpu memory and back
        // bind the old buffer to `CopyReadBuffer`
        GL.BindBuffer(BufferTarget.CopyReadBuffer, buffer);
        // copy data from old buffer to new buffer
        var copySize = Math.Min(currentLength, newLength);
        GL.CopyBufferSubData(BufferTarget.CopyReadBuffer, BufferTarget.CopyWriteBuffer, 
            IntPtr.Zero, IntPtr.Zero, copySize);
        
        
        // unbind buffers / delete old buffer
        GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
        GL.DeleteBuffer(buffer);
        
        return newBuffer;
    }
}