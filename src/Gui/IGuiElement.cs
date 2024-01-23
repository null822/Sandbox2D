using Sandbox2D.Maths;

namespace Sandbox2D.GUI;

public interface IGuiElement
{
    protected bool Enabled { get; set; }
    
    public void MouseOver(Vec2<int> mousePos);

    public void AddToRenderable(Vec2<int> guiPosition);
    
    public sealed void SetVisibility(bool visibility)
    {
        Enabled = visibility;
    }
    
    public sealed bool GetVisibility()
    {
        return Enabled;
    }
    
    
}