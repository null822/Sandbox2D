using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Sandbox2D;

public class ConsumableList<T> : IList<T>
{
    public readonly List<T> List;
    
    public ConsumableList(List<T> list)
    {
        List = list;
    }
    
    public ConsumableList()
    {
        List = [];
    }
    
    public static implicit operator ConsumableList<T>(List<T> list)
    {
        return new ConsumableList<T>(list);
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


    public int Count => List.Count;
    public bool IsReadOnly => false;
    public IEnumerator<T> GetEnumerator() => List.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public void Add(T item) => List.Add(item);
    public void Clear() => List.Clear();
    public bool Contains(T item) => List.Contains(item);
    public void CopyTo(T[] array, int arrayIndex) => List.CopyTo(array, arrayIndex);
    public bool Remove(T item) => List.Remove(item);
    public int IndexOf(T item) => List.IndexOf(item);
    public void Insert(int index, T item) => List.Insert(index, item);
    public void RemoveAt(int index) => List.RemoveAt(index);
    public T this[int index]
    {
        get => List[index];
        set => List[index] = value;
    }
}