using System.Collections.Generic;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Sandbox2D.Managers;

namespace Sandbox2D.Registry_;

public static class GlContext
{
    private static readonly Dictionary<long, RenderManager> RenderManagers = new();
    public static RenderManager RenderManager
    {
        get => RenderManagers[GetContext()];
        set => RenderManagers.Set(GetContext(), value);
    }
    
    private static readonly Dictionary<long, GlRegistry> Registries = new();
    public static GlRegistry Registry
    {
        get => Registries[GetContext()];
        set => Registries.Set(GetContext(), value);
    }

    public static void RegisterContext()
    {
        Registry = new GlRegistry();
    }
    
    private static unsafe long GetContext()
    {
        return (long)GLFW.GetCurrentContext();
    }
    
    /// <summary>
    /// Adds or overrides an entry in a <see cref="Dictionary{TKey,TValue}"/>
    /// </summary>
    private static void Set<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (!dictionary.TryAdd(key, value))
            dictionary[key] = value;
    }
}
