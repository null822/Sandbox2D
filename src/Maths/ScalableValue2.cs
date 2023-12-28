using Microsoft.Xna.Framework;

namespace Sandbox2D.Maths;

public class ScalableValue2
{
    private ScalableValue _x;
    private ScalableValue _y;


    public ScalableValue2(Vector2 scalablePoint,
        Vector2? minValue = null, Vector2? maxValue = null,
        AxisBind bindX = AxisBind.X, AxisBind bindY = AxisBind.Y)
    {
        
        var useMin = minValue != null;
        var useMax = maxValue != null;

        float? minValueX = useMin ? minValue.Value.X : null;
        float? minValueY = useMin ? minValue.Value.Y : null;
        
        float? maxValueX = useMax ? maxValue.Value.X : null;
        float? maxValueY = useMax ? maxValue.Value.Y : null;

        
        _x = new ScalableValue(scalablePoint.X, bindX, minValueX, maxValueX);
        _y = new ScalableValue(scalablePoint.Y, bindY, minValueY, maxValueY);
    }
    
    public ScalableValue2(ScalableValue x, ScalableValue y)
    {
        _x = x;
        _y = y;
    }
    
    public static ScalableValue2 operator *(ScalableValue2 value1, ScalableValue2 value2)
    {
        value1._x *= value2._x;
        value1._y *= value2._y;
        return value1;
    }
    public static ScalableValue2 operator /(ScalableValue2 value1, ScalableValue2 value2)
    {
        value1._x /= value2._x;
        value1._y /= value2._y;
        return value1;
    }
    
    public double GetY()
    {
        return _y;
    }
    
    public double GetX()
    {
        return _x;
    }

    public Vector2 Get()
    {
        return new Vector2((float)_x, (float)_y);
    }
    
    public static implicit operator Vector2(ScalableValue2 value)
    {
        return value.Get();
    }
    
    public static explicit operator ScalableValue2(Vector2 value)
    {
        return new ScalableValue2(new Vector2(1, 1), value, value);
    }
    
}