namespace Math2D;

public static class MathUtil
{
    /// <summary>
    /// Divides a left integer by a right integer, and returns the result, rounded up to the nearest integer.
    /// </summary>
    /// <param name="left">the left integer</param>
    /// <param name="right">the right integer</param>
    /// <returns>The rounded result</returns>
    public static long DivCeil(long left, long right)
    {
        var d = long.DivRem(left, right);
        return d.Quotient + (d.Remainder == 0 ? 0 : 1);
    }
}
