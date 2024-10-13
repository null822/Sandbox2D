using Sandbox2D.Registries;

namespace Sandbox2D;

public static class Registry
{
    public static readonly GuiRegistry Gui = new();
    public static readonly GuiElementRegistry GuiElement = new();
    public static readonly GuiEventRegistry GuiEvent = new();
    public static readonly ShaderRegistry Shader = new();
    public static readonly ShaderProgramRegistry ShaderProgram = new();
    public static readonly TextureRegistry Texture = new();
}
