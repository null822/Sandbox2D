using System.Collections.Generic;
using System.Xml;
using Sandbox2D.Registry_.Registries;

namespace Sandbox2D.UserInterface;

public abstract class IGuiElement
{
    /// <summary>
    /// The attributes of this <see cref="IGuiElement"/>
    /// </summary>
    protected Dictionary<string, string> Attributes { get; } = new();
    
    protected IGuiElement(GuiElementArguments args)
    {
        args.Children.ConsumeAll(n => n.Name == "#comment");
        
        foreach (var attribute in args.Attributes)
        {
            Attributes.Add(attribute.Name, attribute.Value);
        }
    }
    
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
    
    /// <summary>
    /// Initializes the <see cref="IGuiElement"/>'s rendering data.
    /// </summary>
    public virtual void GlInitialize() { }
    /// <summary>
    /// Renders the <see cref="IGuiElement"/>.
    /// </summary>
    public virtual void Render() { }
    /// <summary>
    /// Updates the <see cref="IGuiElement"/>'s logic.
    /// </summary>
    public virtual void Update() { }
}
