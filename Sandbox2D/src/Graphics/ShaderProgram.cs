using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Math2D;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Sandbox2D.Graphics;

public class ShaderProgram : IDisposable
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
    
    private int GetUniformLocation(string name, GlType uniformType)
    {
        if (_uniformLocations.TryGetValue(name, out var location))
        {
            return location;
        }
        
        Util.Error($"Uniform \'{uniformType} {name}\' is not present in the Shader Program {{handle={Handle}}}");
        _uniformLocations.Add(name, -1);
        return -1;
    }

    #region Uniform Setters
    
    public void Set(string uniform, int data)
    {
        var location = GetUniformLocation(uniform, GlType.@int);
        if (location == -1) return;
        
        GL.Uniform1(location, data);
    }
    
    public void Set(string uniform, uint data)
    {
        var location = GetUniformLocation(uniform, GlType.@uint);
        if (location == -1) return;
        
        GL.Uniform1(location, data);
    }
    
    public void Set(string uniform, float data)
    {
        var location = GetUniformLocation(uniform, GlType.@float);
        if (location == -1) return;
        
        GL.Uniform1(location, data);
    }
    
    public void Set(string uniform, double data)
    {
        var location = GetUniformLocation(uniform, GlType.@double);
        if (location == -1) return;
        
        GL.Uniform1(location, data);
    }
    
    public void Set(string uniform, Vec2<int> data)
    {
        var location = GetUniformLocation(uniform, GlType.ivec2);
        if (location == -1) return;
        
        GL.Uniform2(location, data.X, data.Y);
    }
    
    public void Set(string uniform, Vec2<uint> data)
    {
        var location = GetUniformLocation(uniform, GlType.uvec2);
        if (location == -1) return;
        
        GL.Uniform2(location, data.X, data.Y);
    }
    
    public void Set(string uniform, Vec2<float> data)
    {
        var location = GetUniformLocation(uniform, GlType.vec2);
        if (location == -1) return;
        
        GL.Uniform2(location, data.X, data.Y);
    }
    
    public void Set(string uniform, Vec2<double> data)
    {
        var location = GetUniformLocation(uniform, GlType.dvec2);
        if (location == -1) return;
        
        GL.Uniform2(location, data.X, data.Y);
    }
    
    public void Set(string uniform, Vec2<long> data)
    {
        var location = GetUniformLocation(uniform, GlType.i64vec2);
        if (location == -1) return;
        
        GL.Arb.Uniform2(location, data.X, data.Y);
    }
    
    public void Set(string uniform, Vec2<ulong> data)
    {
        var location = GetUniformLocation(uniform, GlType.u64vec2);
        if (location == -1) return;
        
        GL.Arb.Uniform2(location, data.X, data.Y);
    }
    
    #region OpenTK Types
    
    public void Set(string uniform, Vector2 data)
    {
        var location = GetUniformLocation(uniform, GlType.vec2);
        if (location == -1) return;
        
        GL.Uniform2(location, data);
    }
    public void Set(string uniform, Vector2i data)
    {
        var location = GetUniformLocation(uniform, GlType.ivec2);
        if (location == -1) return;
        
        GL.Uniform2(location, data);
    }
    
    public void Set(string uniform, Vector3 data)
    {
        var location = GetUniformLocation(uniform, GlType.vec3);
        if (location == -1) return;
        
        GL.Uniform3(location, data);
    }
    public void Set(string uniform, Vector3i data)
    {
        var location = GetUniformLocation(uniform, GlType.ivec3);
        if (location == -1) return;
        
        GL.Uniform3(location, data);
    }
    
    public void Set(string uniform, Vector4 data)
    {
        var location = GetUniformLocation(uniform, GlType.vec4);
        if (location == -1) return;
        
        GL.Uniform4(location, data);
    }
    public void Set(string uniform, Vector4i data)
    {
        var location = GetUniformLocation(uniform, GlType.ivec4);
        if (location == -1) return;
        
        GL.Uniform4(location, data);
    }
    
    
    public void Set(string uniform, Matrix4 data, bool transpose)
    {
        var location = GetUniformLocation(uniform, GlType.mat4);
        if (location == -1) return;
        
        GL.UniformMatrix4(location, transpose, ref data);
    }
    
    #endregion
    
    #endregion
    
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private enum GlType
    {
        @int,
        @uint,
        @float,
        @double,
        
        ivec2,
        uvec2,
        vec2,
        dvec2,
        i64vec2,
        u64vec2,
        
        ivec3,
        uvec3,
        vec3,
        dvec3,
        i64vec3,
        u64vec3,
        
        ivec4,
        uvec4,
        vec4,
        dvec4,
        i64vec4,
        u64vec4,
        
        mat4,

    }

    public void Dispose()
    {
        _uniformLocations.Clear();
        GL.DeleteProgram(Handle);
    }
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
