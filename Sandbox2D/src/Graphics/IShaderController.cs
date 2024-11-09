using OpenTK.Graphics.OpenGL4;

namespace Sandbox2D.Graphics;

/// <summary>
/// Represents an object that interfaces with a <see cref="ShaderProgram"/> that runs on the GPU.
/// </summary>
public interface IShaderController
{
    /// <summary>
    /// The <see cref="ShaderProgram"/> to run on the GPU
    /// </summary>
    protected ShaderProgram Shader { get; }
    
    /// <summary>
    /// The <see cref="BufferUsageHint"/> for any buffers used by this <see cref="IShaderController"/>
    /// </summary>
    protected BufferUsageHint Hint { get; init; }
    
    /// <summary>
    /// Runs the <see cref="Shader"/>.
    /// </summary>
    public void Invoke();
    
    /// <summary>
    /// Resets the geometry of this <see cref="IShaderController"/>.
    /// </summary>
    public void ResetGeometry();
}
