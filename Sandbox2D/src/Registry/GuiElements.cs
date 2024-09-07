using System.Collections.Generic;
using System.Xml;
using Sandbox2D.UserInterface;

namespace Sandbox2D.Registry;

/// <summary>
/// A delegate that returns a <see cref="GuiElement"/>, provided an <see cref="XmlAttributeCollection"/>.
/// </summary>
public delegate GuiElement GuiElementConstructor(XmlAttributeCollection attributes);

public static class GuiElements
{
    private static readonly Dictionary<string, GuiElementConstructor> Values = new();
    
    /// <summary>
    /// Registers a new <see cref="GuiElement"/>.
    /// </summary>
    /// <param name="tagName">the tag name of the GUI element</param>
    /// <param name="constructor">a <see cref="GuiElementConstructor"/> used to construct the <see cref="GuiElement"/>.</param>
    public static void Register(string tagName, GuiElementConstructor constructor)
    {
        Values.Add(tagName, constructor);
    }
    
    /// <summary>
    /// Creates (by running its <see cref="GuiElementConstructor"/>) and returns a new <see cref="GuiElement"/>.
    /// </summary>
    /// <param name="tagName">the name of the GUI element to create</param>
    /// <param name="attributes">the attributes of the GUI element</param>
    /// <exception cref="MissingTileException">thrown when the <paramref name="tagName"/> is not registered</exception>
    public static GuiElement Create(string tagName, XmlAttributeCollection attributes)
    {
        if (Values.TryGetValue(tagName, out var constructor))
        {
            return constructor.Invoke(attributes);
        }
        
        throw new MissingGuiComponentException(GuiComponent.Gui, tagName);
    }
}
