using System;
using System.IO;
using System.Text;
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
            Util.Fatal(new ShaderCompileException(type, Handle, GL.GetShaderInfoLog(Handle)));
        }
    }
}

public class ShaderCompileException(ShaderType type, int handle, string infoLog)
    : Exception("Error occurred whilst compiling Shader")
{
    public override string ToString()
    {
        var errorStr = new StringBuilder();
        foreach (var error in infoLog.Split("\n"))
        {
            errorStr.Append($"   {error}\n");
        }
        if (errorStr.Length == 0) errorStr.Append('\n');
        
        return $"{nameof(ShaderCompileException)}: Error occurred whilst compiling Shader {{type={type}, handle={handle}}}:\n" +
               $"{errorStr}" +
               $"Compiled: \n" +
               $"{StackTrace}";
    }
}
