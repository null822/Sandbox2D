using System.Collections.Generic;
using Math2D;
using Sandbox2D.Graphics.ShaderControllers;
using Sandbox2D.Registry_;
using Sandbox2D.Registry_.Registries;

namespace Sandbox2D.UserInterface.Elements;

public class TextElement : IGuiElement
{
    public override string Id => "text";
    
    private readonly TextRenderer _textRenderer;
    private Color _textColor;
    private string _text;
    
    public TextElement(GuiElementArgs args) : base(args)
    {
        _textRenderer = new TextRenderer(GlContext.Registry.ShaderProgram.Create("text"));
        _textColor = new Color(Attributes.GetValueOrDefault("color", "#FFFFFF"));
        var textAttrib = args.Attributes.Find(a => a.Name == "text")?.Value;
        _text = textAttrib ?? args.Value ?? "";
    }
    
    public override void Render(Vec2<int> parentPos)
    {
        _textRenderer.SetColor(_textColor);
        _textRenderer.SetText(_text, (4, 4), 2, GlContext.RenderManager.ScreenSize);
        
        _textRenderer.Invoke();
        
        base.Render(parentPos);
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
