namespace Sandbox2D.Graphics.Registry;

public static class Shaders
{
    public static Shader Test { get; private set; }

    public static void Instantiate()
    {
        Test = new Shader("vertColor.vsh", "vertColor.fsh");
        Util.Log("Shaders Loaded");
    }
}