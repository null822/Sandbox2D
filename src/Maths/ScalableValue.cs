using System;

namespace Sandbox2D.Maths;

public class ScalableValue
{
    private readonly double _minValue;
    private readonly double _maxValue;
    private readonly double _scalableValue;

    private readonly bool _useMin;
    private readonly bool _useMax;
    private readonly AxisBind _axisBind;

    private double _scale = 1;


    /// <summary>
    /// Creates a value that will scale depending on the size of the screen.
    /// </summary>
    /// <param name="scalableValue">The amount to scale the value by, depending on screen size {0-1, 0-1}</param>
    /// <param name="axisBind">The axis to bind to</param>
    /// <param name="minValue">The minimum value (optional)</param>
    /// <param name="maxValue">The maximum value (optional)</param>
    public ScalableValue(float scalableValue, AxisBind axisBind, float? minValue = null, float? maxValue = null, bool useMin = true, bool useMax = true)
    {
        _useMin = minValue != null && useMin;
        _useMax = maxValue != null && useMax;

        var minPixelsNonNull = minValue ?? 0;
        var maxPixelsNonNull = maxValue ?? 0;
        
        _minValue = minPixelsNonNull;
        _maxValue = maxPixelsNonNull;
        _scalableValue = scalableValue;
        _axisBind = axisBind;
    }

    /// <summary>
    /// Gets the value, with the current screen size.
    /// </summary>
    public double Get()
    {
        if (Math.Abs(_minValue - _maxValue) < 0.001 && _useMin && _useMax)
            return _minValue;
        
        var scaleValue = _axisBind switch
        {
            AxisBind.X => MainWindow.GetScreenSize().X,
            AxisBind.Y => MainWindow.GetScreenSize().Y,
            AxisBind.Average => (MainWindow.GetScreenSize().X + MainWindow.GetScreenSize().Y) / 2f,
            _ => 0f
        };
        
        
        var point = _scalableValue * scaleValue;

        if (_useMax)
            point = Math.Min(point, _maxValue);
        
        if (_useMin)
            point = Math.Max(point, _minValue);
        
        return point * _scale;
    }

    
    public static ScalableValue operator /(ScalableValue value, double scale)
    {
        value._scale /= scale;
        
        return value;
    }
    public static ScalableValue operator *(ScalableValue value, double scale)
    {
        value._scale *= scale;

        return value;
    }
    

    public static implicit operator double(ScalableValue value)
    {
        return value.Get();
    }
}

public enum AxisBind
{
    X,
    Y,
    Average
}
