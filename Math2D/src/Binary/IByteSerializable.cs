namespace Math2D.Binary;

public interface IByteSerializable
{
    /// <summary>
    /// The length, in bytes, of the result of <see cref="Serialize"/>.
    /// </summary>
    /// <remarks>Must always be correct for any instance of the base class</remarks>
    public static abstract int SerializeLength { get; }
    
    /// <summary>
    /// Returns this object, serialized to a <see cref="byte"/>[] of length
    /// <see cref="SerializeLength"/>.
    /// </summary>
    public byte[] Serialize(bool bigEndian = false);
}
