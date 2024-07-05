using Sandbox2D.Graphics.Registry;
using Sandbox2D.Graphics.Renderables;
using Sandbox2D.Maths;

namespace Sandbox2D.GUI;

public interface IGui
{
    protected Range2D Area { get; }
    
    protected bool Enabled { get; set; }
    
    protected ref GuiRenderable Renderable => ref Renderables.GuiBase;

    protected IGuiElement[] GuiElements { get; }

    public void CreateGeometry(Vec2<int>? offset = null)
    {
        // don't render disabled GUIs
        if (!Enabled)
            return;
        
        var offsetNonNull = offset ?? new Vec2<int>(0, 0);
        
        Renderable.AddQuad(
            (Vec2<int>)Area.Tl + offsetNonNull,
            (Vec2<int>)Area.Br + offsetNonNull
        );
        
        foreach (var gui in GuiElements)
        {
            gui.AddToRenderable(Area.Center);
        }
    }
    
    
    public void MouseOver(Vec2<int> mousePos)
    {
        if (!Enabled)
            return;

        foreach (var guiElement in GuiElements)
        {
            guiElement.MouseOver(mousePos);
        }
    }

    
    public sealed void SetVisibility(bool visibility)
    {
        Enabled = visibility;

        foreach (var guiElement in GuiElements)
        {
            guiElement.SetVisibility(visibility);
        }
    }
    
    public sealed bool GetVisibility()
    {
        return Enabled;
    }

}