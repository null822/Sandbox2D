﻿using Sandbox2D.Graphics;

namespace Sandbox2D.Registry;

public static class Textures
{
    
    public static Texture Font { get; private set; }
    public static Texture DynTilemap { get; private set; }

    public static void Instantiate()
    {
        
        Font = Texture.LoadFromFile("font.png");
        DynTilemap = Texture.LoadFromFile("dynamic_tilemap.png");
        
        Util.Log("Loaded Textures", "Load/Render");
    }
}