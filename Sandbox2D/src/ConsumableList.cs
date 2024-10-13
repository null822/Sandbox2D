using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Sandbox2D;

public class ConsumableList<T>
{
    public readonly List<T> List;
    
    public ConsumableList(List<T> list)
    {
        List = list;
    }
    
    public bool Consume(Predicate<T> predicate, [MaybeNullWhen(false)] out T value)
    {
        var i = List.FindIndex(predicate);
        if (i == -1)
        {
            value = default;
            return false;
        }
        
        value = List[i];
        List.RemoveAt(i);
        return true;
    }
    
    public T[] ConsumeAll(Predicate<T> predicate)
    {
        var matches = new List<T>(List.Count);
        for (var i = 0; i < List.Count; i++)
        {
            var v = List[i];
            if (!predicate.Invoke(v)) continue;
            
            matches.Add(v);
            List.RemoveAt(i);
        }
        
        return matches.ToArray();
    }
    
    public T Peek(Predicate<T> predicate)
    {
        return List.Find(predicate);
    }
    
    public T[] PeekAll(Predicate<T> predicate)
    {
        return List.FindAll(predicate).ToArray();
    }
}