using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using Sandbox2D.UserInterface;
using Sandbox2D.UserInterface.Elements;

namespace Sandbox2D.Registries;

/// <summary>
/// A delegate that returns a <see cref="IGuiElement"/>, provided an <see cref="XmlAttributeCollection"/>.
/// </summary>
/// <param name="arguments">the <see cref="GuiElementArguments"/> used to create the GUI element tag</param>
public delegate IGuiElement GuiElementConstructor(GuiElementArguments arguments);

/// <summary>
/// Represents the properties of an <see cref="IGuiElement"/> as XML.
/// </summary>
/// <param name="Attributes">the attributes of the element's tag</param>
/// <param name="Children">the children in the element's tag</param>
public record GuiElementArguments(List<XmlAttribute> Attributes, ConsumableList<XmlNode> Children);

public class GuiElementRegistry : IRegistryFactory<IGuiElement, GuiElementConstructor, GuiElementArguments>
{
    private readonly Dictionary<string, GuiElementConstructor> _values = new();

    public GuiElementRegistry()
    {
        Register("#text", TextElement.Constructor);
        Register("text", TextElement.Constructor);
    }
    
    public void Register(string id, GuiElementConstructor @delegate)
    {
        _values.Add(id, @delegate);
    }
    
    public IGuiElement Create(string id, GuiElementArguments args)
    {
        if (_values.TryGetValue(id, out var constructor))
        {
            return constructor.Invoke(args);
        }
        
        throw new ArgumentException($"GUI \"{id}\" is not registered");
    }
    
    public bool TryCreate(string id, GuiElementArguments args, [MaybeNullWhen(false)] out IGuiElement element)
    {
        if (_values.TryGetValue(id, out var constructor))
        {
            element = constructor.Invoke(args);
            return true;
        }
        
        element = null;
        return false;
    }
}
