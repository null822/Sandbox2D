using System;
using System.Collections.Generic;
using System.Text;
using Math2D;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Sandbox2D.Graphics;

public class ShaderProgram
{
    public readonly int Handle;
    
    private readonly Dictionary<string, int> _uniformLocations;
    
    public ShaderProgram(Shader[] shaders) : this(GetHandles(shaders)) { }
    public ShaderProgram(int[] shaderHandles)
    {
        // create the shader program
        Handle = GL.CreateProgram();
        
        // attach the shaders
        foreach (var shaderHandle in shaderHandles)
        {
            GL.AttachShader(Handle, shaderHandle);
        }
        
        // link the shaders together
        GL.LinkProgram(Handle);
        
        // store the uniform name -> location in a dictionary for quick access
        GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out var uniformCount);
        _uniformLocations = new Dictionary<string, int>(uniformCount);
        for (var i = 0; i < uniformCount; i++)
        {
            var name = GL.GetActiveUniform(Handle, i, out _, out _);
            var location = GL.GetUniformLocation(Handle, name);
            
            _uniformLocations.Add(name, location);
        }
        
        // throw any linker errors
        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out var code);
        if (code != (int)All.True)
        {
            Util.Fatal(new ShaderProgramLinkException(Handle, GL.GetProgramInfoLog(Handle)));
        }
    }
    
    private static int[] GetHandles(Shader[] shaders)
    {
        var handles = new int[shaders.Length];
        for (var i = 0; i < shaders.Length; i++)
        {
            handles[i] = shaders[i].Handle;
        }
        
        return handles;
    }
    
    public void Use()
    {
        GL.UseProgram(Handle);
    }
    
    public int GetAttribLocation(string attribName)
    {
        return GL.GetAttribLocation(Handle, attribName);
    }
    
    private int GetUniformLocation(string name)
    {
        if (_uniformLocations.TryGetValue(name, out var location))
        {
            return location;
        }
            
        Util.Error($"Uniform \'{name}\' is not present in the shader");
        _uniformLocations.Add(name, -1);
        return -1;
    }

    #region Uniform Setters
    
    public void Set(string uniform, int data)
    {
        var location = GetUniformLocation(uniform);
        if (location == -1) return;
        
        GL.Uniform1(location, data);
    }
    
    public void Set(string uniform, uint data)
    {
        var location = GetUniformLocation(uniform);
        if (location == -1) return;
        
        GL.Uniform1(location, data);
    }
    
    public void Set(string uniform, float data)
    {
        var location = GetUniformLocation(uniform);
        if (location == -1) return;
        
        GL.Uniform1(location, data);
    }
    
    public void Set(string uniform, double data)
    {
        var location = GetUniformLocation(uniform);
        if (location == -1) return;
        
        GL.Uniform1(location, data);
    }
    
    public void Set(string uniform, Vec2<int> data)
    {
        var location = GetUniformLocation(uniform);
        if (location == -1) return;
        
        GL.Uniform2(location, data.X, data.Y);
    }
    
    public void Set(string uniform, Vec2<uint> data)
    {
        var location = GetUniformLocation(uniform);
        if (location == -1) return;
        
        GL.Uniform2(location, data.X, data.Y);
    }
    
    public void Set(string uniform, Vec2<float> data)
    {
        var location = GetUniformLocation(uniform);
        if (location == -1) return;
        
        GL.Uniform2(location, data.X, data.Y);
    }
    
    public void Set(string uniform, Vec2<double> data)
    {
        var location = GetUniformLocation(uniform);
        if (location == -1) return;
        
        GL.Uniform2(location, data.X, data.Y);
    }
    
    public void Set(string uniform, Vec2<long> data)
    {
        var location = GetUniformLocation(uniform);
        if (location == -1) return;
        
        GL.Arb.Uniform2(location, data.X, data.Y);
    }
    
    public void Set(string uniform, Vec2<ulong> data)
    {
        var location = GetUniformLocation(uniform);
        if (location == -1) return;
        
        GL.Arb.Uniform2(location, data.X, data.Y);
    }
    
    #region OpenTK Types
    
    public void Set(string uniform, Vector2 data)
    {
        var location = GetUniformLocation(uniform);
        if (location == -1) return;
        
        GL.Uniform2(location, data);
    }
    public void Set(string uniform, Vector2i data)
    {
        var location = GetUniformLocation(uniform);
        if (location == -1) return;
        
        GL.Uniform2(location, data);
    }
    
    public void Set(string uniform, Vector3 data)
    {
        var location = GetUniformLocation(uniform);
        if (location == -1) return;
        
        GL.Uniform3(location, data);
    }
    public void Set(string uniform, Vector3i data)
    {
        var location = GetUniformLocation(uniform);
        if (location == -1) return;
        
        GL.Uniform3(location, data);
    }
    
    public void Set(string uniform, Vector4 data)
    {
        var location = GetUniformLocation(uniform);
        if (location == -1) return;
        
        GL.Uniform4(location, data);
    }
    public void Set(string uniform, Vector4i data)
    {
        var location = GetUniformLocation(uniform);
        if (location == -1) return;
        
        GL.Uniform4(location, data);
    }
    
    
    public void Set(string uniform, Matrix4 data, bool transpose)
    {
        var location = GetUniformLocation(uniform);
        if (location == -1) return;
        
        GL.UniformMatrix4(location, transpose, ref data);
    }
    
    #endregion
    
    #endregion

}

public class ShaderProgramLinkException(int handle, string infoLog)
    : Exception("Error occurred whilst linking Shader Program")
{
    public override string ToString()
    {
        var errorStr = new StringBuilder();
        foreach (var error in infoLog.Split("\n"))
        {
            errorStr.Append($"   {error}\n");
        }
        if (errorStr.Length == 0) errorStr.Append('\n');
        
        return $"{nameof(ShaderCompileException)}: Error occurred whilst linking Shader Program {{handle={handle}}}:\n" +
               $"{errorStr}" +
               $"Linked: \n" +
               $"{StackTrace}";
    }
}
