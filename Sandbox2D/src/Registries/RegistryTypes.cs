using System;
using System.Diagnostics.CodeAnalysis;

namespace Sandbox2D.Registries;

/// <summary>
/// Represents a registry
/// </summary>
/// <typeparam name="T">the type of elements registered</typeparam>
public interface IRegistry<T> : IRegistry<T, T>;

/// <summary>
/// Represents a registry.
/// </summary>
/// <typeparam name="T">the type of elements registered</typeparam>
/// <typeparam name="TIn">the type of the arguments used to construct the elements</typeparam>
public interface IRegistry<T, in TIn>
{
    /// <summary>
    /// Registers a new <typeparamref name="T"/>.
    /// </summary>
    /// <param name="id">the ID of the new <typeparamref name="T"/></param>
    /// <param name="shaderIds">the value used to initialize the new <typeparamref name="T"/></param>
    public void Register(string id, TIn shaderIds);
    /// <summary>
    /// Gets a registered instance of <typeparamref name="T"/>.
    /// </summary>
    /// <param name="id">the ID of the registered instance</param>
    /// <returns>the instance</returns>
    public T Get(string id);
    /// <summary>
    /// Gets a registered instance of <typeparamref name="T"/>.
    /// </summary>
    /// <param name="id">the ID of the registered instance</param>
    /// <param name="value">the instance, or null if it was not found</param>
    /// <returns>whether the instance was found (registered)</returns>
    public bool TryGet(string id, [MaybeNullWhen(false)] out T value);
}


/// <summary>
/// Represents a factory registry with an internally handled constructor.
/// </summary>
/// <typeparam name="T">the type outputted by the factory</typeparam>
/// <typeparam name="TParams">the type of the parameters given to the internally handled constructor for the type
/// <typeparamref name="T"/></typeparam>
public interface IRegistryFactory<T, in TParams>
{
    /// <summary>
    /// Registers a new <typeparamref name="T"/>.
    /// </summary>
    /// <param name="id">the ID of the new <typeparamref name="T"/></param>
    /// <param name="value">the value used to initialize the new <typeparamref name="T"/></param>
    public void Register(string id, TParams value);
    /// <summary>
    /// Constructs a new instance of <typeparamref name="T"/>.
    /// </summary>
    /// <param name="id">the ID of the registered instance</param>
    /// <returns>the new instance</returns>
    public T Create(string id);
    /// <summary>
    /// Constructs a new instance of <typeparamref name="T"/>.
    /// </summary>
    /// <param name="id">the ID of the registered instance</param>
    /// <param name="value">[out] the new instance, or null if it was not found</param>
    /// <returns>whether the instance's <paramref name="id"/> was found (registered)</returns>
    public bool TryCreate(string id, [MaybeNullWhen(false)] out T value);
}

/// <summary>
/// Represents a factory registry.
/// </summary>
/// <typeparam name="T">the type outputted by the factory</typeparam>
/// <typeparam name="TCtor">the type of the delegate pointing to the constructors used to create instances of
/// <typeparamref name="T"/></typeparam>
/// <typeparam name="TParams">the type of the parameters given to the delegates of type <typeparamref name="TCtor"/></typeparam>
public interface IRegistryFactory<T, in TCtor, in TParams> where TCtor : Delegate
{
    /// <summary>
    /// Registers a new <typeparamref name="T"/>.
    /// </summary>
    /// <param name="id">the ID of the new <typeparamref name="T"/></param>
    /// <param name="delegate">the delegate used to initialize the new <typeparamref name="T"/></param>
    public void Register(string id, TCtor @delegate);
    /// <summary>
    /// Constructs a new instance of <typeparamref name="T"/>.
    /// </summary>
    /// <param name="id">the ID of the registered instance</param>
    /// <param name="params">the parameters given to the registered <typeparamref name="TCtor"/></param>
    /// <returns>the new instance</returns>
    public T Create(string id, TParams @params);
    /// <summary>
    /// Constructs a new instance of <typeparamref name="T"/>.
    /// </summary>
    /// <param name="id">the ID of the registered instance</param>
    /// <param name="params">the parameters given to the registered <typeparamref name="TCtor"/></param>
    /// <param name="value">[out] the new instance, or null if it was not found</param>
    /// <returns>whether the instance's <typeparamref name="TCtor"/> was found (registered)</returns>
    public bool TryCreate(string id, TParams @params, [MaybeNullWhen(false)] out T value);
}
