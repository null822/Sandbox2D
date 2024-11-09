using System;
using System.Collections.Generic;
using System.Xml;
using OpenTK.Graphics.OpenGL4;
using Sandbox2D.Graphics;
using Sandbox2D.Registries;

namespace Sandbox2D.UserInterface.Elements;

public class TestElement : IGuiElement, IShaderController
{
    public Dictionary<string, string> Attributes { get; } = new();
    
    public ShaderProgram Shader { get; }
    public BufferUsageHint Hint { get; init; }
    
    public int VertexArrayObject { get; init; } = GL.GenVertexArray();
    public int VertexBufferObject { get; init; } = GL.GenBuffer();
    public int ElementBufferObject { get; init; } = GL.GenBuffer();
    
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
    
    public static GuiElementConstructor Constructor { get; } = args => new TestElement(args.Attributes, args.Children);
    public TestElement(List<XmlAttribute> attributes, ConsumableList<XmlNode> children)
    {
        children.ConsumeAll(n => n.Name == "#comment");
        
        foreach (var attribute in attributes)
        {
            Attributes.Add(attribute.Name, attribute.Value);
        }
        
        Shader = Registry.ShaderProgram.Create("noise");
        Hint = BufferUsageHint.StaticDraw;
        
        // update the vao (creates it, in this case)
        UpdateVao();
        
        // set up vertex coordinates
        var vertexLocation = Shader.GetAttribLocation("aPosition");
        GL.EnableVertexAttribArray(vertexLocation);
        GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
    }

    public void Render()
    {
        return;
        GL.BindVertexArray(VertexArrayObject);
        Shader.Use();
        GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
    }
    
    public void Invoke() => Render();
    
    public void Update()
    {
        return;
        if (Attributes.TryGetValue("onClick", out var eventName))
        {
            Registry.GuiEvent.Invoke(eventName);
        }
    }
    
    public void UpdateVao()
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
    
    public void ResetGeometry()
    {
        throw new NotImplementedException();
    }
}
