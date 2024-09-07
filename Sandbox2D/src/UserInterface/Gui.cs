using System;
using System.Collections.Generic;
using System.Xml;
using Sandbox2D.Registry;

namespace Sandbox2D.UserInterface;

public class Gui
{
    private readonly GuiElement[] _elements;
    private readonly Dictionary<string, int> _elementIds = new();
    
    public Gui(XmlDocument guiFile)
    {
        var bodies = guiFile.GetElementsByTagName("body");
        if (bodies.Count == 0)
            throw new Exception("GUI XML Does not contain a body");
        var body = bodies[0]!;
        
        var elements = new List<GuiElement>();
        for (var i = 0; i < body.ChildNodes.Count; i++)
        {
            var node = body.ChildNodes[i]!;
            var name = node.Name;
            if (name == "#comment") continue;
            
            var attributes = node.Attributes!;
            
            if (attributes["id"] != null)
            {
                _elementIds.Add(attributes["id"].Value, elements.Count);
            }
            
            elements.Add(GuiElements.Create(name, attributes));
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

    public void Update()
    {
        foreach (var element in _elements)
        {
            element.Update();
        }
    }
    
    public void SetAttribute(string elementId, string attribute, string name)
    {
        if (_elementIds.TryGetValue(elementId, out var elementLocation))
        {
            _elements[elementLocation].SetAttribute(attribute, name);
        }
        else
        {
            throw new Exception($"Element of id {elementId} does not exist");
        }
    }
    
    public string GetAttribute(string elementId, string attribute)
    {
        if (_elementIds.TryGetValue(elementId, out var elementLocation))
        {
            return _elements[elementLocation].GetAttribute(attribute);
        }
        
        throw new Exception($"Element of id {elementId} does not exist");
    }
    
}

public class MissingGuiComponentException(GuiComponent component, string name)
    : Exception($"{component.ToString()} \'{name}\' was not found");

public enum GuiComponent
{
    Gui,
    GuiElement,
    GuiEvent
}