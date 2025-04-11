using System;
using System.Collections.Generic;
using Math2D;
using Sandbox2D.Registry_;
using Sandbox2D.Registry_.Registries;

namespace Sandbox2D.UserInterface;

public abstract class IGuiElement
{
    /// <summary>
    /// The ID (XML tag name) of this <see cref="IGuiElement"/>
    /// </summary>
    public abstract string Id { get; }
    
    /// <summary>
    /// The value of this <see cref="IGuiElement"/>
    /// </summary>
    protected string Value;
    
    protected Vec2<int> Position;
    
    /// <summary>
    /// The elements within this <see cref="IGuiElement"/>
    /// </summary>
    protected List<IGuiElement> Children { get; } = [];
    /// <summary>
    /// The attributes of this <see cref="IGuiElement"/>
    /// </summary>
    protected Dictionary<string, string> Attributes { get; } = new();
    
    protected IGuiElement(GuiElementArgs args)
    {
        Value = args.Value;
        
        args.Children.ConsumeAll(n => n.Name == "#comment");
        var children = args.Children;
        Children.EnsureCapacity(children.Count);
        foreach (var node in children)
        {
            if (Registry.GuiElement.TryCreate(node, out var child))
            {
                Children.Add(child);
            }
        }
        
        foreach (var attribute in args.Attributes)
        {
            Attributes.Add(attribute.Name, attribute.Value);
        }
    }
    
    
    /// <summary>
    /// Renders the <see cref="IGuiElement"/>.
    /// </summary>
    public virtual void Render(Vec2<int> parentPos)
    {
        parentPos += Position;
        
        foreach (var element in Children)
        {
            element.Render(parentPos);
        }
    }
    
    /// <summary>
    /// Updates the <see cref="IGuiElement"/>'s logic.
    /// </summary>
    public virtual void Update() { }
    
    /// <summary>
    /// Sets an attribute.
    /// </summary>
    /// <param name="attribute">the name of the attribute to set</param>
    /// <param name="value">the new value of the attribute</param>
    public void SetAttribute(string attribute, string value)
    {
        Attributes.TryAdd(attribute, value);
        Attributes[attribute] = value;
    }
    
    /// <summary>
    /// Gets an attribute's value.
    /// </summary>
    /// <param name="attribute">the name of the attribute to set</param>
    /// <returns>the value of the attribute, or an empty string if it was not found</returns>
    public string GetAttribute(string attribute)
    {
        return Attributes.GetValueOrDefault(attribute, "");
    }
    
    
    public List<IGuiElement> GetElementsById(string id)
    {
        return Children.FindAll(c => c.Id == id);
    }
    
    public List<IGuiElement> GetElementsByAttribute(string attribute, string value)
    {
        return Children.FindAll(c => c.GetAttribute(attribute) == value);
    }
    
    public List<IGuiElement> GetElementsByPredicate(Predicate<IGuiElement> match)
    {
        return Children.FindAll(match);
    }
}
