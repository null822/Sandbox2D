using System.Collections.Generic;
using Sandbox2D.Graphics.Registry;
using Sandbox2D.Gui.GuiElements;
using Sandbox2D.Maths;

namespace Sandbox2D.GUI;

public static class GuiManager
{
    private static IGui[] _guis;
    private static readonly Dictionary<string, uint> GuiNames = [];
    
    /// <summary>
    /// Runs the MouseOver trigger for each GUI.
    /// </summary>
    /// <param name="mousePos">the position of the mouse, in screen coords where (0, 0) is in the center of the screen</param>
    public static void MouseOver(Vec2<int> mousePos)
    {
        foreach (var gui in _guis)
        {
            gui.MouseOver(mousePos);
        }
    }

    /// <summary>
    /// Runs the MouseOver trigger for a specified GUI.
    /// </summary>
    /// <param name="mousePos">the position of the mouse, in screen coords where (0, 0) is in the center of the screen</param>
    /// <param name="id">the ID of the GUI to trigger</param>
    public static void MouseOver(Vec2<int> mousePos, uint id)
    {
        if (!CheckId(id))
            return;
        
        _guis[id].MouseOver(mousePos);
    }
    
    /// <summary>
    /// Sets the visibility of the specified GUI
    /// </summary>
    /// <param name="id">the ID of the GUI to set the visibility of</param>
    /// <param name="enabled">the visibility. true = visible / false = invisible</param>
    public static void SetVisibility(uint id, bool enabled)
    {
        if (!CheckId(id))
            return;
        
        
        _guis[id].SetVisibility(enabled);
    }
    
    /// <summary>
    /// Returns the visibility of the specified GUI
    /// </summary>
    /// <param name="id">the ID of the GUI to return the visibility of</param>
    public static bool GetVisibility(uint id)
    {
        if (!CheckId(id))
            return false;
        
        
        return _guis[id].GetVisibility();
    }
    
    /// <summary>
    /// Instantiates the GUIs
    /// </summary>
    /// <param name="items"></param>
    private static void Set(Dictionary<string, IGui> items)
    {
        var count = items.Count;
        
        _guis = new IGui[count];
        GuiNames.Clear();
        
        uint id = 0;
        foreach (var (name, gui) in items)
        {
            if (GuiNames.ContainsKey(name))
            {
                Util.Error($"GUI of the same name ({name}) already exists");
                continue;
            }

            gui.CreateGeometry();
            _guis[id] = gui;
            
            GuiNames.Add(name, id);
            
            id++;
        }
    }
    
    /// <summary>
    /// Gets the ID of the GUI with the supplied name
    /// </summary>
    /// <param name="name">the name of the GUI</param>
    public static uint GetId(string name)
    {
        return GuiNames.TryGetValue(name, out var id) ? id : NoId(name);
    }
    
    private static uint NoId(string name)
    {
        Util.Error($"Gui of name \"{name}\" does not exist");
        return 0;
    }
    
    /// <summary>
    /// Re-creates the geometry of all GUIs, updating them
    /// </summary>
    public static void UpdateGuis()
    {
        // reset all GUI renderables' geometry
        Renderables.ResetGeometry(RenderableCategory.Gui);
        
        // re-create the geometry of all GUIs
        foreach (var gui in _guis)
        {
            gui.CreateGeometry();
        }
        
        // update all GUI renderables' VAO
        Renderables.UpdateVao(RenderableCategory.Gui);
    }
    
    public static void Instantiate()
    {
        Set(new Dictionary<string, IGui>
        {
            { "test", new TestGui([
                    new GuiCheckbox()
                ])
            }
        });
        
        // create the GUIs' geometry
        UpdateGuis();
        
        Util.Log("Created GUIs");
    }

    private static bool CheckId(uint id)
    {
        if (id < _guis.Length)
            return true;
        
        Util.Error($"GUI with the id {id} does not exist");
        return false;

    }
    
}