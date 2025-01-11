using Sandbox2D.Graphics.ShaderControllers;
using Sandbox2D.Registry_;
using Sandbox2D.Registry_.Registries;

namespace Sandbox2D.UserInterface.Elements;

public class TestElement : IGuiElement
{
    private TextureRenderer _shader;

    public TestElement(GuiElementArguments args) : base(args)
    {
        
    }
    
    public override void GlInitialize()
    {
        _shader = new TextureRenderer(GlRegistry.ShaderProgram.Create("debug/texture"), GlRegistry.Texture.Get("missing"));
    }
    
    public override void Render()
    {
        _shader.Invoke();
    }
    
    public override void Update()
    {
        // return;
        if (Attributes.TryGetValue("onClick", out var eventName))
        {
            Registry.GuiEvent.Invoke(eventName);
        }
    }
}
