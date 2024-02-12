#nullable enable
using OpenTK.Graphics.OpenGL4;
using Sandbox2D.Graphics.Registry;

namespace Sandbox2D.Graphics;

public abstract class Renderable
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
    protected readonly int VertexArrayObject = GL.GenVertexArray();
    protected readonly int VertexBufferObject = GL.GenBuffer();
    protected readonly int ElementBufferObject = GL.GenBuffer();
    
    protected BufferUsageHint Hint;

    protected Renderable(Shader shader, BufferUsageHint hint = BufferUsageHint.StaticDraw)
    {
        Shader = shader;
        Hint = hint;
    }
    
    /// <summary>
    /// Renders the geometry
    /// </summary>
    /// <param name="category"></param>
    public virtual void Render(RenderableCategory category = RenderableCategory.All)
    {
        // if the renderable has un-updated changes, update the vao
        if (_updatedSinceLastFrame)
            UpdateVao();
    }

    /// <summary>
    /// Updates the VAO, applying any changes to the geometry
    /// <param name="hint">an optional BufferUsageHint. if this parameter is not null, the default hint will be updated</param>
    /// </summary>
    public virtual void UpdateVao(BufferUsageHint? hint = null, RenderableCategory category = RenderableCategory.All)
    {
        // update the hint if it is not null
        if (hint != null) Hint = (BufferUsageHint)hint;

        _updatedSinceLastFrame = false;
    }

    /// <summary>
    /// Resets the geometry of this renderable. Does not update the VAO
    /// </summary>
    public virtual void ResetGeometry(RenderableCategory category = RenderableCategory.All)
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

    protected virtual bool IsInCategory(RenderableCategory category)
    {
        return category == RenderableCategory.All;
    }
}