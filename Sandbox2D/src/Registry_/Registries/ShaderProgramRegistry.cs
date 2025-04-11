using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Sandbox2D.Graphics;

namespace Sandbox2D.Registry_.Registries;

public class ShaderProgramRegistry : IRegistryFactory<ShaderProgram, string[]>
{
    private readonly Dictionary<string, int[]> _shaders = new();
    
    public void Register(string id, params string[] shaderIds)
    {
        var handles = new int[shaderIds.Length];
        for (var i = 0; i < shaderIds.Length; i++)
        {
            handles[i] = GlContext.Registry.Shader.Get(shaderIds[i]).Handle;
        }
        
        _shaders.Add(id, handles);
    }
    
    public ShaderProgram Create(string id)
    {
        if (_shaders.TryGetValue(id, out var handles))
        {
            return new ShaderProgram(handles);
        }
        
        throw new ArgumentException($"Shader Program \"{id}\" was not found");
    }
    
    public bool TryCreate(string id, [MaybeNullWhen(false)] out ShaderProgram value)
    {
        if (_shaders.TryGetValue(id, out var shader))
        {
            value = new ShaderProgram(shader);
            return true;
        }
        value = null;
        return false;
    }
}
