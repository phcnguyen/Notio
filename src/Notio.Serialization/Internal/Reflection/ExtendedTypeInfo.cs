﻿using Notio.Serialization.Internal.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Notio.Serialization.Internal.Reflection;

/// <summary>
/// Provides extended information about a type.
///
/// This class is mainly used to define sets of types within the Definition class
/// and it is not meant for other than querying the BasicTypesInfo dictionary.
/// </summary>
internal class ExtendedTypeInfo
{
    private const string TryParseMethodName = nameof(byte.TryParse);
    private const string ToStringMethodName = nameof(ToString);

    private readonly ParameterInfo[]? _tryParseParameters;
    private readonly int _toStringArgumentLength;

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtendedTypeInfo"/> class.
    /// </summary>
    /// <param name="t">The t.</param>
    protected ExtendedTypeInfo(Type t)
    {
        Type = t ?? throw new ArgumentNullException(nameof(t));

        IsNullableValueType = Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(Nullable<>);
        IsValueType = t.IsValueType;
        UnderlyingType = IsNullableValueType ? new NullableConverter(Type).UnderlyingType : Type;

        // Cache TryParse method info if it exists
        TryParseMethodInfo = GetTryParseMethodInfo();

        // Cache ToString method info
        ToStringMethodInfo = GetToStringMethodInfo();

        _tryParseParameters = TryParseMethodInfo?.GetParameters();
        _toStringArgumentLength = ToStringMethodInfo?.GetParameters().Length ?? 0;
    }

    #endregion Constructors

    #region Properties

    /// <summary>
    /// Gets the type this extended info class provides for.
    /// </summary>
    /// <value>
    /// The type.
    /// </value>
    private Type Type { get; }

    /// <summary>
    /// Gets a value indicating whether the type is a nullable value type.
    /// </summary>
    /// <value>
    /// <c>true</c> if this instance is nullable value type; otherwise, <c>false</c>.
    /// </value>
    private bool IsNullableValueType { get; }


    /// <summary>
    /// Gets a value indicating whether the type is value type.
    /// Nullable value types have this property set to False.
    /// </summary>
    public bool IsValueType { get; }

    /// <summary>
    /// When dealing with nullable value types, this property will
    /// return the underlying value type of the nullable,
    /// Otherwise it will return the same type as the Type property.
    /// </summary>
    /// <value>
    /// The type of the underlying.
    /// </value>
    private Type UnderlyingType { get; }

    /// <summary>
    /// Gets the try parse method information. If the type does not contain
    /// a suitable TryParse static method, it will return null.
    /// </summary>
    /// <value>
    /// The try parse method information.
    /// </value>
    private MethodInfo? TryParseMethodInfo { get; }

    /// <summary>
    /// Gets the ToString method info
    /// It will prefer the overload containing the IFormatProvider argument.
    /// </summary>
    /// <value>
    /// To string method information.
    /// </value>
    private MethodInfo? ToStringMethodInfo { get; }

    /// <summary>
    /// Gets a value indicating whether the type contains a suitable TryParse method.
    /// </summary>
    /// <value>
    /// <c>true</c> if this instance can parse natively; otherwise, <c>false</c>.
    /// </value>
    private bool CanParseNatively => TryParseMethodInfo != null;

    #endregion Properties

    #region Methods

    /// <summary>
    /// Tries to parse the string into an object of the type this instance represents.
    /// Returns false when no suitable TryParse methods exists for the type or when parsing fails
    /// for any reason. When possible, this method uses CultureInfo.InvariantCulture and NumberStyles.Any.
    /// </summary>
    /// <param name="s">The s.</param>
    /// <param name="result">The result.</param>
    /// <returns><c>true</c> if parse was converted successfully; otherwise, <c>false</c>.</returns>
    public bool TryParse(string s, out object? result)
    {
        result = Type.GetDefault();

        if (Type == typeof(string))
        {
            result = Convert.ChangeType(s, Type, CultureInfo.InvariantCulture);
            return true;
        }

        if (IsNullableValueType && string.IsNullOrEmpty(s) || !CanParseNatively)
        {
            return true;
        }

        try
        {
            var dynamicArguments = new List<object?> { s };
            AddTryParseArguments(dynamicArguments);

            var parseArguments = dynamicArguments.ToArray();
            if (TryParseMethodInfo != null && TryParseMethodInfo.Invoke(null, parseArguments) is bool and true)
            {
                result = parseArguments[^1];
                return true;
            }
        }
        catch
        {
            // Ignore exceptions
        }

        return false;
    }

    /// <summary>
    /// Converts this instance to its string representation,
    /// trying to use the CultureInfo.InvariantCulture
    /// IFormat provider if the overload is available.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <returns>A <see cref="string" /> that represents the current object.</returns>
    public string ToStringInvariant(object? instance)
    {
        if (instance == null) return string.Empty;

        return _toStringArgumentLength != 1 ? instance.ToString() ?? string.Empty
            : ToStringMethodInfo?.Invoke(instance, [CultureInfo.InvariantCulture]) as string ?? string.Empty;
    }

    #endregion Methods

    #region Private Methods

    private MethodInfo? GetTryParseMethodInfo()
    {
        try
        {
            return UnderlyingType.GetMethod(TryParseMethodName,
                         [typeof(string), typeof(NumberStyles), typeof(IFormatProvider), UnderlyingType.MakeByRefType()])
                   ?? UnderlyingType.GetMethod(TryParseMethodName, [typeof(string), UnderlyingType.MakeByRefType()]);
        }
        catch
        {
            return null;
        }
    }

    private MethodInfo? GetToStringMethodInfo()
    {
        try
        {
            return UnderlyingType.GetMethod(ToStringMethodName, [typeof(IFormatProvider)])
                   ?? UnderlyingType.GetMethod(ToStringMethodName, []);
        }
        catch
        {
            return null;
        }
    }

    private void AddTryParseArguments(List<object?> dynamicArguments)
    {
        for (var pi = 1; pi < (_tryParseParameters?.Length ?? 0) - 1; pi++)
        {
            var argInfo = _tryParseParameters?[pi];
            if (argInfo?.ParameterType == typeof(IFormatProvider))
                dynamicArguments.Add(CultureInfo.InvariantCulture);
            else if (argInfo?.ParameterType == typeof(NumberStyles))
                dynamicArguments.Add(NumberStyles.Any);
            else
                dynamicArguments.Add(null);
        }

        dynamicArguments.Add(null);
    }

    #endregion Private Methods
}

/// <summary>
/// Provides extended information about a type.
///
/// This class is mainly used to define sets of types within the Constants class
/// and it is not meant for other than querying the BasicTypesInfo dictionary.
/// </summary>
/// <typeparam name="T">The type of extended type information.</typeparam>
internal class ExtendedTypeInfo<T> : ExtendedTypeInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtendedTypeInfo{T}"/> class.
    /// </summary>
    public ExtendedTypeInfo()
        : base(typeof(T))
    {
    }
}
