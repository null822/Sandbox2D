using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using Sandbox2D.Graphics.Registry;
using Sandbox2D.Graphics.Renderables;
using Sandbox2D.Maths;

namespace Sandbox2D.Graphics;

public static class GameObjectRenderableManager
{
    private static GameObjectRenderable[] _renderables = [];
    private static bool[] _updated = [];
    private static readonly Dictionary<string, uint> RenderableNames = [];

    public static void AddQuad(uint id, Vec2<long> tl, Vec2<long> br)
    {
        if (_renderables.Length <= id)
            throw new Exception($"Renderable id {id} is not valid");
        
        ref var renderable = ref _renderables[id];
        
        // if the renderable has not been updated in this frame already, reset the geometry
        if (!_updated[id])
        {
            renderable.ResetGeometry();
        }
        
        renderable.AddQuad(tl, br);
        
        _renderables[id] = renderable;

        _updated[id] = true;
    }

    // resets the geometry of evert renderable
    public static void ResetGeometry()
    {
        for (var id = 0; id < _updated.Length; id++)
        {
            ref var renderable = ref _renderables[id];
            
            renderable.ResetGeometry();

            _updated[id] = true;
        }
    }

    public static void Render(Vec2<decimal> translation, float scale)
    {
        
        for (var id = 0; id < _updated.Length; id++)
        {
            ref var renderable = ref _renderables[id];
            
            renderable.SetTransform(translation, scale);
            
            // if the renderable was updated, upload the new geometry
            if (_updated[id])
            {
                renderable.UpdateVao();
            }
            
            // render
            renderable.Render();
            
            // reset updated flag
            _updated[id] = false;
        }

    }
    
    public static uint GetId(string name)
    {
        return RenderableNames.TryGetValue(name, out var value) ? value : NoId(name);
    }
    
    private static uint NoId(string name)
    {
        Util.Error($"Renderable \"{name}\" does not exist");
        return 0;
    }
    
    private static void Set(Dictionary<string, GameObjectRenderable> items)
    {
        var count = items.Count;
        
        _renderables = new GameObjectRenderable[count];
        _updated = new bool[count];
        RenderableNames.Clear();
        
        uint id = 0;
        foreach (var item in items)
        {
            if (RenderableNames.ContainsKey(item.Key))
            {
                Util.Error($"Renderable of the same name ({item.Key}) already exists");
                continue;
            }
            
            _renderables[id] = item.Value;
            RenderableNames.Add(item.Key, id);
            
            id++;
        }
    }
    
    public static void Instantiate()
    {
        Set(new Dictionary<string, GameObjectRenderable>
        {
            { "dirt", new GameObjectRenderable(Shaders.Dirt, BufferUsageHint.StreamDraw) },
            { "stone", new GameObjectRenderable(Shaders.Stone, BufferUsageHint.StreamDraw) }
        });
        
        Util.Log("Created GameObjectRenderables");
    }
}