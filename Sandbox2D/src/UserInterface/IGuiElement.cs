using System.Collections.Generic;

namespace Sandbox2D.UserInterface;

public interface IGuiElement
{
    /// <summary>
    /// The attributes of this <see cref="IGuiElement"/>
    /// </summary>
    protected Dictionary<string, string> Attributes { get; }
    
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
    /// Renders the <see cref="IGuiElement"/>.
    /// </summary>
    public void Render();
    /// <summary>
    /// Updates the <see cref="IGuiElement"/>'s logic.
    /// </summary>
    public void Update();
    /// <summary>
    /// Updates the <see cref="IGuiElement"/>'s VAO.
    /// </summary>
    public void UpdateVao();
    /// <summary>
    /// Resets the <see cref="IGuiElement"/>'s geometry.
    /// </summary>
    public void ResetGeometry();
}
