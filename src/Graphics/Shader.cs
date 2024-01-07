using System;
using System.IO;
using OpenTK.Graphics.OpenGL;

namespace Sandbox2D.Graphics;

public sealed class Shader : IDisposable
{
    public readonly int Handle;
    
    public void Use()
    {
        GL.UseProgram(Handle);
    }

    /// <summary>
    /// Creates a shader from an already existing shader
    /// </summary>
    /// <param name="shader">the handle of the existing shader</param>
    public Shader(int shader)
    {
        Handle = shader;
    }

    /// <summary>
    /// Creates a Shader
    /// </summary>
    /// <param name="vertexPath">the path to the vertex shader, relative to `assets/shaders/`</param>
    /// <param name="fragmentPath">the path to the fragment shader, relative to `assets/shaders/`</param>
    public Shader(string vertexPath, string fragmentPath)
    {
        // read the files
        var vertexShaderSource = File.ReadAllText("assets/shaders/" + vertexPath);
        var fragmentShaderSource = File.ReadAllText("assets/shaders/" + fragmentPath);
        
        // generate and bind the vert/frag shaders
        var vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);

        var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        
        // compile the vert/frag shaders
        
        GL.CompileShader(vertexShader);
        GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out var success);
        if (success == 0)
        {
            var infoLog = GL.GetShaderInfoLog(vertexShader);
            Console.WriteLine(infoLog);
        }

        GL.CompileShader(fragmentShader);
        GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out success);
        if (success == 0)
        {
            var infoLog = GL.GetShaderInfoLog(fragmentShader);
            Console.WriteLine(infoLog);
        }
        
        // link the shaders
        
        Handle = GL.CreateProgram();
        
        GL.AttachShader(Handle, vertexShader);
        GL.AttachShader(Handle, fragmentShader);
        
        GL.LinkProgram(Handle);
        
        // check for success
        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out success);
        if (success == 0)
        {
            var infoLog = GL.GetProgramInfoLog(Handle);
            Console.WriteLine(infoLog);
        }
        
        // detach and delete the vert/frag shaders
        GL.DetachShader(Handle, vertexShader);
        GL.DetachShader(Handle, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
        
    }
    
    public int GetAttribLocation(string attribName)
    {
        return GL.GetAttribLocation(Handle, attribName);
    }
    
    // disposing
    
    private bool _disposedValue;

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            GL.DeleteProgram(Handle);

            _disposedValue = true;
        }
    }

    ~Shader()
    {
        if (_disposedValue == false)
        {
            Console.WriteLine("GPU Resource leak :o ! Did you forget to call Dispose()?");
        }
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}