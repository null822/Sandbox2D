namespace Sandbox2D.Graphics.Registry;

public static class Shaders
{
    public static Shader VertexDebug { get; private set; }
    public static Shader Noise { get; private set; }
    public static Shader GoNoise { get; private set; }
    public static Shader GoVertexDebug { get; private set; }

    public static void Instantiate()
    {
        VertexDebug = new Shader("vertex_debug.vsh", "vertex_debug.fsh");
        Noise = new Shader("noise.vsh", "noise.fsh");
        GoNoise = new Shader("go_noise.vsh", "go_noise.fsh");
        GoVertexDebug = new Shader("go_vertex_debug.vsh", "go_vertex_debug.fsh");
        
        
        Util.Log("Created Shaders");
    }
}