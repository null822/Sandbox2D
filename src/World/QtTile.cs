using System;
using Sandbox2D.Maths.Quadtree;

namespace Sandbox2D.World;

public class QtTile<T> : IQuadTreeValue<T> where T : IQtSerializable<T>
{
    private T _t1;
    private T _t2;
    
    
    private QtTile(T value)
    {
        _t1 = value;
        _t2 = value;
    }
    
    
    public static IQuadTreeValue<T> New(T value)
    {
        return new QtTile<T>(value);
    }
    
    private T Get()
    {
        return GameManager.WorldBuffer ? _t1 : _t2;
    }
    
    
    T IQuadTreeValue<T>.Get()
    {
        return Get();
    }
    
    void IQuadTreeValue<T>.Set(T value)
    {
        if (GameManager.WorldBuffer)
            _t1 = value;
        else
            _t2 = value;

    }

    public ReadOnlySpan<byte> Serialize()
    {
        return Get().Serialize();
    }


    public static T Deserialize(ReadOnlySpan<byte> bytes)
    {
        return T.Deserialize(bytes);
    }

    public static uint SerializeLength => sizeof(int);
    
    
    uint IQuadTreeValue<T>.LinearSerialize() => Get().LinearSerialize();
    
    bool IQuadTreeValue<T>.Equals(IQuadTreeValue<T> a)
    {
        var v1 = Get();
        var v2 = a.Get();
        
        // handle null
        if (v1 == null && v2 == null)
            return true;
        if (v1 == null || v2 == null)
            return false;
        
        return Get().Equals(a.Get());
    }
    
}