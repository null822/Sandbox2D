using System;

namespace Sandbox2D.UserInterface;

/// <summary>
/// Represents an <see cref="Action"/> that can be invoked by a <see cref="IGuiElement"/>.
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

    public static GuiEvent operator +(GuiEvent @event, Action action)
    {
        @event.Event += action;
        return @event;
    }
    
    public static GuiEvent operator -(GuiEvent @event, Action action)
    {
        @event.Event -= action;
        return @event;
    }
}