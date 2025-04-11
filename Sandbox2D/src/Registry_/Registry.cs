using Sandbox2D.Registry_.Registries;

namespace Sandbox2D.Registry_;

public static class Registry
{
    public static readonly GuiElementRegistry GuiElement = new();
    public static readonly GuiEventRegistry GuiEvent = new();
}
