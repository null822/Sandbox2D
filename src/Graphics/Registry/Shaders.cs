namespace Sandbox2D.Graphics.Registry;

public static class Shaders
{
    public static Shader VertexDebug { get; private set; }
    public static Shader Noise { get; private set; }
    public static Shader GameObject { get; private set; }

    public static void Instantiate()
    {
        VertexDebug = new Shader("vertex_debug.vsh", "vertex_debug.fsh");
        Noise = new Shader("noise.vsh", "noise.fsh");
        GameObject = new Shader("game_object.vsh", "game_object.fsh");
        
        
        Util.Log("Shaders Loaded");
    }
}