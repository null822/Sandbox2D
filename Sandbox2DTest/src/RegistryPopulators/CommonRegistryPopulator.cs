﻿using Sandbox2D;
using Sandbox2D.Registry_;
using Sandbox2D.UserInterface.Elements;

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
        RegisterGuis();
    }
    
    private static void RegisterGuiElements()
    {
        Registry.GuiElement.Register("test", args => new TestElement(args));
    }
    
    private static void RegisterGuiEvents()
    {
        Registry.GuiEvent.Register("run", () => Console.WriteLine("Hello from test event!"));
    }
    
    private static void RegisterGuis()
    {
        Registry.Gui.RegisterAll($"{GlobalVariables.AssetDirectory}/gui/");
    }
}