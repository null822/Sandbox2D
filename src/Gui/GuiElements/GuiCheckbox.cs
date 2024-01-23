using System;
using Sandbox2D.Graphics.Registry;
using Sandbox2D.Graphics.Renderables.GuiRenderables;
using Sandbox2D.GUI;
using Sandbox2D.Maths;

namespace Sandbox2D.Gui.GuiElements;

public class GuiCheckbox : IGuiElement
{
    private static readonly Range2D Area = new(50, 50, -50, -50);
    
    public bool Enabled { get; set; }
    private uint _state;
    
    private static ref GuiCheckboxRenderable Renderable => ref Renderables.GuiCheckbox;
    
    public void MouseOver(Vec2<int> mousePos)
    {
        _state = Area.Contains(mousePos) ? 1u : 0u;
    }
    
    public void AddToRenderable(Vec2<int> guiPosition)
    {
        // don't render if not enabled
        if (!Enabled)
            return;
        
        Renderable.AddQuad(
            (Vec2<int>)Area.TopLeft + guiPosition,
            (Vec2<int>)Area.BottomRight + guiPosition,
            _state);
    }
}