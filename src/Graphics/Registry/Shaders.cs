namespace Sandbox2D.Graphics.Registry;

public static class Shaders
{
    public static Shader VertexDebug { get; private set; }
    public static Shader Noise { get; private set; }
    public static Shader Dirt { get; private set; }
    public static Shader Stone { get; private set; }

    public static void Instantiate()
    {
        VertexDebug = new Shader("vertex_debug.vsh", "vertex_debug.fsh");
        Noise = new Shader("noise.vsh", "noise.fsh");
        Dirt = new Shader("tiles/dirt.vsh", "tiles/dirt.fsh");
        Stone = new Shader("tiles/stone.vsh", "tiles/stone.fsh");
        
        Util.Log("Created Shaders");
    }
}