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
    }
    
    private static void RegisterShaders()
    {
        GlRegistry.Shader.RegisterAll($"{GlobalVariables.AssetDirectory}/shaders/");
    }
    
    private static void RegisterShaderPrograms()
    {
        GlRegistry.ShaderProgram.Register("quadtree", "quadtree_vert", "quadtree_frag");
        GlRegistry.ShaderProgram.Register("data_patch", "data_patch_comp");
        GlRegistry.ShaderProgram.Register("text", "gui/font/text_vert", "gui/font/text_frag");
        
        // debug programs
        GlRegistry.ShaderProgram.Register("debug/noise", "debug/noise_vert", "debug/noise_frag");
        GlRegistry.ShaderProgram.Register("debug/texture", "debug/texture_vert", "debug/texture_frag");
        GlRegistry.ShaderProgram.Register("debug/vertex", "debug/vertex_vert", "debug/vertex_frag");
    }
    
    private static void RegisterTextures()
    {
        GlRegistry.Texture.RegisterAll($"{GlobalVariables.AssetDirectory}/textures/");
    }
    
}
