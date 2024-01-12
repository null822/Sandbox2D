using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using Sandbox2D.Maths;
using Vector2 = OpenTK.Mathematics.Vector2;

namespace Sandbox2D.Graphics.Renderables;

public class GameObjectRenderable : Renderable
{
    private Vector2 _translation = Vector2.Zero;
    private float _scale = 1;

    // geometry arrays
    private readonly List<double> _vertices = 
    [
        0.5f,  0.5f, // top right
        0.5f, -0.5f, // bottom right
        -0.5f, -0.5f, // bottom left
        -0.5f,  0.5f  // top left
    ];
    private readonly List<uint> _indices = 
    [
        0, 1, 3,   // first triangle
        1, 2, 3    // second triangle
    ];
    
    public GameObjectRenderable(Shader shader, BufferUsageHint hint = BufferUsageHint.StaticDraw) : base(shader, hint)
    {
        // update the vao (creates it, in this case)
        UpdateVao(hint);
        
        // set up vertex coords
        var vertexLocation = Shader.GetAttribLocation("worldPos");
        GL.EnableVertexAttribArray(vertexLocation);
        GL.VertexAttribPointer(vertexLocation, 2, VertexAttribPointerType.Double, false, 2 * sizeof(double), 0);
        
        // set up scale/translation/screenSize uniforms
        var scaleLocation = GL.GetUniformLocation(Shader.Handle, "scale");
        GL.Uniform1(scaleLocation, 1, ref _scale);
        
        var translationLocation = GL.GetUniformLocation(Shader.Handle, "translation");
        GL.Uniform2(translationLocation, ref _translation);
        
        Vector2 screenSize = Program.Get().ClientSize;
        var screenSizeLocation = GL.GetUniformLocation(Shader.Handle, "screenSize");
        GL.Uniform2(screenSizeLocation, ref screenSize);
        
    }

    public override void Render()
    {
        GL.BindVertexArray(VertexArrayObject);
        Shader.Use();
        
        // set the uniforms
        Shader.SetFloat("scale", _scale);
        Shader.SetVector2("translation", _translation);
        Shader.SetVector2("screenSize", Program.Get().ClientSize);
        
        Console.WriteLine($"tri: {_indices.Count / 3f}");
        
        GL.DrawElements(PrimitiveType.Triangles, _indices.Count, DrawElementsType.UnsignedInt, 0);
        var error = GL.GetError();

        if (error != ErrorCode.NoError)
        {
            Util.Error($"OpenGL Error {error.ToString()}");
        }
    }

    public sealed override void UpdateVao(BufferUsageHint? hint = null)
    {
        base.UpdateVao(hint);
        
        // bind vao
        GL.BindVertexArray(VertexArrayObject);
        
        // bind/update vbo
        GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Count * sizeof(double), _vertices.ToArray(), Hint);
        
        // bind/update ebo (must be done after vbo)
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Count * sizeof(uint), _indices.ToArray(), Hint);
    }

    public override void ResetGeometry()
    {
        _vertices.Clear();
        _indices.Clear();
    }

    /// <summary>
    /// Adds a quad to the geometry. Does not update the VAO
    /// </summary>
    /// <param name="tl">the top left vertex</param>
    /// <param name="br">the bottom right vertex</param>
    public void AddQuad(Vec2<double> tl, Vec2<double> br)
    {
        // get the next available index in Vertices
        var indexOffset = (uint)(_vertices.Count / 2f);
        
        // add the new vertices
        _vertices.AddRange(new []
        {
            br.X, tl.Y, // right top
            br.X, br.Y, // right bottom
            tl.X, br.Y, // left  bottom
            tl.X, tl.Y  // left  top
        });
        
        // create and add the new indices
        _indices.AddRange(new []
        {
            indexOffset + 0, indexOffset + 1, indexOffset + 3,   // first triangle
            indexOffset + 1, indexOffset + 2, indexOffset + 3    // second triangle
        });
    }

    public void SetTranslation(Vec2<double> translation)
    {
        _translation = translation;
    }
    
    public void SetScale(float scale)
    {
        _scale = scale;
    }


}
