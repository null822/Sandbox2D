using System;
using System.Collections.Generic;
using Math2D;
using Math2D.Binary;
using OpenTK.Graphics.OpenGL4;
using Sandbox2D.Registry_;

namespace Sandbox2D.Graphics.Buffers;

public class DynamicGpuBuffer : IGpuBuffer
{
    public GpuBuffer Buffer { get; private set; }
    private readonly BufferUsageHint _hint;
    
    public int Handle => Buffer.Handle;
    
    /// <summary>
    /// The size, in bytes, of the buffer
    /// </summary>
    public int Length { get; private set; }
    
    private readonly GpuBuffer _patchDataBuffer;
    private readonly GpuBuffer _patchIndexBuffer;
    private readonly ShaderProgram _patchShader;
    
    private readonly byte[] _dataArr;
    private readonly uint[] _indexArr;
    private int _currentBatchLength;

    public DynamicGpuBuffer(BufferUsageHint hint = BufferUsageHint.StaticDraw)
    {
        _patchShader = GlContext.Registry.ShaderProgram.Create("data_patch");
        _hint = hint;
        Buffer = new GpuBuffer(0, false, _hint); // create the buffer
        
        const int uploadBatchSize = Constants.GpuUploadBatchSize;
        _indexArr = new uint[uploadBatchSize / 4];
        _dataArr = new byte[uploadBatchSize];

        _patchDataBuffer = new GpuBuffer(uploadBatchSize);
        _patchIndexBuffer = new GpuBuffer(uploadBatchSize);
        _patchDataBuffer.Bind(BufferTarget.CopyWriteBuffer);
        GL.BufferData(BufferTarget.CopyWriteBuffer, uploadBatchSize, new byte[uploadBatchSize], hint);
        _patchIndexBuffer.Bind(BufferTarget.CopyWriteBuffer);
        GL.BufferData(BufferTarget.CopyWriteBuffer, uploadBatchSize, new byte[uploadBatchSize], hint);
    }
    
    private void ApplyModifications(uint dataSize)
    {
        // bind buffers
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, Handle);
        _patchDataBuffer.Bind(BufferRangeTarget.ShaderStorageBuffer, 1);
        _patchIndexBuffer.Bind(BufferRangeTarget.ShaderStorageBuffer, 2);
        
        // use the shader
        _patchShader.Use();
        
        _patchShader.Set("_dataSize", dataSize / 4);
        _patchShader.Set("_totalCount", (uint)_currentBatchLength);
        
        // invoke the shader
        GL.DispatchCompute((int)Math.Ceiling(_currentBatchLength / 64.0), 1, 1);
        
        // unbind the buffers
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
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

    public void Write<T>(T value, int index) where T : IByteSerializable
    {
        GL.BindBuffer(BufferTarget.CopyWriteBuffer, Handle);

        var tSize = T.SerializeLength;
        
        GL.BufferSubData(
            BufferTarget.CopyWriteBuffer,
            checked((IntPtr)index * tSize),
            (IntPtr)tSize,
            value.Serialize(true));
        
        GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
    }
    
    /// <summary>
    /// Modifies the contents of the buffer.
    /// </summary>
    /// <param name="modifications">the modifications to the buffer section to apply</param>
    /// <param name="batchStart">index within the modifications to start applying at</param>
    /// <returns>1 more than the index (within <paramref name="modifications"/>) of the last modification that was
    /// applied</returns>
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
        
        _patchDataBuffer.Bind(BufferTarget.CopyWriteBuffer);
        GL.BufferSubData(BufferTarget.CopyWriteBuffer,
            IntPtr.Zero,
            checked((IntPtr)(_currentBatchLength * dataSize)),
            _dataArr);
        
        _patchIndexBuffer.Bind(BufferTarget.CopyWriteBuffer);
        GL.BufferSubData(BufferTarget.CopyWriteBuffer,
            IntPtr.Zero,
            checked((IntPtr)(_currentBatchLength * sizeof(uint))),
            _indexArr);
        GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
        
        ApplyModifications((uint)dataSize);
        
        return batchEnd;
    }
    
    public void Clear()
    {
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, Handle);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, 0, Array.Empty<byte>(), _hint);
        Length = 0;
        _currentBatchLength = 0;
    }
    
    /// <summary>
    /// Resizes the buffer to the supplied size.
    /// </summary>
    /// <param name="length">the new size of the buffer, in bytes</param>
    /// <param name="clean">whether to set all newly allocated bits to 0</param>
    /// <param name="permAlloc">a size, in bytes, that will always be allocated in the buffer</param>
    public void Resize(int length, bool clean = false, int permAlloc = 8)
    {
        if (length < permAlloc) length = permAlloc;
        
        // resize tree buffer if needed
        var totalBufferLength = (int)BitUtil.NextPowerOf2((ulong)length);
        if (totalBufferLength > Length || totalBufferLength <= Length/2)
        {
            var oldBuffer = Buffer;
            oldBuffer.Bind(BufferTarget.CopyReadBuffer);
            Buffer = new GpuBuffer(totalBufferLength, clean);
            Buffer.Bind(BufferTarget.CopyWriteBuffer);
            GL.CopyBufferSubData(BufferTarget.CopyReadBuffer, BufferTarget.CopyWriteBuffer, 
                IntPtr.Zero, IntPtr.Zero, Math.Min(Length, totalBufferLength));
            
            Length = length;
        }
    }
    
    public void Dispose()
    {
        _patchShader.Dispose();
        
        Buffer.Dispose();
        _patchDataBuffer.Dispose();
        _patchIndexBuffer.Dispose();
    }
}
