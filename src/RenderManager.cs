using System.Threading;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Sandbox2D.Graphics.Registry;
using Sandbox2D.Graphics.Renderables;
using Sandbox2D.GUI;
using Sandbox2D.Maths;
using Sandbox2D.World;
using Sandbox2D.World.TileTypes;
using static Sandbox2D.Util;
using static Sandbox2D.Constants;

namespace Sandbox2D;

public class RenderManager(int width, int height, string title) : GameWindow(GameWindowSettings.Default,
    new NativeWindowSettings { ClientSize = (width, height), Title = title })
{
    private static bool _worldUpdatedSinceLastFrame = true;

    private static QuadTreeStruct[] _linearQuadTree = [];
    private static float _scale;
    private static Vec2<float> _translation;
    
    
    public static void UpdateLqt(QuadTreeStruct[] lqt, float scale, Vec2<float> translation)
    {
        _linearQuadTree = lqt;

        _scale = scale;
        _translation = translation;

        _worldUpdatedSinceLastFrame = true;
    }
    
    public static void UpdateTransform(float scale, Vec2<float> translation)
    {
        _scale = scale;
        _translation = translation;

        _worldUpdatedSinceLastFrame = true;
    }
    
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        // only render when the game is running
        if (!GameManager.IsRunning)
            return;
        
        
        // clear the color buffer
        GL.Clear(ClearBufferMask.ColorBufferBit);

        // render world (with off-screen culling)
        
        ref var pt = ref Renderables.Pt;
        
        // reupload the world to the gpu if needed
        if (_worldUpdatedSinceLastFrame && GameManager.IsRunning)
        {
            // reset the world geometry, update the transform, and set the new geometry
            Renderables.ResetGeometry(RenderableCategory.Pt);
            pt.SetTransform(_translation, _scale);
            pt.SetGeometry(_linearQuadTree);
            
            // reset worldUpdated flag
            _worldUpdatedSinceLastFrame = false;
        }
        
        pt.Render();
        
        // render all of the tile renderables
        // Renderables.Render(RenderableCategory.Pt);
        
        
        // TODO: render brush outline
        
        
        // render the GUIs
        GuiManager.UpdateGuis();
        Renderables.Render(RenderableCategory.Gui);
        
        
        ref var renderable = ref Renderables.Font;
        var center = GameManager.ScreenSize / 2;
        
        // FPS display
        renderable.SetText((1 / args.Time).ToString("0.0 FPS"), -center + (0,10), 1f, false);
        
        renderable.UpdateVao();
        renderable.Render();
        renderable.ResetGeometry();
        
        // Mouse Coordinate Display
        renderable.SetText(ScreenToWorldCoords((Vec2<int>)MousePosition).ToString(), -center + (0,30), 1f, false);
        
        renderable.UpdateVao();
        renderable.Render();
        renderable.ResetGeometry();
        
        // swap buffers
        SwapBuffers();
    }
    

    /// <summary>
    /// Initializes everything
    /// </summary>
    protected override void OnLoad()
    {
        Log("===============[   LOADING   ]===============");

        base.OnLoad();
        
        // set clear color
        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        
        // create all of the shaders
        Shaders.Instantiate();
        
        // create the textures
        Textures.Instantiate();
        
        // create the renderables. must be done after creating the shaders
        Renderables.Instantiate();
        
        // create the GUIs. must be done after creating the renderables
        GuiManager.Instantiate();
        
        // create tiles. must be done after creating the renderables
        Tiles.Instantiate(new ITile[]
        {
            new Air(),
            new Stone(),
            new Dirt()
        });
        
        // set starting inputs
        GameManager.SetInputs(MouseState, KeyboardState);
        
        // start the game logic
        GameManager.SetRunning(true);
    }
    
    /// <summary>
    /// The game logic loop
    /// </summary>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        var isActive = CheckActive();
        
        if (!isActive)
        {
            Thread.Sleep(CheckActiveDelay);
        }
        
        GameManager.SetRunning(isActive);
        
        
        // set the inputs
        GameManager.SetInputs(MouseState.GetSnapshot(), KeyboardState.GetSnapshot());
        
    }
    
    /// <summary>
    /// Returns true if the game should be running (game logic, rendering, etc.)
    /// </summary>
    private bool CheckActive()
    {
        // only run when focused
        if (!IsFocused)
            return false;
        
        // only run when hovered
        if (MousePosition.X < 0 || MousePosition.X > ClientSize.X || MousePosition.Y < 0 || MousePosition.Y > ClientSize.Y)
            return false;
        
        return true;
    }
    
    /// <summary>
    /// Update the viewport when the window is resized
    /// </summary>
    /// <param name="e"></param>
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);
        
        // update screenSize 
        GameManager.SetScreenSize(new Vec2<int>(e.Width, e.Height));

        // re-render the world
        _worldUpdatedSinceLastFrame = true;
        
        // Update the GUIs, since the vertex coords are calculated on creation,
        // and need to be updated with the new screen size
        GuiManager.UpdateGuis();
    }
    
    
}
