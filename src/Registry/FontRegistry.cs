using System.Collections.Generic;
using MonoGame.Extended.BitmapFonts;

namespace ElectroSim.Content;

/// <summary>
/// Stores all fonts for access anywhere in the program.
/// </summary>
public static class FontRegistry
{
    /// <summary>
    /// A Dictionary containing all fonts (name:font).
    /// </summary>
    private static readonly Dictionary<string, BitmapFont> FontDictionary = new();

    /// <summary>
    /// Register a font, with a name.
    /// </summary>
    /// <param name="name">The name of the font</param>
    /// <param name="font">The font</param>
    public static void RegisterFont(string name, BitmapFont font)
    {
        FontDictionary.Add(name, font);
    }
    
    /// <summary>
    /// Get a font, by name. Returns a missing font none was not found.
    /// </summary>
    /// <param name="name">The name of the font to return</param>
    /// <returns></returns>
    public static BitmapFont GetFont(string name)
    {
        var font = FontDictionary.TryGetValue(name, out var font1) ? font1 : FontDictionary["missing"];
        return font;
    }

}