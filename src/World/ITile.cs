using System;
using Sandbox2D.Graphics;

namespace Sandbox2D.World;

public interface ITile
{
    public static Texture Texture { get; private set; }
    public static string Name => "unnamed";

}
