using System;
using System.Collections.Generic;
using System.Threading;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Sandbox2D.Registry_.Registries;

namespace Sandbox2D.Registry_;

public static class GlRegistry
{
    public static ShaderRegistry Shader => Shaders[GetContext()];
    public static ShaderProgramRegistry ShaderProgram => ShaderPrograms[GetContext()];
    public static TextureRegistry Texture => Textures[GetContext()];
    
    private static readonly Dictionary<long, ShaderRegistry> Shaders = new();
    private static readonly Dictionary<long, ShaderProgramRegistry> ShaderPrograms = new();
    private static readonly Dictionary<long, TextureRegistry> Textures = new();
    
    public static void RegisterContext()
    {
        var context = GetContext();
        Shaders.Add(context, new ShaderRegistry());
        ShaderPrograms.Add(context, new ShaderProgramRegistry());
        Textures.Add(context, new TextureRegistry());
    }
    
    private static unsafe long GetContext()
    {
        return (long)GLFW.GetCurrentContext();
    }
}
