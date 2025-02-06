﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Notio.Lite.Reflection;

/// <summary>
/// A thread-safe cache of attributes belonging to a given key (MemberInfo or Type).
///
/// The Retrieve method is the most useful one in this class as it
/// calls the retrieval process if the type is not contained
/// in the cache.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AttributeCache"/> class.
/// </remarks>
/// <param name="propertyCache">The property cache object.</param>
public class AttributeCache
{
    private readonly Lazy<ConcurrentDictionary<Tuple<object, Type>, IEnumerable<object>>> _data =
        new(() => new ConcurrentDictionary<Tuple<object, Type>, IEnumerable<object>>(), true);

    /// <summary>
    /// Gets the default cache.
    /// </summary>
    /// <value>
    /// The default cache.
    /// </value>
    public static Lazy<AttributeCache> DefaultCache { get; } = new Lazy<AttributeCache>(() => new AttributeCache());

    /// <summary>
    /// Gets one attribute of a specific type from a member.
    /// </summary>
    /// <typeparam name="T">The attribute type.</typeparam>
    /// <param name="member">The member.</param>
    /// <param name="inherit"><c>true</c> to inspect the ancestors of element; otherwise, <c>false</c>.</param>
    /// <returns>An attribute stored for the specified type.</returns>
    public T? RetrieveOne<T>(MemberInfo member, bool inherit = false)
        where T : Attribute
    {
        if (member == null)
            return default;

        var attr = Retrieve(
            new Tuple<object, Type>(member, typeof(T)),
            t => member.GetCustomAttributes(typeof(T), inherit));

        return ConvertToAttribute<T>(attr);
    }

    private static T? ConvertToAttribute<T>(IEnumerable<object> attr)
        where T : Attribute
    {
        if (attr?.Any() != true)
            return default;

        return attr.Count() == 1
            ? (T)Convert.ChangeType(attr.First(), typeof(T))
            : throw new AmbiguousMatchException("Multiple custom attributes of the same type found.");
    }

    private IEnumerable<object> Retrieve(Tuple<object, Type> key, Func<Tuple<object, Type>, IEnumerable<object>> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return _data.Value.GetOrAdd(key, k => factory.Invoke(k).Where(item => item != null));
    }
}