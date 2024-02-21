using OpenTK.Windowing.Common;

namespace Sandbox2D;

public static class Constants
{
    /// <summary>
    /// Private toggle for if the world should be max-sized.
    /// </summary>
    private const bool LargeWorld = true;
    
    /// <summary>
    /// The VSync mode to use.
    /// </summary>
    public const VSyncMode Vsync = VSyncMode.On;

    /// <summary>
    /// The global scale of GUIs.
    /// </summary>
    public const float GuiScale = 1;
    
    /// <summary>
    /// The delay, in milliseconds, between active-checks for when the window is unfocused.
    /// </summary>
    public const int CheckActiveDelay = 100;
    
    /// <summary>
    /// The scale at which to export a QuadTree as an svg as.
    /// </summary>
    public const double QuadTreeSvgScale = 1;
    
    /// <summary>
    /// The depth of the world QuadTree.
    /// </summary>
    /// <remarks>
    /// The maximum value is 63
    /// </remarks>
    public const byte WorldDepth  = LargeWorld ? 63 : 3;
    
    /// <summary>
    /// The depth of Linear QuadTree that gets uploaded to the GPU.
    /// </summary>
    /// <remarks>
    /// The maximum value is 16
    /// </remarks>
    public const byte RenderDepth = 16;


    // whether to print outputs
    public const bool Log = true;
    public const bool Debug = true;
    public const bool Warn = true;
    public const bool Error = true;
    

}
