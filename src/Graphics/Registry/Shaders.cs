namespace Sandbox2D.Graphics.Registry;

public static class Shaders
{
    public static Shader VertexDebug { get; private set; }
    public static Shader Noise { get; private set; }
    
    public static Shader Dirt { get; private set; }
    public static Shader Stone { get; private set; }

    public static Shader Font { get; private set; }

    public static Shader GuiBase { get; private set; }
    
        
    public static Shader GuiCheckbox { get; private set; }

    public static void Instantiate()
    {
        VertexDebug = new Shader("vertex_debug.vsh", "vertex_debug.fsh");
        Noise = new Shader("noise.vsh", "noise.fsh");
        
        Dirt = new Shader("tile/dirt.vsh", "tile/dirt.fsh");
        Stone = new Shader("tile/stone.vsh", "tile/stone.fsh");
        
        Font = new Shader("gui/font/font.vsh", "gui/font/font.fsh");

        GuiBase = new Shader("gui/base.vsh", "gui/base.fsh");
        
        GuiCheckbox = new Shader("gui/checkbox.vsh", "gui/checkbox.fsh");
        
        Util.Log("Created Shaders");
    }
}