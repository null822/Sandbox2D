using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Math2D;
using Sandbox2D.Graphics.ShaderControllers;
using Sandbox2D.Registry_;
using Sandbox2D.Registry_.Registries;

namespace Sandbox2D.UserInterface.Elements;

public class TextElement : IGuiElement
{
    private TextRenderer _textRenderer;
    private Color _textColor;
    private string _text;
    
    public TextElement(GuiElementArguments args) : base(args)
    {
        
        var textNodes = args.Children.ConsumeAll(n => n.Name == "#text");
        var lines = new StringBuilder("asdf");
        foreach (var node in textNodes)
        {
            lines.Append($"{node.Value}\n");
        }
        if (lines.Length != 0)
            lines.Remove(lines.Length - 1, 1);
        
        _textColor = new Color(Attributes.GetValueOrDefault("color", "#FFFFFF"));
        Console.WriteLine(_textColor);
        _text = lines.ToString();
    }

    public override void GlInitialize()
    {
        _textRenderer = new TextRenderer(GlRegistry.ShaderProgram.Create("text"));
    }
    
    public override void Render()
    {
        _textRenderer.SetColor(_textColor);
        _textRenderer.SetText(_text, (4, 4), 2, (600, 800)); // TODO: get the actual screen size
        
        _textRenderer.Invoke();
    }
    
    public void SetText(string text)
    {
        _text = text;
    }
    
    public void SetColor(Color color)
    {
        _textColor = color;
    }
}
