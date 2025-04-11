using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml;
using Sandbox2D.UserInterface;
using Sandbox2D.UserInterface.Elements;

namespace Sandbox2D.Registry_.Registries;

/// <summary>
/// A delegate that returns a <see cref="IGuiElement"/>, provided an <see cref="XmlAttributeCollection"/>.
/// </summary>
/// <param name="args">the <see cref="GuiElementArgs"/> used to create the GUI element tag</param>
public delegate IGuiElement GuiElementConstructor(GuiElementArgs args);

/// <summary>
/// Represents the properties of an <see cref="IGuiElement"/> as XML.
/// </summary>
/// <param name="Attributes">the attributes of the element's tag</param>
/// <param name="Children">the children in the element's tag</param>
public record GuiElementArgs(string Value, List<XmlAttribute> Attributes, ConsumableList<XmlNode> Children);

public class GuiElementRegistry : IRegistryFactory<IGuiElement, GuiElementConstructor, GuiElementArgs>
{
    private readonly Dictionary<string, GuiElementConstructor> _values = new();

    public GuiElementRegistry()
    {
        Register("body", a => new BodyElement(a));
        Register("#text", a => new TextElement(a));
        Register("text", a => new TextElement(a));
    }
    
    public void Register(string id, GuiElementConstructor constructor)
    {
        _values.Add(id, constructor);
    }
    
    
    public bool TryCreate(string id, GuiElementArgs args, [MaybeNullWhen(false)] out IGuiElement element)
    {
        if (_values.TryGetValue(id, out var constructor))
        {
            element = constructor.Invoke(args);
            return true;
        }
        
        element = null;
        return false;
    }
    
    public bool TryCreate(string id, XmlNode xml, [MaybeNullWhen(false)] out IGuiElement element)
    {
        var attributes = xml.Attributes?.Cast<XmlAttribute>().ToList() ?? [];
        var children = new ConsumableList<XmlNode>(xml.ChildNodes.Cast<XmlNode>().ToList());
        
        return TryCreate(id, new GuiElementArgs(xml.Value, attributes, children), out element);
    }

    public bool TryCreate(XmlNode xml, [MaybeNullWhen(false)] out IGuiElement element) =>
        TryCreate(xml.Name, xml, out element);
    
    public bool TryCreate(string id, [MaybeNullWhen(false)] out IGuiElement element) =>
        TryCreate(id, new GuiElementArgs("", [], []), out element);
    
    
    public IGuiElement Create(string id, GuiElementArgs args)
    {
        if (TryCreate(id, args, out var element))
            return element;
        
        throw new ArgumentException($"GUI Element \"{id}\" is not registered");
    }
    public IGuiElement Create(string id, XmlNode xml)
    {
        var attributes = xml.Attributes?.Cast<XmlAttribute>().ToList() ?? [];
        var children = new ConsumableList<XmlNode>(xml.ChildNodes.Cast<XmlNode>().ToList());
        
        return Create(id, new GuiElementArgs(xml.Value, attributes, children));
    }

    public IGuiElement Create(XmlNode xml) =>
        Create(xml.Name, xml);
    
    public IGuiElement Create(string id) => 
        Create(id, new GuiElementArgs("", [], []));

}
