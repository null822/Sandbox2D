using static Sandbox2D.Constants;
namespace Sandbox2D;

public static class DerivedConstants
{
    public const decimal QuadTreeSvgScale = (decimal)QuadTreeSvgWidth / ~(WorldHeight == 64 ? 0 : ~0x0uL << WorldHeight);
}
