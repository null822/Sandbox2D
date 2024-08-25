using OpenTK.Windowing.Common;
using Sandbox2D.Maths;

namespace Sandbox2D;

/// <summary>
/// A set of constant values for the configuration of <see cref="Sandbox2D"/>
/// </summary>
public static class Constants
{
    /// <summary>
    /// The TPS (Ticks Per Second) the game logic will attempt to run at.
    /// </summary>
    public const int Tps = 20;
    
    /// <summary>
    /// The VSync mode to use.
    /// </summary>
    public const VSyncMode Vsync = VSyncMode.On;
    
    /// <summary>
    /// Disables asynchronous OpenGL debug output. Synchronous OpenGL debug is slower than asynchronous, but provides a
    /// stacktrace for all errors.
    /// </summary>
    public const bool SynchronousGlDebug = true;
    
    /// <summary>
    /// The global scale of GUIs.
    /// </summary>
    public const float GuiScale = 1;
    
    /// <summary>
    /// The delay, in milliseconds, between active-checks for when the window is unfocused.
    /// </summary>
    public const int CheckActiveDelay = 100;

    /// <summary>
    /// The timeout, in milliseconds, for how long to wait for a lock for transferring data from the logic thread to
    /// the render thread
    /// </summary>
    public const int RenderLockTimeout = 100;
    
    /// <summary>
    /// The timeout, in milliseconds, for how long to wait for a lock for transferring data from the logic thread to
    /// the render thread
    /// </summary>
    public const int GpuUploadBatchSize = 2048;

    
    /// <summary>
    /// The width/height of the SVG of an exported <see cref="Maths.Quadtree.Quadtree{T}"/> (see <see cref="Maths.Quadtree.Quadtree{T}.GetSvgMap"/>).
    /// </summary>
    public const ulong QuadTreeSvgWidth = 65536uL;
    
    /// <summary>
    /// The default size of the game window
    /// </summary>
    public static readonly Vec2<int> InitialScreenSize = new(800, 600);

    public const int QuadtreeArrayLength = 2048;
}
