#nullable enable
using OpenTK.Graphics.OpenGL4;

namespace Sandbox2D.Graphics;

public abstract class Renderable(Shader shader, BufferUsageHint hint = BufferUsageHint.StaticDraw)
{
    /// <summary>
    /// The shader to use
    /// </summary>
    public Shader Shader { get; } = shader;

    // rendering arrays/buffers
    protected readonly int VertexArrayObject = GL.GenVertexArray();
    protected readonly int VertexBufferObject = GL.GenBuffer();
    protected readonly int ElementBufferObject = GL.GenBuffer();
    
    protected BufferUsageHint Hint = hint;
    

    /// <summary>
    /// Renders the geometry
    /// </summary>
    public abstract void Render();

    /// <summary>
    /// Updates the VAO, applying any changes to the geometry
    /// <param name="hint">an optional BufferUsageHint. if this parameter is not null, the default hint will be updated</param>
    /// </summary>
    public virtual void UpdateVao(BufferUsageHint? hint = null)
    {
        // update the hint if it is not null
        if (hint != null) Hint = (BufferUsageHint)hint;
    }

    /// <summary>
    /// Resets the geometry of this renderable. Does not update the VAO
    /// </summary>
    public abstract void ResetGeometry();
}