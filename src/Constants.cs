using OpenTK.Windowing.Common;

namespace Sandbox2D;

public static class Constants
{
    
    private const bool LargeWorld = true;
    
    public const VSyncMode Vsync = VSyncMode.On;


    public const float GuiScale = 1;
    
    public const int CheckActiveDelay = 100;
    

    public const double BlockMatrixSvgScale = 1;
    
    public const byte WorldDepth  = LargeWorld ? 62 : 16;


    public const bool Log = true;
    public const bool Debug = true;
    public const bool Warn = true;
    public const bool Error = true;
    

}
