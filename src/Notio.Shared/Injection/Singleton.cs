﻿using System;
using System.Collections.Concurrent;

namespace Notio.Shared.Injection;

/// <summary>
/// Singleton class used to register and resolve services and instances using lazy loading.
/// Supports registering interfaces with implementations and factories for service creation.
/// </summary>
public static class Singleton
{
    private static readonly ConcurrentDictionary<Type, Type> TypeMapping = new();
    private static readonly ConcurrentDictionary<Type, Lazy<object>> Services = new();
    private static readonly ConcurrentDictionary<Type, Func<object>> Factories = new();

    /// <summary>
    /// Registers an instance of a class for dependency injection.
    /// </summary>
    /// <typeparam name="TClass">The type of the class to register.</typeparam>
    /// <param name="instance">The instance of the class to register.</param>
    /// <param name="allowOverwrite">If true, allows overwriting an existing registration of the same type. Defaults to false.</param>
    /// <exception cref="ArgumentNullException">Thrown when the instance is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the type is already registered and overwrite is not allowed.</exception>
    public static void Register<TClass>(TClass instance, bool allowOverwrite = false)
        where TClass : class
    {
        ArgumentNullException.ThrowIfNull(instance);
        Type type = typeof(TClass);

        Lazy<object> lazy = new(() => instance, isThreadSafe: true);

        if (allowOverwrite)
        {
            Services.AddOrUpdate(type, lazy, (_, _) => lazy);
        }
        else if (!Services.TryAdd(type, lazy))
        {
            throw new InvalidOperationException($"Type {type.Name} has been registered.");
        }
    }

    /// <summary>
    /// Registers an interface with its implementation type using lazy loading.
    /// </summary>
    /// <typeparam name="TInterface">The interface type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type of the interface.</typeparam>
    /// <param name="factory">An optional factory function to create instances of the implementation. If not provided, a default constructor will be used.</param>
    /// <exception cref="InvalidOperationException">Thrown if the interface has already been registered.</exception>
    public static void Register<TInterface, TImplementation>(Func<TImplementation>? factory = null)
        where TImplementation : class, TInterface
    {
        Type interfaceType = typeof(TInterface);
        Type implementationType = typeof(TImplementation);

        if (!TypeMapping.TryAdd(interfaceType, implementationType))
        {
            throw new InvalidOperationException($"Type {interfaceType.Name} has been registered.");
        }

        if (factory != null)
        {
            Factories.TryAdd(interfaceType, () => factory());
        }
    }

    /// <summary>
    /// Resolves or creates an instance of the requested type.
    /// </summary>
    /// <typeparam name="TClass">The type to resolve.</typeparam>
    /// <param name="createIfNotExists">If true, creates the instance if not already registered. Defaults to true.</param>
    /// <returns>The resolved or newly created instance of the requested type.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the type cannot be resolved or created.</exception>
    public static TClass? Resolve<TClass>(bool createIfNotExists = true) where TClass : class
    {
        Type type = typeof(TClass);

        if (Services.TryGetValue(type, out var lazyService))
        {
            return (TClass)lazyService.Value;
        }

        if (Factories.TryGetValue(type, out var factory))
        {
            Lazy<object> lazyInstance = new(() => factory(), isThreadSafe: true);
            Services.TryAdd(type, lazyInstance);
            return (TClass)lazyInstance.Value;
        }

        if (TypeMapping.TryGetValue(type, out Type? implementationType))
        {
            if (!Services.TryGetValue(implementationType, out var lazyImpl))
            {
                if (!createIfNotExists)
                {
                    return null;
                }

                try
                {
                    Lazy<object> lazyInstance = new(() =>
                    {
                        object instance = Activator.CreateInstance(implementationType)
                        ?? throw new InvalidOperationException($"Failed to create instance of type {implementationType.Name}");

                        return instance;
                    }, isThreadSafe: true);

                    Services.TryAdd(implementationType, lazyInstance);
                    Services.TryAdd(type, lazyInstance);
                    return (TClass)lazyInstance.Value;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to create instance of type {implementationType.Name}", ex);
                }
            }
            return (TClass)lazyImpl.Value;
        }

        if (!createIfNotExists)
        {
            return null;
        }

        throw new InvalidOperationException($"No registration found for type {type.Name}");
    }

    /// <summary>
    /// Checks whether a specific type is registered.
    /// </summary>
    /// <typeparam name="TClass">The type to check for registration.</typeparam>
    /// <returns>True if the type is registered, otherwise false.</returns>
    public static bool IsRegistered<TClass>() where TClass : class
        => Services.ContainsKey(typeof(TClass)) ||
           TypeMapping.ContainsKey(typeof(TClass)) ||
           Factories.ContainsKey(typeof(TClass));

    /// <summary>
    /// Removes the registration of a specific type.
    /// </summary>
    /// <typeparam name="TClass">The type to remove from registration.</typeparam>
    public static void Remove<TClass>() where TClass : class
    {
        Type type = typeof(TClass);
        Services.TryRemove(type, out _);
        TypeMapping.TryRemove(type, out _);
        Factories.TryRemove(type, out _);
    }

    /// <summary>
    /// Clears all registrations.
    /// </summary>
    public static void Clear()
    {
        Services.Clear();
        TypeMapping.Clear();
        Factories.Clear();
    }

    /// <summary>
    /// Disposes of the Singleton container, releasing any resources held by registered services.
    /// </summary>
    public static void Dispose()
    {
        foreach (var lazyService in Services.Values)
            if (lazyService.Value is IDisposable disposable)
                disposable.Dispose();

        Services.Clear();
        Factories.Clear();
        TypeMapping.Clear();
    }
}
