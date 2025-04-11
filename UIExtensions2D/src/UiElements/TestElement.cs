using Math2D;
using Sandbox2D.Graphics.ShaderControllers;
using Sandbox2D.Registry_;
using Sandbox2D.Registry_.Registries;
using Sandbox2D.UserInterface;

namespace UIExtensions2D.UiElements;

public class TestElement : IGuiElement
{
    public override string Id => "test";

    private readonly TextureRenderer _shader;

    public TestElement(GuiElementArgs args) : base(args)
    {
        _shader = new TextureRenderer(GlContext.Registry.ShaderProgram.Create("debug/texture"), GlContext.Registry.Texture.Get("missing"));
    }
    
    public override void Render(Vec2<int> parentPos)
    {
        _shader.Invoke();
        base.Render(parentPos);
    }
    
    public override void Update()
    {
        if (Attributes.TryGetValue("onClick", out var eventName))
        {
            Registry.GuiEvent.Invoke(eventName);
        }
    }
}
