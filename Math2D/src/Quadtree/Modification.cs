using System.Runtime.InteropServices;

namespace Math2D.Quadtree;

[StructLayout(LayoutKind.Sequential, Size = (8 * 4) + (4))]
public struct Modification(Range2D range, uint time)
{
    public Range2D Range = range;
    public uint Time = time;

    public override string ToString()
    {
        return $"Range={Range} Time={Time}";
    }
}