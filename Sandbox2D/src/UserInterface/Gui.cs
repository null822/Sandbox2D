using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Sandbox2D.Registry_;
using Sandbox2D.Registry_.Registries;

namespace Sandbox2D.UserInterface;

public class Gui
{
    private readonly IGuiElement[] _elements;
    private readonly Dictionary<string, int> _elementIds = new();

    public Gui(XmlDocument guiFile)
    {
        var bodies = guiFile.GetElementsByTagName("body");
        if (bodies.Count == 0)
            throw new Exception("GUI XML Does not contain a body");
        var body = bodies[0]!;

        var elements = new List<IGuiElement>();
        var xmlElements = body.ChildNodes.Cast<XmlNode>().ToList();

        for (var i = 0; i < xmlElements.Count; i++)
        {
            var node = xmlElements[i];

            var name = node.Name;
            if (name == "#comment") continue;

            var children = new ConsumableList<XmlNode>(node.ChildNodes.Cast<XmlNode>().ToList());
            var attributes = node.Attributes?.Cast<XmlAttribute>().ToList() ?? [];
            
            if (!Registry.GuiElement.TryCreate(name, new GuiElementArguments(attributes, children),
                    out var element))
            {
                Util.Error($"GUI element \"{name}\" is not registered");
                continue;
            }

            var id = element.GetAttribute("id");
            if (id != "")
                _elementIds.Add(id, elements.Count);

            elements.Add(element);
            xmlElements.AddRange(children.List);
        }

        _elements = elements.ToArray();
        elements.Clear();
    }

    public Gui(string xmlPath) : this(LoadXml(xmlPath)) { }
    
    private static XmlDocument LoadXml(string path)
    {
        var guiXml = new XmlDocument();
        guiXml.Load(path);
        return guiXml;
    }

    /// <summary>
    /// Renders the <see cref="Gui"/> to the screen.
    /// </summary>
    public void Render()
    {
        foreach (var element in _elements)
        {
            element.Render();
        }
    }
    
    /// <summary>
    /// Update's the <see cref="Gui"/>'s logic
    /// </summary>
    public void Update()
    {
        foreach (var element in _elements)
        {
            element.Update();
        }
    }

    /// <summary>
    /// Sets an attribute.
    /// </summary>
    /// <param name="elementId">the id of the <see cref="IGuiElement"/> on which the attribute will be set</param>
    /// <param name="attribute">the name of the attribute</param>
    /// <param name="value">the new value</param>
    /// <exception cref="ArgumentException">thrown when the <see cref="IGuiElement"/> targeted by
    /// <paramref name="elementId"/> does not exist in this <see cref="Gui"/></exception>
    public void SetAttribute(string elementId, string attribute, string value)
    {
        if (_elementIds.TryGetValue(elementId, out var elementLocation))
        {
            _elements[elementLocation].SetAttribute(attribute, value);
        }
        else
        {
            throw new ArgumentException($"GUI Element of id \"{elementId}\" does not exist in this GUI");
        }
    }

    /// <summary>
    /// Gets an attribute's value.
    /// </summary>
    /// <param name="elementId">the id of the <see cref="IGuiElement"/> containing the attribute to get</param>
    /// <param name="attribute">the name of the attribute to get</param>
    /// <returns>the value of the attribute, or an empty string if it was not found</returns>
    /// <exception cref="ArgumentException">thrown when the <see cref="IGuiElement"/> targeted by
    /// <paramref name="elementId"/> does not exist in this <see cref="Gui"/></exception>
    public string GetAttribute(string elementId, string attribute)
    {
        if (_elementIds.TryGetValue(elementId, out var elementLocation))
        {
            return _elements[elementLocation].GetAttribute(attribute);
        }

        throw new ArgumentException($"GUI Element of id \"{elementId}\" does not exist in this GUI");
    }
}
