#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;

namespace Sandbox2D.Maths;

/// <summary>
/// A dynamically-resizing array focused on performance.
/// <br></br><br></br>
/// Features: <br></br>
/// - Allocated memory gets split up into smaller (configurable) arrays, allowing for it to not get allocated into the LOH<br></br>
/// - Dynamic resizing<br></br>
/// - <see cref="GetModifications"/> (optional)<br></br>
/// - <see cref="Add"/>/<see cref="Set"/> elements<br></br>
/// - <see cref="Remove"/> elements<br></br>
/// - Elements are never copied/moved<br></br>
/// </summary>
/// <typeparam name="T">the type of the elements held within</typeparam>
public class DynamicArray<T> : IDisposable
{
    private readonly ArrayPool<T> _dataPool;
    private readonly List<T[]> _data = [];
    /// <summary>
    /// The length of each array in <see cref="_data"/>.
    /// </summary>
    private readonly int _arrayLength;
    
    private readonly DynamicArray<long> _vacancies = null!;
    
    /// <summary>
    /// Whether to store vacant spaces in this <see cref="DynamicArray{T}"/>. If set to false, <see cref="Remove"/>
    /// will throw <see cref="StoredVacanciesException"/>.
    /// </summary>
    private readonly bool _storeVacancies;
    
    /// <summary>
    /// Whether to store modifications to this <see cref="DynamicArray{T}"/>. If false, <see cref="GetModifications"/>
    /// will throw <see cref="StoredModificationsException"/>.
    /// </summary>
    private readonly bool _storeModifications;
    
    /// <summary>
    /// The modifications that have been done to this array since <see cref="GetModifications"/> was last called.
    /// </summary>
    private readonly DynamicArray<ArrayModification<T>> _modifications = null!;
    
    /// <summary>
    /// The index of the first element in this <see cref="DynamicArray{T}"/>. Will be non-zero if the first element(s) in
    /// this <see cref="DynamicArray{T}"/> are removed.
    /// </summary>
    private long _start;
    
    
    /// <summary>
    /// The number of elements in the array, including vacant spaces and spaces before <see cref="_start"/>.
    /// </summary>
    private long _totalLength;
    
    /// <summary>
    /// The number of elements in the array, including vacant spaces.
    /// </summary>
    public long Length => _totalLength - _start;
    
    /// <summary>
    /// Contains the same thing as <see cref="_totalLength"/>, except that any modifications that are still stored in this
    /// <see cref="DynamicArray{T}"/> are guaranteed to access only elements within this length.
    /// </summary>
    /// <remarks>Uninitialized if <see cref="_storeModifications"/> is false.</remarks>
    public long ModificationLength { private set; get; }
    
    /// <summary>
    /// The maximum number of elements that can fit into this <see cref="DynamicArray{T}"/> before more memory has to be allocated.
    /// </summary>
    private long AllocatedLength => (long)_data.Count * _arrayLength;
    
    /// <summary>
    /// Constructs a new <see cref="DynamicArray{T}"/>.
    /// </summary>
    /// <param name="arrayLength">the length of each internal array. Keep below sizeof(T)/85KB to prevent allocations
    /// to the LOH</param>
    /// <param name="storeModifications">whether to store modifications to the <see cref="DynamicArray{T}"/>. Causes a
    /// memory leak if <see cref="GetModifications"/> is never called after modifying the array</param>
    /// <param name="storeVacancies">whether to store vacant spaces in the <see cref="DynamicArray{T}"/>. If disabled,
    /// <see cref="Remove"/> will not work, and the <see cref="DynamicArray{T}"/> will be fully contiguous</param>
    public DynamicArray(int arrayLength = 2048, bool storeModifications = false, bool storeVacancies = true)
    {
        _storeModifications = storeModifications;
        _storeVacancies = storeVacancies;
        
        if (storeModifications) _modifications = new DynamicArray<ArrayModification<T>>(arrayLength, false, false);
        if (storeVacancies) _vacancies = new DynamicArray<long>(arrayLength, false, false);
        
        _arrayLength = arrayLength;
        _dataPool = ArrayPool<T>.Create(_arrayLength, 2048);
    }
    
    /// <summary>
    /// Constructs a new <see cref="DynamicArray{T}"/> and initializes it with values in an array.
    /// </summary>
    /// <remarks>See <see cref="DynamicArray{T}(int, bool, bool)"/> for information on parameters.</remarks>
    public DynamicArray(T[] data, int arrayLength = 2048, bool storeModifications = false, bool storeVacancies = true)
        : this(arrayLength, storeModifications, storeVacancies)
    {
        EnsureCapacity(data.Length);
        for (long i = 0; i < data.Length; i++)
        {
            this[i] = data[i];
        }
    }
    
    /// <summary>
    /// Gets or sets a value in the array.
    /// </summary>
    /// <param name="i">the index at which to get or set</param>
    public T this[long i]
    {
        get => Get(i + _start);
        set => Set(i + _start, value);
    }
    
    /// <summary>
    /// Retrieves a value.
    /// </summary>
    /// <param name="i">the index to retrieve the value at</param>
    /// <returns>the value retrieved</returns>
    private T Get(long i)
    {
        if (i < _start) throw new InvalidIndexException(i - _start, Length);
        if (i >= _totalLength) throw new InvalidIndexException(i - _start, Length);
        
        var value = GetData(i);
        
        return value;
    }
    
    /// <summary>
    /// Sets a value to a specific index in the array.
    /// </summary>
    /// <param name="i">the index of the value to set</param>
    /// <param name="value">the value to set</param>
    /// <remarks>When setting an index past <see cref="_totalLength"/>, the <see cref="DynamicArray{T}"/> will automatically
    /// grow to accomodate for the new value</remarks>
    private void Set(long i, T value)
    {
        if (i < _start) throw new InvalidIndexException(i - _start, Length);
        
        if (i >= _totalLength)
        {
            _totalLength = i + 1;
            Grow();
        }
        
        // set the value
        SetData(i, value);
        
        if (_storeModifications)
            _modifications.Add(new ArrayModification<T>(i, value));
    }
    
    /// <summary>
    /// Adds a value to the next available position.
    /// </summary>
    /// <param name="value">the value to add</param>
    /// <returns>the position the value was added at</returns>
    /// <remarks>Does not always add the value to the end of the array.</remarks>
    public long Add(T value)
    {
        var i = GetNextIndex();
        SetData(i, value);
        
        if (_storeModifications)
            _modifications.Add(new ArrayModification<T>(i, value));
        
        return i;
    }
    
    /// <summary>
    /// Copies elements from this <see cref="DynamicArray{T}"/> into another one.
    /// </summary>
    /// <param name="dest">the array to copy elements to</param>
    /// <param name="start">the index of the first element to copy, in this <see cref="DynamicArray{T}"/></param>
    /// <param name="destStart">the index of where, in <paramref name="dest"/>, to put the first element that is copied</param>
    /// <param name="length">the amount of elements to copy</param>
    public void CopyTo(ref readonly DynamicArray<T> dest, long start = 0, long destStart = 0, long length = -1)
    {
        if (length == -1) length = Length - start;
        
        for (long i = 0; i < length; i++)
        {
            dest[i + destStart] = this[i + start];
        }
    }
    
    /// <summary>
    /// Swaps two elements and updates <see cref="_modifications"/> if <see cref="_storeModifications"/> is enabled.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public void Swap(long a, long b)
    {
        (this[a], this[b]) = (this[b], this[a]);

        _modifications.Add(new ArrayModification<T>(a, this[a]));
        _modifications.Add(new ArrayModification<T>(b, this[b]));
    }
    
    /// <summary>
    /// Removes a value from the array
    /// </summary>
    /// <param name="i">the index of the value to remove</param>
    /// <param name="shrink">whether to try to shrink the <see cref="DynamicArray{T}"/></param>
    /// <remarks>This method only "soft-removes" the value, meaning it remains accessible,
    /// but will be overriden if new values are added. Consider using <see cref="Set"/> if this is not desired.
    /// Elements in the array are never shifted.</remarks>
    public void Remove(long i, bool shrink = true)
    {
        // account for _start
        i += _start;
        
        if (!_storeVacancies) throw new StoredVacanciesException();
        if (i > _totalLength) throw new IndexOutOfRangeException();
        
        _vacancies.Add(i);
        
        // if the last element in the array was removed, shrink the array
        if (shrink && i == _totalLength - 1) Shrink();
    }
    
    /// <summary>
    /// Removes all elements after and including the specified index, keeping only the ones before it.
    /// </summary>
    /// <param name="i">the index</param>
    public void RemoveEnd(long i)
    {
        // account for the array start
        i += _start;
        
        _totalLength = i;
        Shrink();
    }
    
    /// <summary>
    /// Removes all elements before and including the specified index, keeping only the ones after it.
    /// </summary>
    /// <param name="i">the index</param>
    public void RemoveStart(long i)
    {
        // account for the array start
        i += _start;
        
        _start = i + 1;
        
        if (Length <= 0) Clear();
    }
    
    /// <summary>
    /// Performs an in-place merge sort on every element in this <see cref="DynamicArray{T}"/>.
    /// </summary>
    /// <param name="comparison">[optional] the comparer for specifying a sort order. Will use the default comparer for
    /// <see cref="T"/> if none is set</param>
    public void Sort(Comparer<T>? comparison = null)
    {
        // create a (cloned) work array
        var workArr = new DynamicArray<T>(_arrayLength, false, false);
        CopyTo(in workArr);
        
        // sort the arrays. note that workArr is passed into array "a", and this is passed into array "b".
        // if they were the other way around, the sorted result would be in workArr, requiring another copy
        MergeSort(workArr, this, 0, Length, comparison ?? Comparer<T>.Default);
        
        // dispose the work array
        workArr.Dispose();
    }
    
    public bool Contains(Predicate<T> match)
    {
        return IndexOf(match) >= 0;
    }
    
    public long IndexOf(Predicate<T> match)
    {
        for (long i = 0; i < Length; i++)
        {
            if (match.Invoke(this[i])) return i;
        }

        return -1;
    }
    
    /// <summary>
    /// Internally grows the <see cref="DynamicArray{T}"/> to contain at least <paramref name="length"/> elements.
    /// </summary>
    /// <param name="length">the minimum length of the <see cref="DynamicArray{T}"/></param>
    /// <remarks>Useful when adding large amounts of elements to the <see cref="DynamicArray{T}"/> to prevent constant
    /// resizing. Does not make newly added elements accessible by indexing.</remarks>
    public void EnsureCapacity(long length)
    {
        if (length <= Length) return;
        
        Grow(length + _start);
    }
    
    /// <summary>
    /// Clears the <see cref="DynamicArray{T}"/>, resetting it back to its initial state.
    /// </summary>
    public void Clear()
    {
        // remove vacancies and any modifications
        if (_storeVacancies) _vacancies.Clear();
        if (_storeModifications)
        {
            _modifications.Clear();
            ModificationLength = 0;
        }
        
        // set the length and start to 0
        _totalLength = 0;
        _start = 0;
        
        
        // return as much memory as possible
        Shrink();
    }
    
    /// <summary>
    /// Retrieves the modifications that have been done to this <see cref="DynamicArray{T}"/> since the last time this
    /// method was called.
    /// </summary>
    /// <returns>A list of <see cref="ArrayModification{T}"/>s pointing to the element's raw (ignoring <see cref="_start"/>) index</returns>
    /// <remarks>See <see cref="ModificationLength"/></remarks>
    /// <exception cref="DynamicArray{T}.StoredModificationsException">thrown when modification storing is not enabled</exception>
    public ArrayModification<T>[] GetModifications()
    {
        if (!_storeModifications)
            throw new StoredModificationsException();
        
        // if there are no modifications, return none
        if (_modifications._totalLength == 0)
            return [];
        
        
        // copy part of `_modifications` into a new array
        var length = Math.Min(_modifications._arrayLength, _modifications.Length);
        var arr = new ArrayModification<T>[length];
        for (var i = 0; i < length; i++)
        {
            arr[i] = _modifications[i];
        }
        // remove the elements we copied
        _modifications.RemoveStart(length-1);
        
        // if there are no more modifications waiting to be picked up
        if (_modifications._totalLength == 0)
        {
            _modifications.Clear();
            ModificationLength = _totalLength;
        }
        
        return arr;
    }
    
    /// <summary>
    /// Gets the next available and empty index.
    /// </summary>
    private long GetNextIndex()
    {
        var i = _totalLength;
        
        if (_storeVacancies && _vacancies.Length > 0)
        {
            while (i >= _totalLength)
            {
                i = _vacancies[0] + _start;
                _vacancies.RemoveStart(0);
            }
        }
        else
        {
            i = _totalLength;
            _totalLength++;
            
            if (_totalLength > AllocatedLength) 
                Grow();
        }
        
        if (_storeModifications)
            ModificationLength = long.Max(ModificationLength, _totalLength);
        
        return i;
    }
    
    /// <summary>
    /// Allocates more arrays to <see cref="_data"/>, ensuring enough are allocated to cover every element within
    /// <see cref="_totalLength"/>.
    /// </summary>
    /// <remarks>The newly allocated memory is not guaranteed to be empty</remarks>
    private void Grow(long length = -1)
    {
        if (length == -1) length = _totalLength;
        
        for (var i = AllocatedLength; i < length; i+= _arrayLength)
        {
            _data.Add(_dataPool.Rent(_arrayLength));
        }
        
        if (_storeModifications)
            ModificationLength = long.Max(ModificationLength, _totalLength);
    }
    
    /// <summary>
    /// Returns as much data back to <see cref="_dataPool"/> as possible, and updates <see cref="_totalLength"/>.
    /// </summary>
    public void Shrink()
    {
        // if there are vacant spaces, check if we can shrink
        if (_storeVacancies && _vacancies.Length > 0 && _totalLength > 0)
        {
            // sort the vacancies in ascending order
            _vacancies.Sort();
            
            // check if all the vacancies form a continuous section of vacancy starting at the end of the list
            var prevVacancy = _totalLength;
            long i;
            for (i = _vacancies.Length - 1; i >= 0; i--)
            {
                var vacancy = _vacancies[i];
                
                // if this vacancy refers to an element outside the bounds of the array, continue
                if (vacancy >= _totalLength)
                    continue;
                
                // if this vacancy does not refer to the element 1 before or anywhere after the previously checked vacancy, exit
                if (vacancy < prevVacancy-1)
                    break;
                
                // go to the next vacancy
                prevVacancy = vacancy;
            }
            
            // if the DynamicArray can not be shrunk, exit
            if (i == _vacancies.Length - 1)
                return;
            
            // set `Length` to this element
            _totalLength = _vacancies[i+1];
            
            // if we shrunk the array completely, clear it and exit
            if (Length <= 0)
            {
                Clear();
                return;
            }
            
            // remove the vacancies we passed
            // note that vacancies are stored with raw (ignoring _start) indexes, hence the `- _start`
            _vacancies.RemoveEnd(i+1 - _start);
        }
        
        // return as many arrays as possible
        var div = long.DivRem(_totalLength, _arrayLength);
        var firstEmpty = (int)(div.Quotient + (div.Remainder > 0 ? 1 : 0));
        for (var i = firstEmpty; i < _data.Count; i++)
        {
            var arr = _data[i];
            _dataPool.Return(arr);
            Array.Clear(arr);
        }
        _data.RemoveRange(firstEmpty, _data.Count - firstEmpty);
    }
    
    /// <summary>
    /// Sorts an array using Merge Sort. Both arrays <see cref="a"/> and <see cref="b"/> must contain the data to be sorted.
    /// </summary>
    /// <param name="a">An array which will contain the sorted contents</param>
    /// <param name="b">A working array</param>
    /// <param name="start">the start index for sorting (inclusive)</param>
    /// <param name="end">the end index for sorting (exclusive)</param>
    /// <param name="comparison">the <see cref="Comparer{T}"/> to compare values</param>
    private static void MergeSort(DynamicArray<T> a, DynamicArray<T> b, long start, long end, Comparer<T> comparison)
    {
        // lists with less than 2 elements cannot be further sorted
        if (end - start < 2)
            return;
        
        // recursively swap and split the array in half, going down the stack
        var middle = (end + start) / 2;
        MergeSort(b, a, start, middle, comparison);
        MergeSort(b, a, middle, end, comparison);
        
        // sort and combine the elements into b, while going up the stack (hence, starting at arrays with length = 2)
        var l = start;
        var r = middle;
        
        // for every element in the section of array
        for (var i = start; i < end; i++)
        {
            // if the left value is <= the right value
            if (l < middle && (r >= end || comparison.Compare(a[l], a[r]) <= 0))
            {
                // copy the left value into the other array at this index, and move the next left index
                b[i] = a[l];
                l++;
            }
            else
            {
                // otherwise, copy the right value instead, and move the right index
                b[i] = a[r];
                r++;
            }
        }
    }
    
    
    /// <summary>
    /// Splits an index into two parts: an index into <see cref="_data"/> and an index into the array at that position
    /// in <see cref="_data"/>.
    /// </summary>
    /// <param name="i">the index</param>
    private (int a, int b) SplitIndex(long i)
    {
        var div = long.DivRem(i, _arrayLength);
        return ((int)div.Quotient, (int)div.Remainder);
    }
    
    /// <summary>
    /// Gets a single value from the <see cref="DynamicArray{T}"/>. The index ignores the <see cref="_start"/>, and is not sanitized.
    /// </summary>
    /// <param name="i">the index</param>
    /// <returns>the value</returns>
    private T GetData(long i)
    {
        var (a, b) = SplitIndex(i);
        return _data[a][b];
    }

    /// <summary>
    /// Sets a single value in the <see cref="DynamicArray{T}"/>. The index ignores the <see cref="_start"/>, and is not sanitized.
    /// </summary>
    /// <param name="i">the index</param>
    /// <param name="value">the value</param>
    private void SetData(long i, T value)
    {
        var (a, b) = SplitIndex(i);
        _data[a][b] = value;
    }
    
    /// <summary>
    /// Computes the hash of the contents of this <see cref="DynamicArray{T}"/>.
    /// </summary>
    /// <param name="start">the first element to factor into the hash</param>
    /// <param name="stop">the last element to factor into the hash</param>
    public Hash Hash(long start = 0, long stop = -1)
    {
        if (_totalLength == 0) return new Hash();
        
        if (stop < 0) stop = _totalLength-1;
        if (start < 0) start = 0;
        if (stop > _totalLength-1) stop = _totalLength-1;
        if (start > _totalLength-1 || start > stop) start = long.Min(_totalLength-1, stop);
        var hash = new Hash();
        for (var i = start; i <= stop; i++)
        {
            var h = new Hash(Get(i));
            // Console.WriteLine($"{hash:x16} <- {h:x16}");
            hash ^= h;
        }
        
        return hash;
    }
    
    public void Dispose()
    {
        if (_storeVacancies) _vacancies.Clear();
        if (_storeModifications) _modifications.Dispose();
        foreach (var arr in _data)
        {
            _dataPool.Return(arr);
            Array.Clear(arr);
        }
        _data.Clear();
        
        GC.SuppressFinalize(this);
    }
    
    private class StoredModificationsException() :
        Exception("Unable to retrieve modifications from QuadtreeList: Modification Storing is disabled");
    
    private class StoredVacanciesException() :
        Exception("Unable to remove element: Vacancies are not stored");
    
    private class InvalidIndexException(long i, long length) :
        Exception($"Index {i} out of range for {nameof(DynamicArray<T>)} of length {length}");
}

/// <summary>
/// Represents a single modification to an array or list, containing an index and a new value.
/// </summary>
/// <param name="index">the index</param>
/// <param name="value">the new value</param>
/// <typeparam name="T">the type of the new value</typeparam>
public readonly struct ArrayModification<T>(long index, T? value)
{
    public readonly long Index = index;
    public readonly T? Value = value;
    
    public override string ToString()
    {
        return $"[{Index}] => {Value}";
    }
}
