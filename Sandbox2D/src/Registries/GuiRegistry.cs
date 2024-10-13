using System;
using System.Collections.Generic;
using System.IO;
using Sandbox2D.Graphics;
using Sandbox2D.UserInterface;

namespace Sandbox2D.Registries;

public class GuiRegistry : IRegistry<Gui>
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
            var isGui = Path.GetExtension(xmlPath) switch
            {
                ".gui" => true,
                _ => false,
            };
            
            if (!isGui)
                continue;
            
            var name = xmlPath
                .Replace($"{path}{Path.DirectorySeparatorChar}", "")
                .Replace(Path.GetExtension(xmlPath), "");
            Register(name, new Gui(xmlPath));
        }
    }
    
    public void Register(string id, Gui shader)
    {
        _guiNames.Add(id, _guiList.Count);
        _guiList.Add(shader);
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
        if (_guiNames.TryGetValue(id, out var index))
        {
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
    
    /// <summary>
    /// Sets an attribute of a <see cref="Gui"/>.
    /// </summary>
    /// <param name="id">the id of the GUI</param>
    /// <param name="elementId">the id of the IGuiElement on which the attribute will be set</param>
    /// <param name="attributeName">the name of the attribute</param>
    /// <param name="value">the new value</param>
    public void SetAttribute(string id, string elementId, string attributeName, string value)
    {
        Get(id).SetAttribute(elementId, attributeName, value);
    }
    
    /// <summary>
    /// Gets the value of an attribute of a <see cref="Gui"/>.
    /// </summary>
    /// <param name="id">the id of the GUI</param>
    /// <param name="elementId">the id of the IGuiElement containing the attribute to get</param>
    /// <param name="attributeName">the name of the attribute to get</param>
    /// <returns>the value of the attribute, or an empty string if it was not found</returns>
    public string GetAttribute(string id, string elementId, string attributeName)
    {
        return Get(id).GetAttribute(elementId, attributeName);
    }
}
