using static Sandbox2D.Constants;
namespace Sandbox2D;

public static class DerivedConstants
{
    public static decimal QuadTreeSvgScale => (decimal)QuadTreeSvgSize / ~(GameManager.WorldHeight == 64 ? 0 : ~0x0uL << GameManager.WorldHeight);
}
