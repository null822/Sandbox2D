namespace Sandbox2DTest.Packets;

public readonly struct LocalPacket
{
    public readonly string Name;
    public readonly LocalPacketType Type;
    public readonly object? Arg;
    public readonly string? ResponseName;
    
    public LocalPacket(string name, LocalPacketType type, object? arg = null, string? responseName = null)
    {
        Name = name;
        Type = type;
        Arg = arg;
        ResponseName = responseName;
    }

    public T GetArg<T>()
    {
        if (Arg is not T arg)
        {
            throw new IncorrectTypeException<T>(Arg);
        }
        
        return arg;
    }
    
    public class IncorrectTypeException<T>(object? arg)
        : Exception($"{nameof(LocalPacket)} Argument Type was expected as {typeof(T).Name}, but was {arg?.GetType().Name}");
}
