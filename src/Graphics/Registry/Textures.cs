namespace Sandbox2D.Graphics.Registry;

public static class Textures
{
    
    public static Texture Font { get; private set; }
    public static Texture DynTilemap { get; private set; }

    public static void Instantiate()
    {
        
        Font = Texture.LoadFromFile("font.png");
        DynTilemap = Texture.LoadFromFile("dynamic_tilemap.png");
        
        Util.Log("Created Textures");
    }
}