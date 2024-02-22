using System;
using OpenTK.Graphics.OpenGL4;
using Sandbox2D.Graphics.Renderables;
using Sandbox2D.Graphics.Renderables.GuiRenderables;

namespace Sandbox2D.Graphics.Registry;

public static class Renderables
{

    private static BaseRenderable _vertexDebug;
    private static BaseRenderable _noise;
    private static PathTracedRenderable _pt;
    private static FontRenderable _font;
    private static GuiRenderable _guiBase;
    private static GuiCheckboxRenderable _guiCheckbox;
    private static TileRenderable _air;
    private static TileRenderable _dirt;
    private static TileRenderable _stone;
    
    
    public static ref BaseRenderable VertexDebug => ref _vertexDebug;
    public static ref BaseRenderable Noise => ref _noise;
    public static ref PathTracedRenderable Pt => ref _pt;
    public static ref FontRenderable Font => ref _font;
    public static ref GuiRenderable GuiBase => ref _guiBase;
    public static ref GuiCheckboxRenderable GuiCheckbox => ref _guiCheckbox;
    public static ref TileRenderable Air => ref _air;
    public static ref TileRenderable Dirt => ref _dirt;
    public static ref TileRenderable Stone => ref _stone;
    
    
    public static void Instantiate()
    {
        
        _vertexDebug = new BaseRenderable(Shaders.VertexDebug, BufferUsageHint.StreamDraw);
        _noise = new BaseRenderable(Shaders.Noise, BufferUsageHint.StreamDraw);
        
        _pt = new PathTracedRenderable(Shaders.Pt, BufferUsageHint.StreamDraw);
        
        _font = new FontRenderable(Shaders.Font, BufferUsageHint.DynamicDraw);
        
        _guiBase = new GuiRenderable(Shaders.GuiBase, BufferUsageHint.DynamicDraw);
        _guiCheckbox = new GuiCheckboxRenderable(Shaders.GuiCheckbox, BufferUsageHint.DynamicDraw);

        _air = new TileRenderable(Shaders.Noise);
        _dirt = new TileRenderable(Shaders.Dirt, BufferUsageHint.StreamDraw);
        _stone = new TileRenderable(Shaders.Stone, BufferUsageHint.StreamDraw);
        
        Util.Log("Created Renderables");
    }
    
    //TODO: make this less awful
    private static void Run(Func<Renderable, bool> lambda)
    {
        lambda.Invoke(_vertexDebug);
        lambda.Invoke(_noise);

        lambda.Invoke(_pt);
        
        lambda.Invoke(_font);

        lambda.Invoke(_guiBase);
        lambda.Invoke(_guiCheckbox);

        lambda.Invoke(_air);
        lambda.Invoke(_dirt);
        lambda.Invoke(_stone);
    }
    
    /// <summary>
    /// Calls the render function of every renderable in the category supplied.
    /// </summary>
    public static void Render(RenderableCategory category = RenderableCategory.All)
    {
        Run(renderable =>
        {
            renderable.Render(category);
            return true;
        });
    }
    
    /// <summary>
    /// Resets the geometry of every renderable in the category supplied.
    /// </summary>
    public static void ResetGeometry(RenderableCategory category = RenderableCategory.All)
    {
        Run(renderable =>
        {
            renderable.ResetGeometry(category);
            return true;
        });
    }
    
    /// <summary>
    /// Resets the geometry of every renderable in the category supplied.
    /// </summary>
    public static void UpdateVao(RenderableCategory category = RenderableCategory.All)
    {
        Run(renderable =>
        {
            renderable.UpdateVao(category:category);
            return true;
        });
    }
    
}

public enum RenderableCategory
{
    All,
    Base,
    Tile,
    Pt,
    Font,
    Gui,
    Checkbox,
}