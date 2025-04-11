using Sandbox2D;
using Sandbox2D.Registry_;

namespace Sandbox2DTest.RegistryPopulators;

public class ClientRegistryPopulator : IRegistryPopulator
{
    /// <summary>
    /// Registers everything, unless everything is already registered.
    /// </summary>
    public void Register()
    {
        RegisterShaders();
        RegisterShaderPrograms();
        RegisterTextures();
        RegisterGuis();
    }
    
    private static void RegisterShaders()
    {
        GlContext.Registry.Shader.RegisterAll($"{GlobalVariables.AssetDirectory}/shaders/");
    }
    
    private static void RegisterShaderPrograms()
    {
        GlContext.Registry.ShaderProgram.Register("quadtree", "quadtree_vert", "quadtree_frag");
        GlContext.Registry.ShaderProgram.Register("data_patch", "data_patch_comp");
        GlContext.Registry.ShaderProgram.Register("text", "gui/font/text_vert", "gui/font/text_frag");
        
        // debug programs
        GlContext.Registry.ShaderProgram.Register("debug/noise", "debug/noise_vert", "debug/noise_frag");
        GlContext.Registry.ShaderProgram.Register("debug/texture", "debug/texture_vert", "debug/texture_frag");
        GlContext.Registry.ShaderProgram.Register("debug/vertex", "debug/vertex_vert", "debug/vertex_frag");
    }
    
    private static void RegisterTextures()
    {
        GlContext.Registry.Texture.RegisterAll($"{GlobalVariables.AssetDirectory}/textures/");
    }
    
    private static void RegisterGuis()
    {
        GlContext.Registry.Gui.RegisterAll($"{GlobalVariables.AssetDirectory}/gui/");
    }
}
