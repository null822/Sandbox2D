using System;
using System.Collections.Generic;
using Sandbox2D.UserInterface;

namespace Sandbox2D.Registry_.Registries;

public class GuiEventRegistry : IRegistry<GuiEvent, Action[]>
{
    private readonly Dictionary<string, GuiEvent> _events = new();
    
    public void Register(string name, params Action[] gui)
    {
        var @event = new GuiEvent();
        foreach (var action in gui)
        {
            @event += action;
        }
        _events.Add(name, @event);
    }
    
    public GuiEvent Get(string id)
    {
        if (_events.TryGetValue(id, out var guiEvent))
        {
            return guiEvent;
        }
        
        throw new ArgumentException($"GUI Event \"{id}\" is not registered");
    }
    
    public bool TryGet(string id, out GuiEvent @event)
    {
        return _events.TryGetValue(id, out @event);
    }
    
    /// <summary>
    /// Invokes a <see cref="GuiEvent"/>
    /// </summary>
    /// <param name="eventName">the name of the <see cref="GuiEvent"/></param>
    public void Invoke(string eventName)
    {
        Get(eventName).Invoke();
    }
}
