using Sandbox2D.Graphics;
using Sandbox2D.Graphics.Registry;
using Sandbox2D.Graphics.Renderables;
using Sandbox2D.GUI;
using Sandbox2D.Maths;

namespace Sandbox2D.Gui.GuiElements;

public class TestGui : IGui
{
    public Range2D Area => new(200, 200, -200, -200);
    
    public bool Enabled { get; set; }

    public ref GuiRenderable Renderable => ref Renderables.GuiBase;
    public IGuiElement[] GuiElements { get; }

    public TestGui(IGuiElement[] guis)
    {
        GuiElements = guis;
    }

}