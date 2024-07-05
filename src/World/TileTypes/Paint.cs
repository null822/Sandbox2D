using System;
using System.Runtime.InteropServices;
using Sandbox2D.Graphics;

namespace Sandbox2D.World.TileTypes;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Paint : ITile
{
    public string Name => "Paint";
    public Tile Tile { get; } = new(1);
    
    public Paint(uint color)
    {
        Tile = new Tile(1, color);
    }

    public Color GetColor()
    {
        return new Color((uint)(Tile.Data & (~0x0uL >> (64 - 24))));
    }
    
    public override string ToString()
    {
        return $"{Name} [color=#{GetColor():X6}]";
    }
}
