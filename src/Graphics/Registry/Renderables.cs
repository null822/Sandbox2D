using System;
using OpenTK.Graphics.OpenGL4;

namespace Sandbox2D.Graphics.Registry;

public static class Renderables
{
    public static Renderable Test { get; private set; }

    public static void Instantiate()
    {
        Test = new Renderable(Shaders.Test, BufferUsageHint.StreamDraw);
        Util.Log("Renderables Instantiated");
    }
}