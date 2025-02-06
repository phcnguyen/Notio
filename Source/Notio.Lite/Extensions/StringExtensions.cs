﻿using Notio.Lite.Reflection;
using System;

namespace Notio.Lite.Extensions;

/// <summary>
/// String related extension methods.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Returns a string that represents the given item
    /// It tries to use InvariantCulture if the ToString(IFormatProvider)
    /// overload exists.
    /// </summary>
    /// <param name="this">The item.</param>
    /// <returns>A <see cref="string" /> that represents the current object.</returns>
    public static string? ToStringInvariant(this object? @this)
    {
        if (@this == null)
            return string.Empty;

        var itemType = @this.GetType();

        if (itemType == typeof(string))
            return @this as string ?? string.Empty;

        return Definitions.BasicTypesInfo.Value.TryGetValue(itemType, out ExtendedTypeInfo? value)
            ? value.ToStringInvariant(@this)
            : @this.ToString();
    }

    /// <summary>
    /// Returns a string that represents the given item
    /// It tries to use InvariantCulture if the ToString(IFormatProvider)
    /// overload exists.
    /// </summary>
    /// <typeparam name="T">The type to get the string.</typeparam>
    /// <param name="item">The item.</param>
    /// <returns>A <see cref="string" /> that represents the current object.</returns>
    public static string ToStringInvariant<T>(this T item)
        => typeof(string) == typeof(T) ? item as string
        ?? string.Empty : ToStringInvariant(item as object)
        ?? string.Empty;

    /// <summary>
    /// Retrieves a section of the string, inclusive of both, the start and end indexes.
    /// This behavior is unlike JavaScript's Slice behavior where the end index is non-inclusive
    /// If the string is null it returns an empty string.
    /// </summary>
    /// <param name="this">The string.</param>
    /// <param name="startIndex">The start index.</param>
    /// <param name="endIndex">The end index.</param>
    /// <returns>Retrieves a substring from this instance.</returns>
    public static string Slice(this string @this, int startIndex, int endIndex)
    {
        if (@this == null)
            return string.Empty;

        var end = Math.Clamp(endIndex, startIndex, @this.Length - 1);
        return startIndex >= end ? string.Empty : @this.Substring(startIndex, (end - startIndex) + 1);
    }

    /// <summary>
    /// Gets a part of the string clamping the length and startIndex parameters to safe values.
    /// If the string is null it returns an empty string. This is basically just a safe version
    /// of string.Substring.
    /// </summary>
    /// <param name="this">The string.</param>
    /// <param name="startIndex">The start index.</param>
    /// <param name="length">The length.</param>
    /// <returns>Retrieves a substring from this instance.</returns>
    public static string SliceLength(this string @this, int startIndex, int length)
    {
        if (@this == null)
            return string.Empty;

        var start = Math.Clamp(startIndex, 0, @this.Length - 1);
        var len = Math.Clamp(length, 0, @this.Length - start);

        return len == 0 ? string.Empty : @this.Substring(start, len);
    }

    /// <summary>
    /// Gets the line and column number (i.e. not index) of the
    /// specified character index. Useful to locate text in a multi-line
    /// string the same way a text editor does.
    /// Please not that the tuple contains first the line number and then the
    /// column number.
    /// </summary>
    /// <param name="value">The string.</param>
    /// <param name="charIndex">Index of the character.</param>
    /// <returns>A 2-tuple whose value is (item1, item2).</returns>
    public static Tuple<int, int> TextPositionAt(this string value, int charIndex)
    {
        if (value == null)
            return Tuple.Create(0, 0);

        var index = Math.Clamp(charIndex, 0, value.Length - 1);

        var lineIndex = 0;
        var colNumber = 0;

        for (var i = 0; i <= index; i++)
        {
            if (value[i] == '\n')
            {
                lineIndex++;
                colNumber = 0;
                continue;
            }

            if (value[i] != '\r')
                colNumber++;
        }

        return Tuple.Create(lineIndex + 1, colNumber);
    }
}