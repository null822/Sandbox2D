using Sandbox2D;
using Sandbox2D.Registry_;
using Sandbox2D.UserInterface.Elements;
// using UIExtensions2D.UiElements;

namespace Sandbox2DTest.RegistryPopulators;

public class CommonRegistryPopulator : IRegistryPopulator
{
    /// <summary>
    /// Registers everything, unless everything is already registered.
    /// </summary>
    public void Register()
    {
        RegisterGui();
    }
    
    private static void RegisterGui()
    {
        RegisterGuiElements();
        RegisterGuiEvents();
    }
    
    private static void RegisterGuiElements()
    {
        // Registry.GuiElement.Register("test", args => new TestElement(args));
    }
    
    private static void RegisterGuiEvents()
    {
        Registry.GuiEvent.Register("run", () => Console.WriteLine("Hello from test event!"));
    }
}
