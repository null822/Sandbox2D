using System;
using System.Xml;
using OpenTK.Graphics.OpenGL4;
using Sandbox2D.Registry;

namespace Sandbox2D.UserInterface.Elements;

public class TestElement : GuiElement
{
    // geometry arrays
    private readonly float[] _vertices = 
    [
        0.5f,  0.5f, 0.0f, // top right
        0.5f, -0.5f, 0.0f, // bottom right
        -0.5f, -0.5f, 0.0f, // bottom left
        -0.5f,  0.5f, 0.0f  // top left
    ];
    private readonly uint[] _indices = 
    [
        0, 1, 3,   // first triangle
        1, 2, 3    // second triangle
    ];
    
    public TestElement(XmlAttributeCollection attributes) : base(attributes, Shaders.Noise)
    {
        // update the vao (creates it, in this case)
        UpdateVao();
        
        // set up vertex coords
        var vertexLocation = Shader.GetAttribLocation("aPosition");
        GL.EnableVertexAttribArray(vertexLocation);
        GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
    }
    
    public override void Render()
    {
        GL.BindVertexArray(VertexArrayObject);
        Shader.Use();
        GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
    }
    
    public override void Update()
    {
        return;
        if (Attributes.TryGetValue("onClick", out var eventName))
        {
            GuiEvents.Invoke(eventName);
        }
    }
    
    public sealed override void UpdateVao()
    {
        // bind vao
        GL.BindVertexArray(VertexArrayObject);
        
        // bind/update vbo
        GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, Hint);
        
        // bind/update ebo (must be done after vbo)
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, Hint);
    }
    
    public override void ResetGeometry()
    {
        throw new NotImplementedException();
    }
}
