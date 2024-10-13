#nullable enable
using OpenTK.Graphics.OpenGL4;

namespace Sandbox2D.Graphics;

public interface IRenderable
{
    /// <summary>
    /// The shader to run on the GPU
    /// </summary>
    protected ShaderProgram Shader { get; }
    
    // rendering arrays/buffers
    protected int VertexArrayObject { get; init; }
    protected int VertexBufferObject { get; init; }
    protected int ElementBufferObject { get; init; }
    
    protected BufferUsageHint Hint { get; init; }
    
    /// <summary>
    /// Runs the <see cref="Shader"/>.
    /// </summary>
    public void Render();
    
    /// <summary>
    /// Updates the VAO, applying any changes to the geometry.
    /// </summary>
    public void UpdateVao();
    
    /// <summary>
    /// Resets the geometry of this renderable.
    /// </summary>
    public void ResetGeometry();
}
