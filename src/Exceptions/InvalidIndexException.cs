using System;
using Sandbox2D.Maths;

namespace Sandbox2D.Exceptions;

[Serializable]
public class InvalidIndexException : Exception
{
    public InvalidIndexException()
    {
        
    }
    
    public InvalidIndexException(Vec2<byte> index2D) : base($"Invalid Index {index2D}")
    {
        
    }    
}