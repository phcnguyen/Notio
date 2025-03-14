using Notio.Common.Attributes;
using Notio.Serialization.Internal.Extensions;
using Notio.Serialization.Internal.Reflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Notio.Serialization;

/// <summary>
/// A very simple, light-weight JSON library written by Mario
/// to teach Geo how things are done
///
/// This is an useful helper for small tasks but it doesn't represent a full-featured
/// serializer such as the beloved Serialization.NET.
/// </summary>
public class SerializerOptions
{
    private static readonly ConcurrentDictionary<Type, Dictionary<Tuple<string, string>, MemberInfo>> TypeCache = new();

    private readonly string[]? _includeProperties;
    private readonly Dictionary<int, List<WeakReference>> _parentReferences = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializerOptions"/> class.
    /// </summary>
    /// <param name="format">if set to <c>true</c> [format].</param>
    /// <param name="typeSpecifier">The type specifier.</param>
    /// <param name="includeProperties">The include properties.</param>
    /// <param name="excludeProperties">The exclude properties.</param>
    /// <param name="parentReferences">The parent references.</param>
    /// <param name="jsonSerializerCase">The json serializer case.</param>
    public SerializerOptions(
        bool format,
        string? typeSpecifier,
        string[]? includeProperties,
        string[]? excludeProperties = null,
        IReadOnlyCollection<WeakReference>? parentReferences = null,
        JsonSerializerCase jsonSerializerCase = JsonSerializerCase.None)
    {
        _includeProperties = includeProperties;

        ExcludeProperties = excludeProperties;
        Format = format;
        TypeSpecifier = typeSpecifier;
        JsonSerializerCase = jsonSerializerCase;

        if (parentReferences == null)
            return;

        foreach (var parentReference in parentReferences.Where(x => x is { IsAlive: true, Target: not null }))
        {
            IsObjectPresent(parentReference.Target!);
        }
    }

    /// <summary>
    /// Gets a value indicating whether this <see cref="SerializerOptions"/> is format.
    /// </summary>
    /// <value>
    ///   <c>true</c> if format; otherwise, <c>false</c>.
    /// </value>
    public bool Format { get; }

    /// <summary>
    /// Gets the type specifier.
    /// </summary>
    /// <value>
    /// The type specifier.
    /// </value>
    public string? TypeSpecifier { get; }

    /// <summary>
    /// Gets the json serializer case.
    /// </summary>
    /// <value>
    /// The json serializer case.
    /// </value>
    private JsonSerializerCase JsonSerializerCase { get; }

    /// <summary>
    /// Gets or sets the exclude properties.
    /// </summary>
    /// <value>
    /// The exclude properties.
    /// </value>
    public string[]? ExcludeProperties { get; set; }

    internal bool IsObjectPresent(object target)
    {
        int hash = target.GetHashCode();
        if (_parentReferences.TryGetValue(hash, out var list))
        {
            if (list.Any(wr => ReferenceEquals(wr.Target, target)))
                return true;
            list.Add(new WeakReference(target));
            return false;
        }
        _parentReferences.Add(hash, [new WeakReference(target)]);
        return false;
    }

    internal Dictionary<string, MemberInfo> GetProperties(Type targetType)
        => GetPropertiesCache(targetType)
        .When(() => _includeProperties?.Length > 0, query => query.Where(p => _includeProperties!.Contains(p.Key.Item1)))
        .When(() => ExcludeProperties?.Length > 0, query => query.Where(p => !ExcludeProperties!.Contains(p.Key.Item1)))
        .ToDictionary(x => x.Key.Item2, x => x.Value);

    private Dictionary<Tuple<string, string>, MemberInfo> GetPropertiesCache(Type targetType)
    {
        if (TypeCache.TryGetValue(targetType, out var current))
            return current;

        var fields = new List<MemberInfo>(
            PropertyTypeCache.DefaultCache.Value.RetrieveAllProperties(targetType)
            .Where(p => p.CanRead));

        if (targetType.IsValueType)
        {
            fields.AddRange(FieldTypeCache.DefaultCache.Value.RetrieveAllFields(targetType));
        }

        bool hasJsonInclude = fields.Any(x => Attribute.IsDefined(x, typeof(JsonIncludeAttribute)));

        Dictionary<Tuple<string, string>, MemberInfo> value = fields
            .Where(x => Attribute.IsDefined(x, typeof(JsonIncludeAttribute)) || x is PropertyInfo)
            .ToDictionary(
                x => Tuple.Create(
                    x.Name,
                    x.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? x.Name.GetNameWithCase(JsonSerializerCase)
                ),
                x => x
            );

        TypeCache.TryAdd(targetType, value);

        return value;
    }

}
