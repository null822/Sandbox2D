namespace Math2D.Binary;

public interface IByteDeserializable<out TSelf>
{
    /// <summary>
    /// Returns a new instance of <see cref="TSelf"/>, deserialized from a sequence of bytes.
    /// </summary>
    public static abstract TSelf Deserialize(byte[] data, bool bigEndian = false);
}
