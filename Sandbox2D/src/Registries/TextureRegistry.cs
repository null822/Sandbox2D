using System;
using System.Collections.Generic;
using System.IO;
using Sandbox2D.Graphics;

namespace Sandbox2D.Registries;

public class TextureRegistry : IRegistry<Texture>
{
    private readonly Dictionary<string, Texture> _textures = new();
    
    public void RegisterAll(string path)
    {
        path = path.Replace('/', Path.DirectorySeparatorChar);
        if (path.EndsWith(Path.DirectorySeparatorChar)) path = path[..^1];
        foreach (var texturePath in Directory.GetFiles(path, "", SearchOption.AllDirectories))
        {
            var isTexture = Path.GetExtension(texturePath) switch
            {
                ".png" => true,
                _ => false,
            };
            
            if (!isTexture)
                continue;
            
            var name = texturePath
                .Replace($"{path}{Path.DirectorySeparatorChar}", "")
                .Replace(Path.GetExtension(texturePath), "");
            Register(name, new Texture(texturePath));
        }
    }
    
    public void Register(string id, Texture texture)
    {
        _textures.Add(id, texture);
    }
    
    public Texture Get(string name)
    {
        if (_textures.TryGetValue(name, out var texture))
        {
            return texture;
        }
        
        throw new ArgumentException($"Texture \"{name}\" was not found");
    }
    
    public bool TryGet(string id, out Texture value)
    {
        return _textures.TryGetValue(id, out value);
    }
}