using System.Collections.Generic;
using System.Xml;
using OpenTK.Graphics.OpenGL4;
using Sandbox2D.Graphics;

namespace Sandbox2D.UserInterface;

public abstract class GuiElement : IRenderable
{
    public Shader Shader { get; }
    public BufferUsageHint Hint { get; init; }
    
    public int VertexArrayObject { get; init; } = GL.GenVertexArray();
    public int VertexBufferObject { get; init; } = GL.GenBuffer();
    public int ElementBufferObject { get; init; } = GL.GenBuffer();
    
    protected readonly Dictionary<string, string> Attributes = new();
    
    protected GuiElement(XmlAttributeCollection attributes, Shader shader, BufferUsageHint hint = BufferUsageHint.StaticDraw)
    {
        Shader = shader;
        Hint = hint;
        
        for (var i = 0; i < attributes.Count; i++)
        {
            var attribute = attributes[i];
            Attributes.Add(attribute.Name, attribute.Value);
        }
    }
    
    public void SetAttribute(string attribute, string value)
    {
        Attributes.TryAdd(attribute, value);
        Attributes[attribute] = value;
    }
    
    public string GetAttribute(string attribute)
    {
        return Attributes.GetValueOrDefault(attribute, "");
    }
    
    public abstract void Render();
    public abstract void Update();
    public abstract void UpdateVao();
    public abstract void ResetGeometry();
}