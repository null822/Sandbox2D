namespace Sandbox2D.Graphics.Registry;

public static class Textures
{
    
    public static Texture Font { get; private set; }

    public static void Instantiate()
    {
        
        Font = Texture.LoadFromFile("font.png");
        
        Util.Log("Created Textures");
    }
}