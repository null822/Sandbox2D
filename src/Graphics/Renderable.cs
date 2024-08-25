#nullable enable
using System;
using OpenTK.Graphics.OpenGL4;
using Sandbox2D.Graphics.Registry;

namespace Sandbox2D.Graphics;

public abstract class Renderable : IDisposable
{
    /// <summary>
    /// The shader to use
    /// </summary>
    protected Shader Shader { get; }
    
    /// <summary>
    /// Describes if the Renderable was updated since the last time it was rendered.
    /// If set to true, the VAO will automatically update upon render.
    /// </summary>
    private bool _updatedSinceLastFrame;
    
    /// <summary>
    /// Describes if rendering the Renderable will do anything (e.g. set to false if the geometry is empty).
    /// </summary>
    protected bool ShouldRender = true;

    // rendering arrays/buffers
    protected readonly int VertexArrayObject;
    protected readonly int VertexBufferObject;
    protected readonly int ElementBufferObject;
    
    protected readonly BufferUsageHint Hint;
    
    protected Renderable(Shader shader, BufferUsageHint hint = BufferUsageHint.StaticDraw)
    {
        VertexArrayObject = GL.GenVertexArray();
        VertexBufferObject = GL.GenBuffer();
        ElementBufferObject = GL.GenBuffer();
        
        Shader = shader;
        Hint = hint;
    }
    
    /// <summary>
    /// Renders the geometry
    /// </summary>
    public virtual void Render()
    {
        // if the renderable has un-updated changes, update the vao
        if (_updatedSinceLastFrame)
            UpdateVao();
    }

    /// <summary>
    /// Updates the VAO, applying any changes to the geometry
    /// </summary>
    public virtual void UpdateVao()
    {
        _updatedSinceLastFrame = false;
    }

    /// <summary>
    /// Resets the geometry of this renderable. Does not update the VAO
    /// </summary>
    public virtual void ResetGeometry()
    {
        // set the flags
        ShouldRender = false;
        _updatedSinceLastFrame = true;
    }

    /// <summary>
    /// Sets the flags correctly for when new geometry was added to the Renderable
    /// </summary>
    protected void GeometryAdded()
    {
        ShouldRender = true;
        _updatedSinceLastFrame = true;
    }
    
    public virtual void Dispose()
    {
        GL.DeleteBuffer(VertexArrayObject);
        GL.DeleteBuffer(VertexBufferObject);
        GL.DeleteBuffer(ElementBufferObject);
    }
}
