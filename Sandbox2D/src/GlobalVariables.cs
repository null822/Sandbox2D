using System;

namespace Sandbox2D;

public static class GlobalVariables
{
    /// <summary>
    /// The absolute path to the asset directory of <see cref="Sandbox2D"/>
    /// </summary>
    public static readonly string AssetDirectory = (AppDomain.CurrentDomain.BaseDirectory + "assets").Replace('\\', '/');
}
