using OpenTK.Graphics.OpenGL4;
using Sandbox2D.Registry_;

namespace Sandbox2D.Graphics;

/// <summary>
/// Represents an object that interfaces with a <see cref="ShaderProgram"/> that runs on the GPU.
/// </summary>
public abstract class ShaderController(ShaderProgram shader, BufferUsageHint hint)
{
    /// <summary>
    /// The <see cref="ShaderProgram"/> to run on the GPU
    /// </summary>
    protected ShaderProgram Shader { get; } = shader;

    /// <summary>
    /// The <see cref="BufferUsageHint"/> for any buffers used by this <see cref="ShaderController"/>
    /// </summary>
    protected BufferUsageHint Hint { get; } = hint;

    protected ShaderController(string shaderProgramName, BufferUsageHint hint)
        : this(GlContext.Registry.ShaderProgram.Create(shaderProgramName), hint) { }
    
    /// <summary>
    /// Runs the <see cref="Shader"/>.
    /// </summary>
    public abstract void Invoke();
    
    /// <summary>
    /// Resets the geometry of this <see cref="ShaderController"/>.
    /// </summary>
    public abstract void ResetGeometry();
}
