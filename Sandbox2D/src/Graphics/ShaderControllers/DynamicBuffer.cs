using System;
using System.Collections.Generic;
using Math2D;
using Math2D.Binary;
using OpenTK.Graphics.OpenGL4;

namespace Sandbox2D.Graphics.ShaderControllers;

public class DynamicBuffer : ShaderController, IDisposable
{
    /// <summary>
    /// The handle of the buffer
    /// </summary>
    private int _buffer;
    /// <summary>
    /// The size, in bytes, of the buffer
    /// </summary>
    private int _bufferLength;
    
    private readonly int _dataBuffer;
    private readonly int _indexBuffer;
    
    private readonly byte[] _dataArr;
    private readonly uint[] _indexArr;
    private int _currentBatchLength;

    public DynamicBuffer(BufferUsageHint hint = BufferUsageHint.StaticDraw)
        : base("data_patch", hint)
    {
        const int uploadBatchSize = Constants.GpuUploadBatchSize;
        _indexArr = new uint[uploadBatchSize / 4];
        _dataArr = new byte[uploadBatchSize];
        
        // create the buffers
        _buffer = GL.GenBuffer();
        Resize(0);
        
        _dataBuffer = GL.GenBuffer();
        _indexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.CopyWriteBuffer, _dataBuffer);
        GL.BufferData(BufferTarget.CopyWriteBuffer, uploadBatchSize, new byte[uploadBatchSize], hint);
        GL.BindBuffer(BufferTarget.CopyWriteBuffer, _indexBuffer);
        GL.BufferData(BufferTarget.CopyWriteBuffer, uploadBatchSize, new byte[uploadBatchSize], hint);
    }

    public override void Invoke() { }
    
    private void ApplyModifications(uint dataSize)
    {
        // bind buffers
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, _buffer);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, _dataBuffer);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, _indexBuffer);
        
        // use the shader
        Shader.Use();
        
        Shader.Set("_dataSize", dataSize / 4);
        Shader.Set("_totalCount", (uint)_currentBatchLength);
        
        // invoke the shader
        GL.DispatchCompute((int)Math.Ceiling(_currentBatchLength / 64.0), 1, 1);
        
        // unbind the buffers
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
    }
    
    /// <summary>
    /// Binds the buffer.
    /// </summary>
    /// <param name="target">the buffer target to bind to</param>
    /// <param name="index">the index of the binding point</param>
    public void Bind(BufferRangeTarget target, int index)
    {
        GL.BindBufferBase(target, index, _buffer);
    }

    public void Set<T>(T value, int index) where T : IByteSerializable
    {
        GL.BindBuffer(BufferTarget.CopyWriteBuffer, _buffer);

        var tSize = T.SerializeLength;
        
        GL.BufferSubData(
            BufferTarget.CopyWriteBuffer,
            checked((IntPtr)index * tSize),
            (IntPtr)tSize,
            value.Serialize(true));
        
        GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
    }
    
    /// <summary>
    /// Updates the geometry, modifying the existing buffers
    /// </summary>
    /// <param name="modifications">the modifications to the tree section to upload</param>
    /// <param name="batchStart">index within the tree modifications to start uploading at</param>
    /// <returns>1 more than the index of the last element that was uploaded</returns>
    public int Modify<T>(DynamicArray<ArrayModification<T>> modifications, int batchStart) 
        where T : IByteSerializable, IByteDeserializable<T>
    {
        if (modifications.Length == 0) return batchStart;
        var dataSize = T.SerializeLength;
        
        var batchLength = (int)Math.Min(modifications.Length - batchStart, Constants.GpuUploadBatchSize / dataSize);
        var unique = new HashSet<long>();
        var batchEnd = batchStart + batchLength;
        var index = 0;
        for (var i = batchEnd - 1; i >= batchStart; i--)
        {
            var modification = modifications[i];
            if (!unique.Add(modification.Index)) continue;
            
            var value = modification.Value?.Serialize(true) ?? new byte[T.SerializeLength];
            value.CopyTo(_dataArr, index * dataSize);
            
            _indexArr[index] = (uint)modification.Index;
            
            index++;
        }
        _currentBatchLength = index;
        
        GL.BindBuffer(BufferTarget.CopyWriteBuffer, _dataBuffer);

        GL.BufferSubData(BufferTarget.CopyWriteBuffer,
            IntPtr.Zero,
            checked((IntPtr)(_currentBatchLength * dataSize)),
            _dataArr);
        GL.BindBuffer(BufferTarget.CopyWriteBuffer, _indexBuffer);
        GL.BufferSubData(BufferTarget.CopyWriteBuffer,
            IntPtr.Zero,
            checked((IntPtr)(_currentBatchLength * sizeof(uint))),
            _indexArr);
        GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
        
        ApplyModifications((uint)dataSize);
        
        
        return batchEnd;
    }
    
    public override void ResetGeometry()
    {
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _buffer);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, 0, Array.Empty<byte>(), Hint);
        _bufferLength = 0;
        
        _currentBatchLength = 0;
    }

    /// <summary>
    /// Resizes the buffer to the supplied size.
    /// </summary>
    /// <param name="newLength">the new size of the buffer, in bytes</param>
    /// <param name="permAlloc">a size, in bytes, that will always be allocated in the buffer</param>
    /// <param name="clean">whether to set all newly allocated bits to 0</param>
    public void Resize(int newLength, bool clean = false, int permAlloc = 8)
    {
        if (newLength < permAlloc) newLength = permAlloc;
        
        // resize tree buffer if needed
        var newBufferLength = (int)BitUtil.NextPowerOf2((ulong)newLength);
        if (newBufferLength > _bufferLength || newBufferLength <= _bufferLength/2)
        {
            _buffer = GlUtil.ResizeBuffer(_buffer, _bufferLength, newBufferLength, Hint, clean);
            _bufferLength = newLength;
        }
    }
    
    public void Dispose()
    {
        Shader.Dispose();
        
        GL.DeleteBuffer(_dataBuffer);
        GL.DeleteBuffer(_indexBuffer);
        GL.DeleteBuffer(_buffer);
    }
}
