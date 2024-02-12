using System;
using Sandbox2D.Maths;

namespace Sandbox2D.Exceptions;

[Serializable]
public class CoordOutOfBoundsException : Exception
{
    public CoordOutOfBoundsException()
    {
        
    }
    
    public CoordOutOfBoundsException(Vec2<long> coord, Range2D bound) : base($"Coordinate {coord} is outside QuadTree bounds of {bound}")
    {
        
    }    
}