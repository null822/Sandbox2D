namespace Sandbox2D.Maths;

public static class MathUtil
{
    public static long DivCeil(long left, long right)
    {
        var d = long.DivRem(left, right);
        return d.Quotient + (d.Remainder == 0 ? 0 : 1);
    }
}