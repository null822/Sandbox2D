using OpenTK.Windowing.Common;

namespace Sandbox2D;

public static class Constants
{
    
    public const VSyncMode Vsync = VSyncMode.On;
    private const bool LargeWorld = false;
    
    /// <summary>
    /// false = "(-5..5, -5..5)"<br></br>
    /// true = "(-5, -5)..(5, 5)"
    /// </summary>
    public const bool Range2DStringFormat = true;

    public const double BlockMatrixSvgScale = 1;
    
    public const long WorldWidth  = LargeWorld ? 4611686018427387904 : 65536;
    public const long WorldHeight = LargeWorld ? 4611686018427387904 : 65536;

}
