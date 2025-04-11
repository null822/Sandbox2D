using Sandbox2D.Registry_.Registries;

namespace Sandbox2D.UserInterface.Elements;

public class BodyElement : IGuiElement
{
    public override string Id => "body";
    
    public BodyElement(GuiElementArgs args) : base(args) { }
}
