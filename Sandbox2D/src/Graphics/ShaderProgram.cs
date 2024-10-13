using System.Collections.Generic;
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
        
    /// <summary>
    /// Set a uniform float on this shader.
    /// </summary>
    /// <param name="name">the name of the uniform</param>
    /// <param name="data">the data to set</param>
    public void Set(string name, uint data)
    {
        var location = GetUniformLocation(name);
        if (location == -1) return;
            
        GL.UseProgram(Handle);
        GL.Uniform1(location, data);
    }
        
    /// <summary>
    /// Set a uniform int on this shader.
    /// </summary>
    /// <param name="name">the name of the uniform</param>
    /// <param name="data">the data to set</param>
    public void Set(string name, int data)
    {
        var location = GetUniformLocation(name);
        if (location == -1) return;
            
        GL.UseProgram(Handle);
        GL.Uniform1(location, data);
    }

    /// <summary>
    /// Set a uniform float on this shader.
    /// </summary>
    /// <param name="name">the name of the uniform</param>
    /// <param name="data">the data to set</param>
    public void Set(string name, float data)
    {
        var location = GetUniformLocation(name);
        if (location == -1) return;
            
        GL.UseProgram(Handle);
        GL.Uniform1(location, data);
    }

    /// <summary>
    /// Set a uniform Matrix4 on this shader
    /// </summary>
    /// <param name="name">the name of the uniform</param>
    /// <param name="data">the data to set</param>
    /// <remarks>
    /// The matrix is transposed before being sent to the shader.
    /// </remarks>
    public void Set(string name, Matrix4 data)
    {
        var location = GetUniformLocation(name);
        if (location == -1) return;
            
        GL.UseProgram(Handle);
        GL.UniformMatrix4(location, true, ref data);
    }
        
    /// <summary>
    /// Set a uniform Vector2 on this shader.
    /// </summary>
    /// <param name="name">the name of the uniform</param>
    /// <param name="data">the data to set</param>
    public void Set(string name, Vector2 data)
    {
        var location = GetUniformLocation(name);
        if (location == -1) return;
            
        GL.UseProgram(Handle);
        GL.Uniform2(location, data);
    }

    /// <summary>
    /// Set a uniform Vector2 on this shader.
    /// </summary>
    /// <param name="name">the name of the uniform</param>
    /// <param name="data">the data to set</param>
    public void Set(string name, Vector2i data)
    {
        var location = GetUniformLocation(name);
        if (location == -1) return;
            
        GL.UseProgram(Handle);
        GL.Uniform2(location, data);
    }
    
    /// <summary>
    /// Set a uniform Vector3 on this shader.
    /// </summary>
    /// <param name="name">the name of the uniform</param>
    /// <param name="data">the data to set</param>
    public void Set(string name, Vector3 data)
    {
        var location = GetUniformLocation(name);
        if (location == -1) return;
        
        GL.UseProgram(Handle);
        GL.Uniform3(location, data);
    }
}