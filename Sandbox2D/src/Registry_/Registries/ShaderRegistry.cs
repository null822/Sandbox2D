using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using Sandbox2D.Graphics;

namespace Sandbox2D.Registry_.Registries;

public class ShaderRegistry : IRegistry<Shader>
{
    private readonly Dictionary<string, Shader> _shaders = new();

    public void RegisterAll(string path)
    {
        path = path.Replace('/', Path.DirectorySeparatorChar);
        if (path.EndsWith(Path.DirectorySeparatorChar)) path = path[..^1];
        foreach (var shaderPath in Directory.GetFiles(path, "", SearchOption.AllDirectories))
        {
            var type = Path.GetExtension(shaderPath) switch
            {
                ".vsh" => ShaderType.VertexShader,
                ".fsh" => ShaderType.FragmentShader,
                ".gsh" => ShaderType.GeometryShader,
                ".tcsh" => ShaderType.TessControlShader,
                ".tesh" => ShaderType.TessEvaluationShader,
                ".csh" => ShaderType.ComputeShader,
                
                ".vert" => ShaderType.VertexShader,
                ".frag" => ShaderType.FragmentShader,
                ".geom" => ShaderType.GeometryShader,
                ".tesc" => ShaderType.TessControlShader,
                ".tese" => ShaderType.TessEvaluationShader,
                ".comp" => ShaderType.ComputeShader,
                
                _ => (ShaderType)0,
            };
            
            if (type == 0)
                continue;
            var name = shaderPath
                .Replace($"{path}{Path.DirectorySeparatorChar}", "")
                .Replace(Path.GetExtension(shaderPath), "")
                .Replace(Path.DirectorySeparatorChar, '/');
            
            var fullName = $"{name}_{ShaderTypeToString(type)}";
            var text = File.ReadAllText(shaderPath);
            Register(fullName, new Shader(text, type));
        }
    }

    private string ShaderTypeToString(ShaderType type)
    {
        return type switch
        {
            ShaderType.ComputeShader => "comp",
            ShaderType.VertexShader => "vert",
            ShaderType.FragmentShader => "frag",
            ShaderType.GeometryShader => "geom",
            ShaderType.TessControlShader => "tesc",
            ShaderType.TessEvaluationShader => "tese",
            _ => "none"
        };
    }
    
    public void Register(string id, Shader shader)
    {
        _shaders.Add(id, shader);
    }
    
    public Shader Get(string name)
    {
        if (_shaders.TryGetValue(name, out var shader))
        {
            return shader;
        }
        
        throw new ArgumentException($"Shader \"{name}\" was not found");
    }
    
    public bool TryGet(string id, [MaybeNullWhen(false)] out Shader value)
    {
        return _shaders.TryGetValue(id, out value);
    }
}