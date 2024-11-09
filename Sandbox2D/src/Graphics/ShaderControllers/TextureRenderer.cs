#nullable enable
using System.Collections.Generic;
using Math2D;
using OpenTK.Graphics.OpenGL4;

namespace Sandbox2D.Graphics.ShaderControllers;

public class TextureRenderer : IShaderController
{
    public ShaderProgram Shader { get; }
    public BufferUsageHint Hint { get; init; }
    
    public int VertexArrayObject { get; init; } = GL.GenVertexArray();
    public int VertexBufferObject { get; init; } = GL.GenBuffer();
    public int ElementBufferObject { get; init; } = GL.GenBuffer();

    private Texture _texture;
    
    // geometry arrays
    private readonly List<float> _vertices = 
    [
         0.5f,  0.5f, 0.0f,  1.2f,  1.2f, // top right
         0.5f, -0.5f, 0.0f,  1.2f, -0.2f, // bottom right
        -0.5f, -0.5f, 0.0f, -0.2f, -0.2f, // bottom left
        -0.5f,  0.5f, 0.0f, -0.2f,  1.2f  // top left
    ];
    private readonly List<uint> _indices = 
    [
        0, 1, 3,   // first triangle
        1, 2, 3    // second triangle
    ];
    
    public TextureRenderer(ShaderProgram shader, Texture texture, BufferUsageHint hint = BufferUsageHint.StaticDraw)
    {
        Shader = shader;
        _texture = texture;
        Hint = hint;
        
        // update the vao (creates it, in this case)
        UpdateVao();
        
        // set up vertex coords
        var vertexLocation = Shader.GetAttribLocation("aPosition");
        GL.EnableVertexAttribArray(vertexLocation);
        GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        
        var texCoordLocation = Shader.GetAttribLocation("aTexCoord");
        GL.EnableVertexAttribArray(texCoordLocation);
        GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
    }
    
    public void Invoke()
    {
        GL.BindVertexArray(VertexArrayObject);
        _texture.Use(TextureUnit.Texture0);
        Shader.Use();
        GL.DrawElements(PrimitiveType.Triangles, _indices.Count, DrawElementsType.UnsignedInt, 0);
    }
    
    public void UpdateVao()
    {
        // bind vao
        GL.BindVertexArray(VertexArrayObject);
        
        // bind/update vbo
        GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Count * sizeof(float), _vertices.ToArray(), Hint);
        
        // bind/update ebo (must be done after vbo)
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Count * sizeof(uint), _indices.ToArray(), Hint);
    }
    
    /// <summary>
    /// Adds a quad to the geometry. Does not update the VAO
    /// </summary>
    /// <param name="tl">the top left vertex</param>
    /// <param name="br">the bottom right vertex</param>
    public void AddQuad(Vec2<float> tl, Vec2<float> br)
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
    
}