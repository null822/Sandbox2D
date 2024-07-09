using System;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using Sandbox2D.Graphics.Registry;
using Sandbox2D.Maths;
using Sandbox2D.Maths.Quadtree;
using Sandbox2D.World;
using Vector2 = OpenTK.Mathematics.Vector2;
using static Sandbox2D.Util;

namespace Sandbox2D.Graphics.Renderables;

public class QuadtreeRenderable : Renderable
{
    private Vector2 _translation = Vector2.Zero;
    private float _scale = 1;
    
    private static readonly int MaxHeight = Math.Min(Constants.WorldHeight, 16);
    
    private readonly Texture _dynTilemap;

    private int _treeBufferLength;
    private int _dataBufferLength;
    
    /// <summary>
    /// The buffer that contains the tree array of the quadtree to be rendered
    /// </summary>
    private int _treeBuffer = GL.GenBuffer();
    
    /// <summary>
    /// The buffer that contains the data array of the quadtree to be rendered
    /// </summary>
    private int _dataBuffer = GL.GenBuffer();
    
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

    private static readonly int QuadTreeNodeSize = Marshal.SizeOf(new QuadtreeNode());
    private static readonly int TileSize = Marshal.SizeOf(new TileData());
    
    public QuadtreeRenderable(Shader shader, BufferUsageHint hint = BufferUsageHint.StaticDraw) : base(shader, hint)
    {
        // update the vao (creates it, in this case)
        UpdateVao(hint);
        
        // set up vertex coords
        var vertexLocation = Shader.GetAttribLocation("aPosition");
        GL.EnableVertexAttribArray(vertexLocation);
        GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(int), 0);
        
        int loc;
        
        // set up uniforms
        loc = GL.GetUniformLocation(Shader.Handle, "Scale");
        GL.Uniform1(loc, 1, ref _scale);
        
        loc = GL.GetUniformLocation(Shader.Handle, "Translation");
        GL.Uniform2(loc, ref _translation);
        
        loc = GL.GetUniformLocation(Shader.Handle, "ScreenSize");
        GL.Uniform2(loc, GameManager.ScreenSize);
        
        loc = GL.GetUniformLocation(Shader.Handle, "MaxHeight");
        GL.Uniform1(loc, MaxHeight);
        
        _dynTilemap = Textures.DynTilemap;
        _dynTilemap.Use(TextureUnit.Texture0);
        
        // initialize the buffers
        ResizeBuffers(8, 8);
        
        PrintGlErrors();
    }
    
    public override void Render(RenderableCategory category = RenderableCategory.All)
    {
        if (!IsInCategory(category) || !ShouldRender)
            return;
        
        base.Render(category);
        
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
        Shader.SetVector2("ScreenSize", Program.Get().ClientSize);
        Shader.SetInt("MaxHeight", MaxHeight);
        
        // render the geometry
        GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
        
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

        PrintGlErrors();
    }
    
    public sealed override void UpdateVao(BufferUsageHint? hint = null, RenderableCategory category = RenderableCategory.All)
    {
        if (!IsInCategory(category))
            return;

        base.UpdateVao(hint, category);
        
        // bind vao
        GL.BindVertexArray(VertexArrayObject);
        
        // bind/update vbo
        GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(int), _vertices, Hint);
        
        // bind/update ebo (must be done after vbo)
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, Hint);
        
        PrintGlErrors();
    }

    /// <summary>
    /// Updates the geometry, modifying the existing buffers
    /// </summary>
    /// <param name="modifications">the modifications to the tree and data buffers</param>
    /// <param name="treeLength">the total length of the tree</param>
    /// <param name="dataLength">the total length of the data</param>
    /// <param name="renderRoot">the root node for rendering</param>
    public unsafe void SetGeometry(QuadtreeModifications<Tile> modifications, int treeLength, int dataLength, QuadtreeNode renderRoot)
    {
        // resize buffers if needed
        ResizeBuffers(treeLength, dataLength);
        
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _treeBuffer);
        var treePtr = (QuadtreeNode*)GL.MapBuffer(BufferTarget.ShaderStorageBuffer, BufferAccess.ReadWrite).ToPointer();
        
        
        var tree = modifications.Tree;
        foreach (var element in tree)
        {
            treePtr[element.Index] = element.Value;
        }
        Array.Clear(tree);
        
        // upload the render root
        treePtr[0] = renderRoot;
        
        GL.UnmapBuffer(BufferTarget.ShaderStorageBuffer);
        PrintGlErrors();
        
        var data = modifications.Data;
        if (data.Length != 0)
        {
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _dataBuffer);
            
            var dataPtr = (TileData*)GL.MapBuffer(BufferTarget.ShaderStorageBuffer, BufferAccess.WriteOnly).ToPointer();
            
            foreach (var element in data)
            {
                dataPtr[element.Index] = element.Value.GpuSerialize();
            }
            Array.Clear(data);

            GL.UnmapBuffer(BufferTarget.ShaderStorageBuffer);
            
            PrintGlErrors();
        }
        
        
        ShouldRender = true;
    }
    
    /// <summary>
    /// Sets the new geometry and updates the VAO
    /// </summary>
    /// <param name="tree">the new tree buffer</param>
    /// <param name="data">the new data buffer</param>
    public void SetGeometry(QuadtreeNode[] tree, uint[] data)
    {
        var newTreeLength = (int)NextPowerOf2((ulong)tree.Length);
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _treeBuffer);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, newTreeLength * QuadTreeNodeSize, tree, Hint);
        _treeBufferLength = newTreeLength;
        
        var newDataLength = (int)NextPowerOf2((ulong)data.Length);
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _dataBuffer);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, newDataLength * sizeof(uint), data, Hint);
        _dataBufferLength = newDataLength;
        
        PrintGlErrors();
        
        ShouldRender = tree.Length != 0;
    }
    
    public void SetTransform(Vec2<float> translation, float scale)
    {
        _translation = translation;
        _scale = scale;
    }
    
    public override void ResetGeometry(RenderableCategory category = RenderableCategory.All)
    {
        if (!IsInCategory(category))
            return;
        
        base.ResetGeometry(category);
        
        
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _treeBuffer);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, 0, Array.Empty<QuadtreeNode>(), Hint);
        _treeBufferLength = 0;
        
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _dataBuffer);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, 0, Array.Empty<uint>(), Hint);
        _dataBufferLength = 0;
        
        PrintGlErrors();
    }
    
    private void ResizeBuffers(int treeLength, int dataLength)
    {
        if (treeLength < 8) treeLength = 8;
        if (dataLength < 8) dataLength = 8;
        
        // resize tree buffer if needed
        var newTreeBufferLength = (int)NextPowerOf2((ulong)treeLength);
        if (newTreeBufferLength > _treeBufferLength || newTreeBufferLength <= _treeBufferLength/2)
        {
            // bind the old buffer to `CopyReadBuffer`
            GL.BindBuffer(BufferTarget.CopyReadBuffer, _treeBuffer);
            
            // allocate data for a new buffer
            var newBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.CopyWriteBuffer, newBuffer);
            GL.BufferData(BufferTarget.CopyWriteBuffer, newTreeBufferLength * QuadTreeNodeSize, Array.Empty<QuadtreeNode>(), Hint);
            
            var copySize = Math.Min(_treeBufferLength, newTreeBufferLength) * QuadTreeNodeSize;
            
            // copy all the data from `CopyReadBuffer` to `CopyWriteBuffer`
            GL.CopyBufferSubData(BufferTarget.CopyReadBuffer, BufferTarget.CopyWriteBuffer, 0, 0, copySize);
            
            // unbind/delete buffers
            GL.DeleteBuffer(_treeBuffer);
            GL.BindBuffer(BufferTarget.CopyReadBuffer, 0);
            GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
            
            _treeBuffer = newBuffer;
            _treeBufferLength = newTreeBufferLength;
            
            PrintGlErrors();
        }
        
        // resize data buffer if needed
        var newDataBufferLength = (int)NextPowerOf2((ulong)dataLength);
        if (newDataBufferLength > _dataBufferLength || newDataBufferLength <= _dataBufferLength/2)
        {
            // bind the old buffer to `CopyReadBuffer`
            GL.BindBuffer(BufferTarget.CopyReadBuffer, _dataBuffer);
            
            // allocate data for a new buffer
            var newBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.CopyWriteBuffer, newBuffer);
            GL.BufferData(BufferTarget.CopyWriteBuffer, newDataBufferLength * TileSize, Array.Empty<TileData>(), Hint);
            
            var copySize = Math.Min(_dataBufferLength, newDataBufferLength) * TileSize;
            
            // copy all the data from `CopyReadBuffer` to `CopyWriteBuffer`
            GL.CopyBufferSubData(BufferTarget.CopyReadBuffer, BufferTarget.CopyWriteBuffer, 0, 0, copySize);
            
            // unbind/delete buffers
            GL.DeleteBuffer(_dataBuffer);
            GL.BindBuffer(BufferTarget.CopyReadBuffer, 0);
            GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);
            
            _dataBuffer = newBuffer;
            _dataBufferLength = newDataBufferLength;
            
            PrintGlErrors();
        }
    }
    
}
