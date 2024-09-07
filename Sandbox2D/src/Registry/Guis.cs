using System.Collections.Generic;
using Sandbox2D.UserInterface;

namespace Sandbox2D.Registry;

public static class Guis
{
    private static readonly List<Gui> GuiList = [];
    private static readonly Dictionary<string, int> GuiNames = new();
    private static readonly List<int> VisibleGuis = [];
    
    public static void Register(string identifier, Gui gui)
    {
        GuiNames.Add(identifier, GuiList.Count);
        GuiList.Add(gui);
    }
    
    /// <summary>
    /// Renders a <see cref="Gui"/>
    /// </summary>
    /// <param name="name">the name of the <see cref="Gui"/> to render</param>
    public static void Render(string name)
    {
        Get(name).Render();
    }
    
    /// <summary>
    /// Renders all visible <see cref="Gui"/>s
    /// </summary>
    public static void RenderVisible()
    {
        foreach (var guiIndex in VisibleGuis)
        {
            GuiList[guiIndex].Render();
        }
    }
    
    /// <summary>
    /// Updates all visible <see cref="Gui"/>s.
    /// </summary>
    public static void UpdateVisible()
    {
        foreach (var guiIndex in VisibleGuis)
        {
            GuiList[guiIndex].Update();
        }
    }
    
    /// <summary>
    /// Sets whether a <see cref="Gui"/> is visible or not.
    /// </summary>
    /// <param name="name">the name of the <see cref="Gui"/></param>
    /// <param name="visible">a <see cref="bool"/> stating whether the <see cref="Gui"/> is visible or not</param>
    public static void SetVisibility(string name, bool visible)
    {
        if (GuiNames.TryGetValue(name, out var index))
        {
            var currentIndex = VisibleGuis.IndexOf(index);
            var currentIsVisible = currentIndex != -1;
            
            // exit if the GUI is already in the correct state
            if (currentIsVisible == visible) return;
            
            if (visible)
            {
                VisibleGuis.Add(index);
            }
            else
            {
                VisibleGuis.RemoveAt(currentIndex);
            }
            
        }
    }
    
    public static void SetAttribute(string name, string elementId, string attributeName, string value)
    {
        Get(name).SetAttribute(elementId, attributeName, value);
    }
    
    public static string GetAttribute(string name, string elementId, string attributeName)
    {
        return Get(name).GetAttribute(elementId, attributeName);
    }
    
    public static Gui Get(string name)
    {
        if (GuiNames.TryGetValue(name, out var index))
        {
            return GuiList[index];
        }
        
        throw new MissingGuiComponentException(GuiComponent.Gui, name);
    }
}
