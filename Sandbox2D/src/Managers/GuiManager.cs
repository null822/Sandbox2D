using System;
using System.Collections.Generic;
using System.IO;
using Sandbox2D.Registry_.Registries;
using Sandbox2D.UserInterface;

namespace Sandbox2D.Managers;

public class GuiManager : IRegistry<Gui>
{
    private readonly List<Gui> _guiList = [];
    private readonly Dictionary<string, int> _guiNames = new();
    private readonly List<int> _visibleGuis = [];
    
    public void RegisterAll(string path)
    {
        path = path.Replace('/', Path.DirectorySeparatorChar);
        if (path.EndsWith('/')) path = path[..^1];
        foreach (var xmlPathS in Directory.GetFiles(path, "", SearchOption.AllDirectories))
        {
            var xmlPath = xmlPathS.Replace(Path.DirectorySeparatorChar, '/');
            var extension = Path.GetExtension(xmlPath);
            var isGui = extension switch
            {
                ".gui" => true,
                _ => false,
            };
            
            if (!isGui)
                continue;
            
            var name = Path.GetFileName(xmlPath).Replace(extension, "");
            Register(name, new Gui(xmlPath));
        }
    }
    
    public void Register(string id, Gui gui)
    {
        _guiNames.Add(id, _guiList.Count);
        _guiList.Add(gui);
    }
    
    public Gui Get(string name)
    {
        if (_guiNames.TryGetValue(name, out var index))
        {
            return _guiList[index];
        }
        
        throw new ArgumentException($"GUI \"{name}\" was not found");
    }
    
    public bool TryGet(string id, out Gui value)
    {
        if (_guiNames.TryGetValue(id, out var index))
        {
            value = _guiList[index];
            return true;
        }
        
        value = null;
        return false;
    }
    
    /// <summary>
    /// Renders a <see cref="Gui"/>
    /// </summary>
    /// <param name="id">the id of the <see cref="Gui"/> to render</param>
    public void Render(string id)
    {
        Get(id).Render();
    }
    
    /// <summary>
    /// Renders all visible <see cref="Gui"/>s
    /// </summary>
    public void RenderVisible()
    {
        foreach (var guiIndex in _visibleGuis)
        {
            _guiList[guiIndex].Render();
        }
    }
    
    /// <summary>
    /// Updates all visible <see cref="Gui"/>s.
    /// </summary>
    public void UpdateVisible()
    {
        foreach (var guiIndex in _visibleGuis)
        {
            _guiList[guiIndex].Update();
        }
    }
    
    /// <summary>
    /// Sets whether a <see cref="Gui"/> is visible or not.
    /// </summary>
    /// <param name="id">the id of the <see cref="Gui"/></param>
    /// <param name="visible">a <see cref="bool"/> stating whether the <see cref="Gui"/> is visible or not</param>
    public void SetVisibility(string id, bool visible)
    {
        var index = _guiNames[id];
        
        var currentIndex = _visibleGuis.IndexOf(index);
        var currentIsVisible = currentIndex != -1;
        
        // exit if the GUI is already in the correct state
        if (currentIsVisible == visible) return;
            
        if (visible)
        {
            _visibleGuis.Add(index);
        }
        else
        {
            _visibleGuis.RemoveAt(currentIndex);
        }
    }
}
