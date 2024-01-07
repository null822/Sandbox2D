#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics.OpenGL4;
using Sandbox2D.Maths;

namespace Sandbox2D.Graphics;

public class Renderable : IDisposable
{
    /// <summary>
    /// The shader to use
    /// </summary>
    private readonly Shader _shader;
    
    // rendering arrays/buffers
    private readonly int _vertexArrayObject;
    private readonly int _vertexBufferObject;
    private readonly int _elementBufferObject;

    private BufferUsageHint _hint;
    
    // geometry arrays
    private List<float> _vertices = 
    [
         0.5f,  0.5f, 0.0f, // top right
         0.5f, -0.5f, 0.0f, // bottom right
        -0.5f, -0.5f, 0.0f, // bottom left
        -0.5f,  0.5f, 0.0f  // top left
    ];
    private List<uint> _indices = 
    [
        0, 1, 3,   // first triangle
        1, 2, 3    // second triangle
    ];
    
    
    public Renderable(Shader shader, BufferUsageHint hint = BufferUsageHint.StaticDraw)
    {
        // set the hint
        _hint = hint;
        
        // create the vao
        _vertexArrayObject = GL.GenVertexArray();
        
        _vertexBufferObject = GL.GenBuffer();
        _elementBufferObject = GL.GenBuffer();
        
        // update the vao (creates it, in this case)
        UpdateVao(hint);
        
        // set the shader
        _shader = shader;
        
        // set up vertex coords
        var vertexLocation = _shader.GetAttribLocation("aPosition");
        GL.EnableVertexAttribArray(vertexLocation);
        GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
    }
    
    public virtual void Render()
    {
        GL.BindVertexArray(_vertexArrayObject);
        _shader.Use();
        GL.DrawElements(PrimitiveType.Triangles, _indices.Count, DrawElementsType.UnsignedInt, 0);
    }

    public void UpdateVao(BufferUsageHint? hint = null)
    {
        // update the hint if it is not null
        if (hint != null) _hint = (BufferUsageHint)hint;
        
        // bind vao
        GL.BindVertexArray(_vertexArrayObject);
        
        // bind/update vbo
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Count * sizeof(float), _vertices.ToArray(), _hint);
        
        // bind/update ebo (must be done after vbo)
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Count * sizeof(uint), _indices.ToArray(), _hint);
        
        Console.WriteLine($"{_indices.Count}i | {_vertices.Count / 3f}v | {_indices.Count / 3f}t");
    }

    /// <summary>
    /// Adds a quad to the geometry. Does not update the VAO
    /// </summary>
    /// <param name="tl">the top left vertex</param>
    /// <param name="br">the bottom right vertex</param>
    public void AddQuad(Vec2Float tl, Vec2Float br)
    {
        // get the next available index in _vertices
        var indexOffset = (uint)(_vertices.Count / 3f);
        
        // add the new vertices
        _vertices.AddRange(new []
        {
            br.X, tl.Y, 0.0f, // right top
            br.X, br.Y, 0.0f, // right bottom
            tl.X, br.Y, 0.0f, // left  bottom
            tl.X, tl.Y, 0.0f  // left  top
        });
        
        // create and add the new indices
        _indices.AddRange(new []
        {
            indexOffset + 0, indexOffset + 1, indexOffset + 3,   // first triangle
            indexOffset + 1, indexOffset + 2, indexOffset + 3    // second triangle
        });
    }

    /// <summary>
    /// Resets the geometry of this renderable. Does not update the VAO
    /// </summary>
    public void ResetGeometry()
    {
        _vertices.Clear();
        _indices.Clear();
    }

    /// <summary>
    /// Sets the new geometry and updates the VAO
    /// </summary>
    /// <param name="vertices">the new vertices array</param>
    /// <param name="indices">the new indices array</param>
    /// <param name="hint">a buffer usage hint to efficiently allocate gpu memory</param>
    public void SetGeometry(float[]? vertices = null, uint[]? indices = null, BufferUsageHint? hint = null)
    {
        // set arrays
        if (vertices != null) _vertices = vertices.ToList();
        if (indices != null) _indices = indices.ToList();
        
        // update the vao if something was changed
        if (vertices != null || indices != null) UpdateVao(hint);
    }

    public int GetVao()
    {
        return _vertexArrayObject;
    }

    public Shader GetShader()
    {
        return _shader;
    }
    
    public void Dispose()
    {
        _shader.Dispose();
        GC.SuppressFinalize(this);
    }
    
}