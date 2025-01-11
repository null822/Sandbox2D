using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using Math2D.Binary;

namespace Math2D;

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
    
    /// <summary>
    /// A <see cref="DynamicArray{T}"/> of <see cref="ulong"/>s, used as a bitset to store the usage state of each
    /// value in this <see cref="DynamicArray{T}"/>. See <see cref="IsOccupied"/> and <see cref="SetOccupied"/>.
    /// <code>
    /// 0 = vacant
    /// 1 = used
    /// </code>
    /// </summary>
    private readonly DynamicArray<ulong> _occupiedIndexes = null!;
    
    /// <summary>
    /// Whether to store vacant spaces in this <see cref="DynamicArray{T}"/>. If set to false, <see cref="Remove"/>
    /// will throw <see cref="StoredVacanciesException"/>.
    /// </summary>
    public readonly bool StoreOccupied;
    
    /// <summary>
    /// Whether to store modifications to this <see cref="DynamicArray{T}"/>. If false, <see cref="GetModifications"/>
    /// will throw <see cref="StoredModificationsException"/>.
    /// </summary>
    public readonly bool StoreModifications;
    
    /// <summary>
    /// The modifications that have been done to this array since <see cref="GetModifications"/> was last called.
    /// </summary>
    private readonly DynamicArray<ArrayModification<T>> _modifications = null!;
    /// <summary>
    /// The number of elements in the array, including vacant spaces.
    /// </summary>
    public long Length { get; private set; }
    
    /// <summary>
    /// Contains the same thing as <see cref="Length"/>, except that any modifications that are still stored in this
    /// <see cref="DynamicArray{T}"/> are guaranteed to access only elements within this length.
    /// </summary>
    public long ModificationLength { private set; get; }
    
    /// <summary>
    /// The maximum number of elements that can fit into this <see cref="DynamicArray{T}"/> before more memory has to be allocated.
    /// </summary>
    private long AllocatedLength => (long)_data.Count * _arrayLength;

    #region Constructors / Indexers
    
    /// <summary>
    /// Constructs a new <see cref="DynamicArray{T}"/>.
    /// </summary>
    /// <param name="arrayLength">the length of each internal array. Keep below sizeof(T)/85KB to prevent allocations
    /// to the LOH</param>
    /// <param name="storeModifications">whether to store modifications to the <see cref="DynamicArray{T}"/>. Causes a
    /// memory leak if <see cref="GetModifications"/> is never called after modifying the array</param>
    /// <param name="storeOccupied">whether to store vacant spaces in the <see cref="DynamicArray{T}"/>. If disabled,
    /// <see cref="Remove"/> will not work, and the <see cref="DynamicArray{T}"/> will be fully contiguous</param>
    public DynamicArray(int arrayLength = 2048, bool storeModifications = false, bool storeOccupied = true)
    {
        StoreModifications = storeModifications;
        StoreOccupied = storeOccupied;
        
        if (storeModifications) _modifications = new DynamicArray<ArrayModification<T>>(arrayLength, false, false);
        if (storeOccupied) _occupiedIndexes = new DynamicArray<ulong>((int)MathUtil.DivCeil(arrayLength, 64), false, false);
        
        _arrayLength = arrayLength;
        _dataPool = ArrayPool<T>.Create(_arrayLength, 2048);
    }
    
    /// <summary>
    /// Constructs a new <see cref="DynamicArray{T}"/> and initializes it with values in an array.
    /// </summary>
    /// <remarks>See <see cref="DynamicArray{T}(int, bool, bool)"/> for information on parameters.</remarks>
    public DynamicArray(T[] data, int arrayLength = 2048, bool storeModifications = false, bool storeOccupied = true)
        : this(arrayLength, storeModifications, storeOccupied)
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
        get => Get(i);
        set => Set(i, value);
    }
    
    #endregion
    
    #region Direct Element Modification

    /// <summary>
    /// Retrieves a value.
    /// </summary>
    /// <param name="i">the index to retrieve the value at</param>
    /// <returns>the value retrieved</returns>
    private T Get(long i)
    {
        if (i < 0 || i >= Length) throw new InvalidIndexException(i, Length);
        if (StoreOccupied && !IsOccupied(i)) throw new DeletedElementException(i);
        
        
        var value = GetData(i);
        
        return value;
    }
    

    /// <summary>
    /// Sets a value to a specific index in the array.
    /// </summary>
    /// <param name="i">the index of the value to set</param>
    /// <param name="value">the value to set</param>
    /// <remarks>When setting an index past <see cref="Length"/>, the <see cref="DynamicArray{T}"/> will automatically
    /// grow to accomodate for the new value</remarks>
    private void Set(long i, T value)
    {
        if (i < 0) throw new InvalidIndexException(i, Length);
        
        if (i >= Length)
        {
            Length = i + 1;
            Grow();
        }
        
        // set the value
        SetData(i, value);
        
        // ensure the element is no longer marked as vacant
        if (StoreOccupied) SetOccupied(i, true);
        
        if (StoreModifications)
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
        
        if (StoreModifications)
            _modifications.Add(new ArrayModification<T>(i, value));
        
        return i;
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
        if (!StoreOccupied) throw new StoredVacanciesException();
        if (i < 0 || i >= Length) throw new InvalidIndexException(i, Length);
        
        if (StoreOccupied) SetOccupied(i, false);
        
        // if the last element in the array was removed, shrink the array
        if (shrink && i == Length - 1) Shrink();
    }
    
    
    /// <summary>
    /// Swaps two elements and updates <see cref="_modifications"/> if <see cref="StoreModifications"/> is enabled.
    /// </summary>
    /// <param name="a">the index of the first element to swap</param>
    /// <param name="b">the index of the second element to swap</param>
    public void Swap(long a, long b)
    {
        (this[a], this[b]) = (this[b], this[a]);

        _modifications.Add(new ArrayModification<T>(a, this[a]));
        _modifications.Add(new ArrayModification<T>(b, this[b]));
    }
    
    #endregion
    
    #region Multiple Element Modification
    
    /// <summary>
    /// Removes all elements after and including the specified index, keeping only the ones before it.
    /// </summary>
    /// <param name="i">the index</param>
    public void RemoveEnd(long i)
    {
        Length = i;
        Shrink();
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
        CopyTo(workArr);
        
        // sort the arrays. note that workArr is passed into array "a", and this is passed into array "b".
        // if they were the other way around, the sorted result would be in workArr, requiring another copy
        MergeSort(workArr, this, 0, Length, comparison ?? Comparer<T>.Default);
        
        // dispose the work array
        workArr.Dispose();
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
    /// Clears the <see cref="DynamicArray{T}"/>, resetting it back to its initial state.
    /// </summary>
    public void Clear()
    {
        // remove vacancies and any modifications
        if (StoreOccupied) _occupiedIndexes.Clear();
        if (StoreModifications)
        {
            _modifications.Clear();
            ModificationLength = 0; // update modification length
        }
        
        // set the length to 0
        Length = 0;
        
        // delete all data chunks
        foreach (var arr in _data)
        {
            _dataPool.Return(arr, true);
            Array.Clear(arr);
        }
        _data.RemoveRange(0, _data.Count);
    }

    #endregion
    
    #region Data Extraction
    
    /// <summary>
    /// Copies elements from this <see cref="DynamicArray{T}"/> into another one.
    /// </summary>
    /// <param name="dest">the array to copy elements to</param>
    /// <param name="start">the index of the first element to copy, in this <see cref="DynamicArray{T}"/></param>
    /// <param name="destStart">the index of where, in <paramref name="dest"/>, to put the first element that is copied</param>
    /// <param name="length">the amount of elements to copy</param>
    public void CopyTo(DynamicArray<T> dest, long start = 0, long destStart = 0, long length = -1)
    {
        if (length == -1) length = Length - start;
        
        for (long i = 0; i < length; i++)
        {
            dest[i + destStart] = this[i + start];
        }
    }
    
    
    /// <summary>
    /// Copies elements from this <see cref="DynamicArray{T}"/> into an array.
    /// </summary>
    /// <param name="dest">the array to copy elements to</param>
    /// <param name="start">the index of the first element to copy, in this <see cref="DynamicArray{T}"/></param>
    /// <param name="destStart">the index of where, in <paramref name="dest"/>, to put the first element that is copied</param>
    /// <param name="length">the amount of elements to copy</param>
    public void CopyTo(T[] dest, long start = 0, long destStart = 0, long length = -1)
    {
        if (length == -1) length = Length - start;
        
        for (long i = 0; i < length; i++)
        {
            dest[i + destStart] = this[i + start];
        }
    }
    
    /// <summary>
    /// Retrieves every modification that has been done to this <see cref="DynamicArray{T}"/> since the last time
    /// <see cref="ClearModifications"/> was called, and copies them into <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">the buffer into which to copy the modifications</param>
    /// <returns>The amount of modifications that were copied into <paramref name="destination"/></returns>
    /// <remarks>See <see cref="ModificationLength"/></remarks>
    /// <exception cref="DynamicArray{T}.StoredModificationsException">thrown when modification storing is not enabled</exception>
    public long GetModifications(DynamicArray<ArrayModification<T>> destination)
    {
        if (!StoreModifications)
            throw new StoredModificationsException();

        var count = _modifications.Length;
        if (count == 0) return 0;
        
        _modifications.CopyTo(destination);
        
        return count;
    }
    
    /// <summary>
    /// Resets the internally stored modifications.
    /// </summary>
    /// <exception cref="DynamicArray{T}.StoredModificationsException">thrown when modification storing is not enabled</exception>
    public void ClearModifications()
    {
        if (!StoreModifications)
            throw new StoredModificationsException();
        
        _modifications.Clear();
        ModificationLength = Length;
    }
    
    #endregion
    
    #region Searching
    
    /// <summary>
    /// Checks whether the given <see cref="Predicate{T}"/> has any matches in this <see cref="DynamicArray{T}"/>.
    /// </summary>
    /// <param name="match">the <see cref="Predicate{T}"/> to match</param>
    public bool Contains(Predicate<T> match)
    {
        return IndexOf(match) >= 0;
    }
    
    /// <summary>
    /// Finds the first index the given predicate matches, or returns -1 if none match.
    /// </summary>
    /// <param name="match"></param>
    /// <remarks>Skips vacant spaces.</remarks>
    public long IndexOf(Predicate<T> match)
    {
        for (long i = 0; i < Length; i++)
        {
            if (StoreOccupied && IsOccupied(i)) continue; // skip vacant spaces
            if (match.Invoke(this[i])) return i;
        }

        return -1;
    }
    
    #endregion
    
    #region Resizing
    
    /// <summary>
    /// Internally grows the <see cref="DynamicArray{T}"/> to contain at least <paramref name="length"/> elements.
    /// </summary>
    /// <param name="length">the minimum length of the <see cref="DynamicArray{T}"/></param>
    /// <remarks>Useful when adding large amounts of elements to the <see cref="DynamicArray{T}"/> to prevent constant
    /// resizing. Does not make newly added elements accessible by indexing.</remarks>
    public void EnsureCapacity(long length)
    {
        if (length <= Length) return;
        
        Grow(length);
    }
    
    /// <summary>
    /// Allocates more arrays to <see cref="_data"/>, ensuring enough are allocated to cover every element within
    /// <see cref="Length"/>.
    /// </summary>
    /// <param name="length">the length of the allocated space in this <see cref="DynamicArray{T}"/>. Set to -1 to grow
    /// up to the <see cref="Length"/></param>
    /// <remarks>The newly allocated memory is not guaranteed to be empty.</remarks>
    private void Grow(long length = -1)
    {
        if (length == -1) length = Length;
        
        for (var i = AllocatedLength; i < length; i+= _arrayLength)
        {
            _data.Add(_dataPool.Rent(_arrayLength));
        }
        
        if (StoreModifications)
            ModificationLength = long.Max(ModificationLength, Length);
        
        if (StoreOccupied)
        {
            var origSize = _occupiedIndexes.Length;
            var newSize = MathUtil.DivCeil(length, 64);
            _occupiedIndexes.Grow(newSize);
            _occupiedIndexes.Length = newSize;
            
            // clear newly allocated memory
            for (var i = origSize; i < _occupiedIndexes.Length; i++)
            {
                _occupiedIndexes[i] = 0uL;
            }
        }
    }
    
    /// <summary>
    /// Returns as much data back to <see cref="_dataPool"/> as possible, and updates <see cref="Length"/>.
    /// </summary>
    public void Shrink()
    {
        // if there are vacant spaces, check if we can shrink
        if (StoreOccupied && _occupiedIndexes.Length > 0 && Length > 0)
        {
            // loop through all the chunks in _usedIndexes, starting at [0] (to fill vacant spaces at the start of the array first)
            var chunkIndex = _occupiedIndexes.Length - 1;
            long lastUsedIndex = 0;
            while (chunkIndex > 0)
            {
                var chunk = _occupiedIndexes[chunkIndex];
                // if the chunk is completely empty (no used spaces) go to the chunk before it
                if (chunk == 0)
                {
                    chunkIndex--;
                    continue;
                }
                
                // otherwise, if there is a used space, calculate its index and break out of the loop
                lastUsedIndex = chunkIndex * 64 + (64 - BitUtil.LeadingZeros(chunk) - 1);
                break;
            }
            
            // if the DynamicArray can not be shrunk, exit
            if (lastUsedIndex == _occupiedIndexes.Length - 1)
                return;
            
            // set `Length` to the last used element
            Length = lastUsedIndex + 1;
            
            // if we shrunk the array completely, clear it and exit
            if (Length <= 0)
            {
                Clear();
                return;
            }
        }
        
        // return as many arrays as possible
        var firstEmpty = (int)MathUtil.DivCeil(Length, _arrayLength);
        for (var i = firstEmpty; i < _data.Count; i++)
        {
            var arr = _data[i];
            _dataPool.Return(arr);
            Array.Clear(arr);
        }
        _data.RemoveRange(firstEmpty, _data.Count - firstEmpty);
    }
    
    #endregion
    
    #region Raw / Unsanitized Get/Set
    
    /// <summary>
    /// Gets the next available and empty index.
    /// </summary>
    private long GetNextIndex()
    {
        // try to find the first vacant space
        if (StoreOccupied)
        {
            // loop through all the chunks in _usedIndexes, starting at [0] (to fill vacant spaces at the start of the array first)
            var chunkIndex = 0;
            while (chunkIndex < _occupiedIndexes.Length)
            {
                var chunk = _occupiedIndexes[chunkIndex];
                // if the chunk is fully populated (no free spaces) go to the next chunk
                if (chunk == ulong.MaxValue)
                {
                    chunkIndex++;
                    continue;
                }
                
                // otherwise, if there is a free space, return the index of that space
                var i = chunkIndex * 64 + BitUtil.TrailingZeros(~chunk);
                if (i > Length - 1) break; // if the index is outside the DynamicArray, exit the loop
                SetOccupied(i, true); // set the space to the used state
                return i;
            }
        }
        
        // if there are no vacant spaces, grow the DynamicArray by 1 and return the last index
        Length++;
        Grow();
        
        if (StoreModifications)
            ModificationLength = long.Max(ModificationLength, Length);
        
        var index = Length - 1;
        if (StoreOccupied) SetOccupied(index, true);
        return index;
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
    /// Gets a single value from the <see cref="DynamicArray{T}"/>. The index is not sanitized.
    /// </summary>
    /// <param name="i">the index</param>
    /// <returns>the value</returns>
    private T GetData(long i)
    {
        var (a, b) = SplitIndex(i);
        return _data[a][b];
    }

    /// <summary>
    /// Sets a single value in the <see cref="DynamicArray{T}"/>. The index is not sanitized.
    /// </summary>
    /// <param name="i">the index</param>
    /// <param name="value">the value</param>
    private void SetData(long i, T value)
    {
        var (a, b) = SplitIndex(i);
        _data[a][b] = value;
    }
    
    /// <summary>
    /// Gets whether a specified element in this <see cref="DynamicArray{T}"/> is vacant.
    /// </summary>
    /// <param name="i">the index of the element</param>
    /// <returns>true if used, false if vacant</returns>
    private bool IsOccupied(long i)
    {
        return ((_occupiedIndexes[i / 64] >> (int)(i % 64)) & 0x1uL) == 1;
    }
    
    /// <summary>
    /// Sets the vacancy of a specified element in this <see cref="DynamicArray{T}"/> is vacant.
    /// </summary>
    /// <param name="i">the index of the element</param>
    /// <param name="value">whether the element is used.</param>
    private void SetOccupied(long i, bool value)
    {
        var mask = 0x1uL << (int)(i % 64);
        
        if (value)
            _occupiedIndexes[i / 64] |= mask;
        else
            _occupiedIndexes[i / 64] &= ~mask;
        
    }
    
    #endregion
    
    #region Public Util
    
    /// <summary>
    /// Returns a <see cref="string"/> of 1s and 0s, where each digit specifies whether the element at that index in
    /// this <see cref="DynamicArray{T}"/> is occupied or free. 0 = free and 1 = occupied
    /// </summary>
    public string FillState()
    {
        if (_occupiedIndexes.Length == 0) return "";
        
        var s = new StringBuilder();
        
        for (var i = 0; i < _occupiedIndexes.Length; i++)
        {
            var v = _occupiedIndexes[i];

            if (i == _occupiedIndexes.Length - 1)
            {
                for (var j = 0; j < Length - i * 64; j++)
                {
                    s.Append($"{(v >> j) & 0x1:b}");
                }
                
                break;
            }

            for (var j = 0; j < 64; j++)
            {
                s.Append($"{(v >> j) & 0x1:b}");
            }
        }
        
        
        return s.ToString();
    }
    
    /// <summary>
    /// Computes the hash of the contents of this <see cref="DynamicArray{T}"/>.
    /// </summary>
    /// <param name="start">the first element to factor into the hash</param>
    /// <param name="stop">the last element to factor into the hash</param>
    public Hash Hash(long start = 0, long stop = -1)
    {
        if (Length == 0) return new Hash();
        
        if (stop < 0) stop = Length-1;
        if (start < 0) start = 0;
        if (stop > Length-1) stop = Length-1;
        if (start > Length-1 || start > stop) start = long.Min(Length-1, stop);
        var hash = new Hash();
        for (var i = start; i <= stop; i++)
        {
            var h = new Hash(Get(i));
            hash ^= h;
        }
        
        return hash;
    }
    
    public void Dispose()
    {
        if (StoreOccupied) _occupiedIndexes.Clear();
        if (StoreModifications) _modifications.Dispose();
        foreach (var arr in _data)
        {
            _dataPool.Return(arr);
            Array.Clear(arr);
        }
        _data.Clear();
        
        GC.SuppressFinalize(this);
    }
    
    #endregion
    
    #region Exceptions
    
    public class StoredModificationsException() :
        Exception("Unable to retrieve modifications from DynamicArray: Modification Storing is disabled");
    
    public class StoredVacanciesException() :
        Exception("Unable to remove element: Vacancies are not stored");
    
    public class InvalidIndexException(long i, long length) :
        Exception($"Index {i} out of range for {nameof(DynamicArray<T>)} of length {length}");
    
    public class DeletedElementException(long i) :
        Exception($"Element at index {i} was removed and can no longer be accessed");
    
    #endregion
}

/// <summary>
/// Represents a single modification to an array or list, containing an index and a new value.
/// </summary>
/// <param name="index">the index</param>
/// <param name="value">the new value</param>
/// <typeparam name="T">the type of the new value</typeparam>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct ArrayModification<T>(long index, T? value)
{
    public readonly long Index = index;
    public readonly T? Value = value;
    
    public override string ToString()
    {
        return $"[{Index}] => {Value}";
    }
}
