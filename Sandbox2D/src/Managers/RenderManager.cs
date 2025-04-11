using System;
using System.Collections.Generic;
using System.ComponentModel;
using Math2D;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Sandbox2D.Registry_;
using Sandbox2D.UserInterface.Keybinds;

namespace Sandbox2D.Managers;

/// <summary>
/// Represents an object that manages a render thread.
/// </summary>
public abstract class RenderManager(IRegistryPopulator registryPopulator) : IDisposable
{
    private WindowManager _windowManager;
    
    private readonly HashSet<string> _supportedExtensions = [];

    public Vec2<int> ScreenSize => _windowManager.ClientSize.ToVec2();
    protected KeybindSieve KeybindManager => _windowManager.KeybindManager;
    protected static GuiManager GuiManager => GlContext.Registry.Gui;
    protected IGLFWGraphicsContext Context => _windowManager.Context;
    
    public void SetWindowManager(WindowManager windowManager)
    {
        _windowManager = windowManager;
    }
    
    /// <summary>
    /// Renders the next frame.
    /// </summary>
    /// <param name="frametime">the amount of time that passed since the previous frame, in seconds</param>
    public virtual void Render(double frametime) { }
    
    /// <summary>
    /// Handles all the controls. Runs before <see cref="Render"/>.
    /// </summary>
    public virtual void UpdateControls(MouseState mouseState, KeyboardState keyboardState) { }
    
    /// <summary>
    /// Called every time the window gets resized.
    /// </summary>
    /// <param name="newSize">the new window size</param>
    public virtual void OnResize(Vec2<int> newSize) { }
    
    /// <summary>
    /// Called when the <see cref="RenderManager"/> gets paused.
    /// </summary>
    public virtual void OnPause() { }
    
    /// <summary>
    /// Called when the <see cref="RenderManager"/> gets unpaused.
    /// </summary>
    public virtual void OnUnpause() { }
    
    /// <summary>
    /// Called when the window tries to close.
    /// </summary>
    /// <param name="c"><see cref="CancelEventArgs"/> to allow for canceling the close event, and keeping the window
    /// open</param>
    public virtual void OnClose(CancelEventArgs c) { }
    
    /// <summary>
    /// Initializes the <see cref="RenderManager"/>. Runs once, at creation.
    /// </summary>
    public virtual void Initialize()
    {
        GlContext.RegisterContext();
        GlContext.RenderManager = this;
        
        registryPopulator.Register();
        
        // load supported extensions
        GL.GetInteger(GetPName.NumExtensions, out var extensionCount);
        for (var i = 0; i < extensionCount; i++) {
            var ext = GL.GetString(StringNameIndexed.Extensions, i);
            _supportedExtensions.Add(ext);
        }
    }
    
    protected bool IsExtensionSupported(string extension)
    {
        return _supportedExtensions.Contains(extension);
    }
    
    /// <summary>
    /// Converts screen coords (like mouse pos) into vertex coords (used by OpenGL)
    /// </summary>
    /// <param name="screenCoords">the screen coords to convert</param>
    public Vec2<float> ScreenToVertexCoords(Vec2<float> screenCoords)
    {
        return ScreenToVertexCoords(screenCoords, ScreenSize);
    }
    
    /// <summary>
    /// Converts screen coords (like mouse pos) into vertex coords (used by OpenGL)
    /// </summary>
    /// <param name="screenCoords">the screen coords to convert</param>
    /// <param name="screenSize">the size of the screen</param>
    public static Vec2<float> ScreenToVertexCoords(Vec2<float> screenCoords, Vec2<int> screenSize)
    {
        // cast screenCoords to a float
        var vertexCoords = screenCoords;
        
        // divide vertexCoords by screenSize, to get it to a 0-1 range
        vertexCoords /= (Vec2<float>)screenSize;
        
        // multiply vertexCoords by 2, to get it to a 0-2 range
        vertexCoords *= 2;
        
        // subtract 1 from vertexCoords, to get it to a (-1)-1 range
        vertexCoords -= new Vec2<float>(1);
        
        // negate the Y axis to flip the coords correctly
        vertexCoords = vertexCoords.FlipY();
        
        // return vertex coords
        return vertexCoords;
    }
    
    
    public virtual void Dispose()
    {
        _windowManager.Dispose();
        _supportedExtensions.Clear();
    }
}
