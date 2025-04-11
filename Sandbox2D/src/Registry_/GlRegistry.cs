using Sandbox2D.Managers;
using Sandbox2D.Registry_.Registries;

namespace Sandbox2D.Registry_;

public record GlRegistry(
    ShaderRegistry Shader,
    ShaderProgramRegistry ShaderProgram,
    TextureRegistry Texture,
    GuiManager Gui
)
{
    public GlRegistry() : this(
        new ShaderRegistry(), 
        new ShaderProgramRegistry(), 
        new TextureRegistry(), 
        new GuiManager()
        ) { }
}
