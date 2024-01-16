using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using Sandbox2D.Graphics.Renderables;

namespace Sandbox2D.Graphics.Registry;

public static class Renderables
{
    private static Renderable[] _renderables = [];
    private static readonly Dictionary<string, uint> RenderableNames = [];
    
    public static uint GetId(string name)
    {
        return RenderableNames.TryGetValue(name, out var value) ? value : NoId(name);
    }

    private static uint NoId(string name)
    {
        Util.Error($"Renderable \"{name}\" does not exist");
        return 0;
    }

    private static void Set(Dictionary<string, Renderable> items)
    {
        uint i = 0;
        _renderables = new Renderable[items.Count];
        RenderableNames.Clear();
        
        foreach (var item in items)
        {
            if (RenderableNames.ContainsKey(item.Key))
            {
                Util.Error($"Renderable of the same name ({item.Key}) already exists");
                continue;
            }
            
            _renderables[i] = item.Value;
            RenderableNames.Add(item.Key, i);
            
            i++;
        }
    }
    
    public static ref Renderable Get(uint id)
    {
        if (_renderables.Length > id)
            return ref _renderables[id];
        
        Util.Error($"Id {id} is not valid");
        return ref _renderables[0];
    }
    
    public static void Set(uint id, Renderable renderable)
    {
        if (_renderables.Length > id)
        {
            _renderables[id] = renderable;
            return;
        }

        Util.Error($"Id {id} is not valid");
    }
    
    public static void Instantiate()
    {
        Set(new Dictionary<string, Renderable>
        {
            { "vertex_debug", new BaseRenderable(Shaders.VertexDebug, BufferUsageHint.StreamDraw) },
            { "noise", new BaseRenderable(Shaders.Noise, BufferUsageHint.StreamDraw) },
            { "go_vertex_debug", new GameObjectRenderable(Shaders.GoVertexDebug, BufferUsageHint.StreamDraw) },
            { "go_noise", new GameObjectRenderable(Shaders.GoNoise, BufferUsageHint.StreamDraw) },
        });
        
        Util.Log("Created Renderables");
    }
}