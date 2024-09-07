using System;

namespace Sandbox2D;

public static class GlobalVariables
{
    /// <summary>
    /// The class responsible for interfacing with OpenGL to render everything, handle controls and handle all other 
    /// actions that need to run every frame
    /// </summary>
    public static readonly RenderManager RenderManager = new (
        Constants.InitialScreenSize.X,
        Constants.InitialScreenSize.Y,
        "Sandbox2D");
    
    /// <summary>
    /// The absolute path to the asset directory of <see cref="Sandbox2D"/>
    /// </summary>
    public static readonly string AssetDirectory = (AppDomain.CurrentDomain.BaseDirectory + "assets").Replace('\\', '/');
    
    
}
