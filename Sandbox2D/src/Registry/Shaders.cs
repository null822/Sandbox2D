using Sandbox2D.Graphics;

namespace Sandbox2D.Registry;

public static class Shaders
{
    public static Shader VertexDebug { get; private set; }
    public static Shader Noise { get; private set; }
    
    public static Shader Qtr { get; private set; }
    
    public static Shader Font { get; private set; }
    public static Shader Text { get; private set; }
    
    
    
    public static void Instantiate()
    {
        VertexDebug = new Shader("vertex_debug.vsh", "vertex_debug.fsh");
        Noise = new Shader("noise.vsh", "noise.fsh");
        
        Qtr = new Shader("quadtree.vsh", "quadtree.fsh");
        
        Font = new Shader("gui/font/font.vsh", "gui/font/font.fsh");
        Text = new Shader("gui/font/text.vsh", "gui/font/text.fsh");
        
        
        Util.Log("Loaded Shaders", "Load/Render");
    }
}