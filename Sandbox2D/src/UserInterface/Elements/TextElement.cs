using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Math2D;
using Sandbox2D.Graphics.ShaderControllers;
using Sandbox2D.Registries;

namespace Sandbox2D.UserInterface.Elements;

public class TextElement : IGuiElement
{
    public Dictionary<string, string> Attributes { get; } = new ();
    
    private readonly TextRenderer _textRenderer = new(Registry.ShaderProgram.Create("text"));
    
    public static GuiElementConstructor Constructor { get; } = args => new TextElement(args.Attributes, args.Children);
    public TextElement(List<XmlAttribute> attributes, ConsumableList<XmlNode> children)
    {
        children.ConsumeAll(n => n.Name == "#comment");
        
        foreach (var attribute in attributes)
        {
            Attributes.Add(attribute.Name, attribute.Value);
        }
        
        var textNodes = children.ConsumeAll(n => n.Name == "#text");
        var lines = new StringBuilder("asdf");
        foreach (var node in textNodes)
        {
            lines.Append($"{node.Value}\n");
        }
        if (lines.Length != 0)
            lines.Remove(lines.Length - 1, 1);
        
        var color = new Color(Attributes.GetValueOrDefault("color", "#FFFFFF"));
        _textRenderer.SetColor(color);
        Console.WriteLine(color);
        _textRenderer.SetText("hello", (4, 4), 2);
        _textRenderer.SetText("hello", (50, 50), 4);
    }
    
    public void Render()
    {
        // _textRenderable.SetText("hello", (50, 50), 4);
        _textRenderer.Invoke();
    }
    
    public void Update()
    {
        
    }
    
    public void UpdateVao()
    {
        _textRenderer.UpdateVao();
    }
    
    public void ResetGeometry()
    {
        // _textRenderable.ResetGeometry();
    }
}
