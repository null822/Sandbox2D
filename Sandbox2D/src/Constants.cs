using Math2D;
using Math2D.Quadtree;
using OpenTK.Windowing.Common;

namespace Sandbox2D;

/// <summary>
/// A set of constant values for the configuration of <see cref="Sandbox2D"/>
/// </summary>
public static class Constants
{
    /// <summary>
    /// The height of the world created on startup
    /// </summary>
    public const int InitialWorldHeight = 64;
    
    /// <summary>
    /// The TPS (Ticks Per Second) the game logic will attempt to run at
    /// </summary>
    public const double Tps = 20;
    
    /// <summary>
    /// The VSync mode to use
    /// </summary>
    public const VSyncMode Vsync = VSyncMode.On;
    
    /// <summary>
    /// The additional amound of times to poll for new input frames per second, independently of rendering framerate.
    /// Note that input is already polled once every frame. If this value is lower than the current framerate, no
    /// additional input frames will be polled.
    /// </summary>
    public const int AdditionalInputFrequency = 5000;
    
    /// <summary>
    /// The time in milliseconds to wait before updating the framerate and inputrate counters.
    /// </summary>
    public const int FrameStatisticsBatchTime = 50;
    
    /// <summary>
    /// Enables OpenGL debug logging.
    /// </summary>
    public const bool GlDebug = true;
    
    /// <summary>
    /// Disables asynchronous OpenGL debug output. Synchronous OpenGL debug is slower than asynchronous, but provides a
    /// stacktrace for all errors.
    /// </summary>
    public const bool SynchronousGlDebug = true;
    
    /// <summary>
    /// The global scale of GUIs
    /// </summary>
    public const float GuiScale = 1;
    
    /// <summary>
    /// The timeout, in milliseconds, for how long to wait for a lock for transferring data from the logic thread to
    /// the render thread
    /// </summary>
    public const int RenderLockTimeout = 100;
    
    /// <summary>
    /// The maximum amount of bytes (per buffer) upload to the GPU in one frame
    /// </summary>
    public const int GpuUploadBatchSize = 65536;
    
    /// <summary>
    /// Represents the accuracy of drawing when zoomed out to scales below 1. Higher number means less accurate, but
    /// faster drawing
    /// </summary>
    public const long DrawAccuracy = 4;
    
    /// <summary>
    /// The width/height of the SVG of an exported <see cref="Quadtree{T}"/> (see <see cref="Quadtree{T}.GetSvgMap"/>)
    /// </summary>
    public const ulong QuadTreeSvgSize = 65536uL;
    
    /// <summary>
    /// The default size of the game window
    /// </summary>
    public static readonly Vec2<int> InitialScreenSize = new(800, 600);
}
