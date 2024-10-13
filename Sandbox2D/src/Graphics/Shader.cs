using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;

namespace Sandbox2D.Graphics;

public class Shader
{
    public readonly int Handle;
    
    public Shader(string shaderSource, ShaderType type)
    {
        // create shader
        Handle = GL.CreateShader(type);
        
        // bind the shader source
        // var shaderSource = ;
        GL.ShaderSource(Handle, shaderSource);
        
        // compile the shader
        GL.CompileShader(Handle);
        
        // throw any compilation errors
        GL.GetShader(Handle, ShaderParameter.CompileStatus, out var code);
        if (code != (int)All.True)
        {
            throw new Exception($"Error occurred whilst compiling Shader {Handle}: {GL.GetShaderInfoLog(Handle)}");
        }
    }
}