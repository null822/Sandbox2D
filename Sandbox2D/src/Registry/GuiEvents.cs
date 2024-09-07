using System;
using System.Collections.Generic;
using Sandbox2D.UserInterface;

namespace Sandbox2D.Registry;

public static class GuiEvents
{
    private static readonly Dictionary<string, GuiEvent> Values = new();
    
    /// <summary>
    /// Registers a new <see cref="GuiEvent"/>.
    /// </summary>
    /// <param name="eventName">the name of the <see cref="GuiEvent"/></param>
    public static void Register(string eventName)
    {
        Values.Add(eventName, new GuiEvent());
    }
    
    /// <summary>
    /// Registers a new <see cref="GuiEvent"/>
    /// </summary>
    /// <param name="eventName">the name of the <see cref="GuiEvent"/></param>
    /// <param name="action">an initial subscriber to the <see cref="GuiEvent"/></param>
    public static void Register(string eventName, Action action)
    {
        Register(eventName);
        Get(eventName).Add(action);
    }
    
    /// <summary>
    /// Gets a <see cref="GuiEvent"/>.
    /// </summary>
    /// <param name="eventName">the name of the <see cref="GuiEvent"/></param>
    /// <exception cref="MissingTileException">thrown when the <see cref="GuiEvent"/> does not exist</exception>
    public static GuiEvent Get(string eventName)
    {
        if (Values.TryGetValue(eventName, out var guiEvent))
        {
            return guiEvent;
        }
        
        throw new MissingGuiComponentException(GuiComponent.GuiEvent, eventName);
    }
    
    /// <summary>
    /// Invokes a <see cref="GuiEvent"/>
    /// </summary>
    /// <param name="eventName">the name of the <see cref="GuiEvent"/></param>
    public static void Invoke(string eventName)
    {
        Get(eventName).Invoke();
    }
}

/// <summary>
/// Represents an <see cref="Action"/> that can be invoked by a <see cref="GuiElement"/>.
/// </summary>
public class GuiEvent
{
    private event Action Event;
        
    /// <summary>
    /// Invokes the <see cref="GuiEvent"/>.
    /// </summary>
    public void Invoke()
    {
        Event?.Invoke();
    }
        
    /// <summary>
    /// Adds a subscriber to the <see cref="GuiEvent"/>.
    /// </summary>
    /// <param name="action">the subscriber</param>
    public void Add(Action action)
    {
        Event += action;
    }
        
    /// <summary>
    /// Removes a subscriber from the <see cref="GuiEvent"/>.
    /// </summary>
    /// <param name="action">the subscriber</param>
    public void Remove(Action action)
    {
        Event -= action;
    }
}