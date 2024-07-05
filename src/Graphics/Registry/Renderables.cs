using System;
using OpenTK.Graphics.OpenGL4;
using Sandbox2D.Graphics.Renderables;
using Sandbox2D.Graphics.Renderables.GuiRenderables;

namespace Sandbox2D.Graphics.Registry;

public static class Renderables
{

    private static GuiRenderable _guiBase;
    private static GuiCheckboxRenderable _guiCheckbox;
    
    public static ref GuiRenderable GuiBase => ref _guiBase;
    public static ref GuiCheckboxRenderable GuiCheckbox => ref _guiCheckbox;
    
    
    public static void Instantiate()
    {
        _guiBase = new GuiRenderable(Shaders.GuiBase, BufferUsageHint.DynamicDraw);
        _guiCheckbox = new GuiCheckboxRenderable(Shaders.GuiCheckbox, BufferUsageHint.DynamicDraw);

        Util.Log("Created Renderables", OutputSource.Load);
    }
    
    //TODO: make this less awful
    private static void Run(Func<Renderable, bool> lambda)
    {
        lambda.Invoke(_guiBase);
        lambda.Invoke(_guiCheckbox);
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
    Gui,
    Checkbox,
}