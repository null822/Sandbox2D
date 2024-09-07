using System;
using System.Runtime.InteropServices;
using Math2D;
using Math2D.Quadtree;
using OpenTK.Graphics.OpenGL4;
using Sandbox2D.Registry;
using Sandbox2D.World;
using Vector2 = OpenTK.Mathematics.Vector2;

namespace Sandbox2D.Graphics.Renderables;

public class QuadtreeRenderable : IRenderable
{
    public Shader Shader { get; }
    public BufferUsageHint Hint { get; init; }
    
    public int VertexArrayObject { get; init; } = GL.GenVertexArray();
    public int VertexBufferObject { get; init; } = GL.GenBuffer();
    public int ElementBufferObject { get; init; } = GL.GenBuffer();

    private Vector2 _translation = Vector2.Zero;
    private float _scale = 1;
    
    private int _maxHeight;
    private static Vector2 ScreenSize => Util.Vec2ToVector2(GameManager.ScreenSize);
    
    private readonly Texture _dynTilemap;
    
    private int _treeBufferLength;
    private int _dataBufferLength;
    
    /// <summary>
    /// The buffer that contains the tree array of the quadtree to be rendered
    /// </summary>
    private int _treeBuffer;

    /// <summary>
    /// The buffer that contains the data array of the quadtree to be rendered
    /// </summary>
    private int _dataBuffer;
    
    // geometry arrays
    private readonly float[] _vertices =
    [
         1f,  1f, 0.0f, // top right
         1f, -1f, 0.0f, // bottom right
        -1f, -1f, 0.0f, // bottom left
        -1f,  1f, 0.0f  // top left
    ];
    private readonly uint[] _indices =
    [
        0, 1, 3,   // first triangle
        1, 2, 3    // second triangle
    ];
    
    /// <summary>
    /// The size, in bytes, of a single instance of <see cref="QuadtreeNode"/>
    /// </summary>
    private static readonly int QuadtreeNodeSize = Marshal.SizeOf<QuadtreeNode>();
    /// <summary>
    /// The size, in bytes, of a single instance of <see cref="TileData"/>
    /// </summary>
    private static readonly int TileDataSize = Marshal.SizeOf<TileData>();
    
    public QuadtreeRenderable(Shader shader, int quadtreeMaxHeight, BufferUsageHint hint = BufferUsageHint.StaticDraw)
    {
        Shader = shader;
        Hint = hint;
        
        if (quadtreeMaxHeight > 16) throw new Exception($"Invalid Max Height for Quadtree. Was: {quadtreeMaxHeight}, Range: 2-16.");
        _maxHeight = quadtreeMaxHeight;
        
        _treeBuffer = GL.GenBuffer();
        _dataBuffer = GL.GenBuffer();
        
        // update the vao (creates it, in this case)
        UpdateVao();
        
        // set up vertex coords
        var vertexLocation = Shader.GetAttribLocation("aPosition");
        GL.EnableVertexAttribArray(vertexLocation);
        GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(int), 0);
        
        Shader.Use();
        
        int loc;
        // set up uniforms
        loc = GL.GetUniformLocation(Shader.Handle, "Scale");
        GL.Uniform1(loc, _scale);
        
        loc = GL.GetUniformLocation(Shader.Handle, "Translation");
        GL.Uniform2(loc, _translation);
        
        loc = GL.GetUniformLocation(Shader.Handle, "ScreenSize");
        GL.Uniform2(loc, ScreenSize);
        
        loc = GL.GetUniformLocation(Shader.Handle, "MaxHeight");
        GL.Uniform1(loc, _maxHeight);
        
        _dynTilemap = Textures.DynTilemap;
        _dynTilemap.Use(TextureUnit.Texture0);
        
        // initialize the buffers
        ResizeBuffers(8, 8);
    }

    public void Render()
    {
        // bind vao / ssbos
        GL.BindVertexArray(VertexArrayObject);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, _treeBuffer);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, _dataBuffer);
        
        // apply textures
        _dynTilemap.Use(TextureUnit.Texture0);
        
        // use the shader
        Shader.Use();
        
        // set the uniforms
        Shader.SetFloat("Scale", _scale);
        Shader.SetVector2("Translation", _translation);
        Shader.SetVector2("ScreenSize", GlobalVariables.RenderManager.ClientSize);
        Shader.SetInt("MaxHeight", _maxHeight);
        
        // render the geometry
        GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
        
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
    }
    
    public void UpdateVao()
    {
        // bind VAO
        GL.BindVertexArray(VertexArrayObject);
        
        // bind/update VBO
        GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(int), _vertices, Hint);
        
        // bind/update EBO (must be done after VBO)
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, Hint);
    }

    /// <summary>
    /// Updates the geometry, modifying the existing buffers
    /// </summary>
    /// <param name="tree">the modifications to the tree section to upload</param>
    /// <param name="data">the modifications to the tree section to upload</param>
    /// <param name="treeLength">the total length of the tree section</param>
    /// <param name="dataLength">the total length of the data section</param>
    /// <param name="treeIndex">index within the tree modifications to start uploading at</param>
    /// <param name="dataIndex">index within the data modifications to start uploading at</param>
    /// <param name="renderRoot">the root node for rendering</param>
    public unsafe void SetGeometry(
        ref DynamicArray<ArrayModification<QuadtreeNode>> tree, ref DynamicArray<ArrayModification<Tile>> data,
        long treeLength, long dataLength,
        ref long treeIndex, ref long dataIndex,
        QuadtreeNode renderRoot)
    {
        // resize buffers if needed
        ResizeBuffers(treeLength, dataLength);
        
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _treeBuffer);
        var tMax = Math.Min(tree.Length, treeIndex + Constants.GpuUploadBatchSize);
        for (var i = treeIndex; i < tMax; i++)
        {
            var element = tree[i];
            
            // skip elements residing outside the buffer
            if (element.Index >= treeLength) continue;
            
            GL.BufferSubData(
                BufferTarget.ShaderStorageBuffer,
                (IntPtr)element.Index * sizeof(QuadtreeNode),
                1 * sizeof(QuadtreeNode),
                [element.Value]);
        }
        // upload the render root
        GL.BufferSubData(
            BufferTarget.ShaderStorageBuffer,
            (IntPtr)0 * sizeof(QuadtreeNode),
            1 * sizeof(QuadtreeNode),
            [renderRoot]);
        
        treeIndex = tMax;
        
        if (data.Length != 0)
        {
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _dataBuffer);
            var dMax = Math.Min(data.Length, dataIndex + Constants.GpuUploadBatchSize);
            for (var i = dataIndex; i < dMax; i++)
            {
                var element = data[i];
                
                // skip elements residing outside the buffer
                if (element.Index >= dataLength) continue;
                
                GL.BufferSubData(
                    BufferTarget.ShaderStorageBuffer,
                    (IntPtr)element.Index * element.Value.SerializeLength,
                    1 * element.Value.SerializeLength,
                    [element.Value.GpuSerialize()]);
            }
            dataIndex = dMax;
            
        }
        
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
    }
    
    public void SetTransform(Vec2<float> translation, float scale)
    {
        _translation = Util.Vec2ToVector2(translation);
        _scale = scale;
    }
    
    public void ResetGeometry()
    {
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _treeBuffer);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, 0, Array.Empty<QuadtreeNode>(), Hint);
        _treeBufferLength = 0;
        
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _dataBuffer);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, 0, Array.Empty<TileData>(), Hint);
        _dataBufferLength = 0;
    }

    public void SetMaxHeight(int maxHeight)
    {
        _maxHeight = maxHeight;
    }
    
    private void ResizeBuffers(long treeLength, long dataLength)
    {
        if (treeLength < 8) treeLength = 8;
        if (dataLength < 8) dataLength = 8;
        
        // resize tree buffer if needed
        var newTreeBufferLength = (int)BitUtil.NextPowerOf2((ulong)treeLength);
        if (newTreeBufferLength > _treeBufferLength || newTreeBufferLength <= _treeBufferLength/2)
        {
            (_treeBuffer, _treeBufferLength) =
                ResizeBuffer<QuadtreeNode>(_treeBuffer, _treeBufferLength, newTreeBufferLength, QuadtreeNodeSize);
        }
        
        // resize data buffer if needed
        var newDataBufferLength = (int)BitUtil.NextPowerOf2((ulong)dataLength);
        if (newDataBufferLength > _dataBufferLength || newDataBufferLength <= _dataBufferLength/2)
        {
            (_dataBuffer, _dataBufferLength) =
                ResizeBuffer<TileData>(_dataBuffer, _dataBufferLength, newDataBufferLength, TileDataSize);
        }
    }

    private (int NewBuffer, int NewBufferLength) ResizeBuffer<T>(int buffer, int currentLength, int newLength, int typeSize) where T : struct
    {
        // bind the old buffer to `CopyReadBuffer`
        GL.BindBuffer(BufferTarget.CopyReadBuffer, buffer);
        
        // allocate data for a new buffer
        var newBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.CopyWriteBuffer, newBuffer);
        GL.BufferData(BufferTarget.CopyWriteBuffer, newLength * typeSize, Array.Empty<T>(), Hint);
        
        // copy all the data from `CopyReadBuffer` to `CopyWriteBuffer`
        var copySize = Math.Min(currentLength, newLength) * typeSize;
        GL.CopyBufferSubData(BufferTarget.CopyReadBuffer, BufferTarget.CopyWriteBuffer, IntPtr.Zero, IntPtr.Zero, copySize);
        
        // unbind buffer / delete old buffer
        GL.DeleteBuffer(buffer);
        GL.BindBuffer(BufferTarget.CopyReadBuffer, 0);
        GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
        
        return (newBuffer, newLength);
    }
    
    public void Dispose()
    {
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        GL.DeleteBuffer(_treeBuffer);
        GL.DeleteBuffer(_dataBuffer);
    }
}
