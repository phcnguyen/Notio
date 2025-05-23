using System;

namespace Nalix.Serialization.Formatters;

/// <summary>
/// Provides a global registry for registering and retrieving formatters without boxing.
/// </summary>
public static class FormatterProvider
{
    /// <summary>
    /// Initializes the static <see cref="FormatterProvider"/> class by registering formatters.
    /// </summary>
    static FormatterProvider()
    {
        // ============================================================ //
        // Integer types
        Register<System.Char>(new UnmanagedFormatter<System.Char>());
        Register<System.Byte>(new UnmanagedFormatter<System.Byte>());
        Register<System.SByte>(new UnmanagedFormatter<System.SByte>());
        Register<System.Int16>(new UnmanagedFormatter<System.Int16>());
        Register<System.UInt16>(new UnmanagedFormatter<System.UInt16>());
        Register<System.Int32>(new UnmanagedFormatter<System.Int32>());
        Register<System.UInt32>(new UnmanagedFormatter<System.UInt32>());
        Register<System.Int64>(new UnmanagedFormatter<System.Int64>());
        Register<System.UInt64>(new UnmanagedFormatter<System.UInt64>());
        Register<System.Single>(new UnmanagedFormatter<System.Single>());
        Register<System.Double>(new UnmanagedFormatter<System.Double>());
        Register<System.Boolean>(new UnmanagedFormatter<System.Boolean>());
        Register<System.Decimal>(new UnmanagedFormatter<System.Decimal>());

        // ============================================================ //
        // Integer arrays
        Register<System.Char[]>(new ArrayFormatter<System.Char>());
        Register<System.Byte[]>(new ArrayFormatter<System.Byte>());
        Register<System.SByte[]>(new ArrayFormatter<System.SByte>());
        Register<System.Int16[]>(new ArrayFormatter<System.Int16>());
        Register<System.UInt16[]>(new ArrayFormatter<System.UInt16>());
        Register<System.Int32[]>(new ArrayFormatter<System.Int32>());
        Register<System.UInt32[]>(new ArrayFormatter<System.UInt32>());
        Register<System.Int64[]>(new ArrayFormatter<System.Int64>());
        Register<System.UInt64[]>(new ArrayFormatter<System.UInt64>());
        Register<System.Single[]>(new ArrayFormatter<System.Single>());
        Register<System.Double[]>(new ArrayFormatter<System.Double>());
        Register<System.Boolean[]>(new ArrayFormatter<System.Boolean>());

        // ============================================================ //
        // String
        Register<System.String>(new StringFormatter());

        // ============================================================ //
        // Nullable types
        Register<System.Nullable<System.Char>>(new NullableFormatter<System.Char>());
        Register<System.Nullable<System.Byte>>(new NullableFormatter<System.Byte>());
        Register<System.Nullable<System.SByte>>(new NullableFormatter<System.SByte>());
        Register<System.Nullable<System.Int16>>(new NullableFormatter<System.Int16>());
        Register<System.Nullable<System.UInt16>>(new NullableFormatter<System.UInt16>());
        Register<System.Nullable<System.Int32>>(new NullableFormatter<System.Int32>());
        Register<System.Nullable<System.UInt32>>(new NullableFormatter<System.UInt32>());
        Register<System.Nullable<System.Int64>>(new NullableFormatter<System.Int64>());
        Register<System.Nullable<System.UInt64>>(new NullableFormatter<System.UInt64>());
        Register<System.Nullable<System.Single>>(new NullableFormatter<System.Single>());
        Register<System.Nullable<System.Double>>(new NullableFormatter<System.Double>());
        Register<System.Nullable<System.Decimal>>(new NullableFormatter<System.Decimal>());
        Register<System.Nullable<System.Boolean>>(new NullableFormatter<System.Boolean>());
    }

    /// <summary>
    /// Registers a formatter for the specified type.
    /// </summary>
    /// <typeparam name="T">The type for which the formatter is being registered.</typeparam>
    /// <param name="formatter">The formatter to register.</param>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown if the provided formatter is null.
    /// </exception>
    public static void Register<T>(IFormatter<T> formatter)
        => FormatterCache<T>.Formatter = formatter
        ?? throw new System.ArgumentNullException(nameof(formatter));

    /// <summary>
    /// Retrieves the registered formatter for the specified type.
    /// </summary>
    /// <typeparam name="T">The type for which to retrieve the formatter.</typeparam>
    /// <returns>The registered formatter for the specified type.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown if no formatter is registered for the given type.
    /// </exception>
    public static IFormatter<T> Get<T>()
    {
        IFormatter<T> formatter = FormatterCache<T>.Formatter;
        if (formatter != null)
            return formatter;

        // Auto-register for enums
        if (typeof(T).IsEnum)
        {
            EnumFormatter<T> enums = new();
            Register<T>(enums);

            return enums;
        }

        throw new InvalidOperationException($"No formatter registered for type {typeof(T)}.");
    }

    //public static IFormatter<T> Get<T>()
    //=> FormatterCache<T>.Formatter
    //?? throw new System.InvalidOperationException($"No formatter registered for type {typeof(T)}.");
}
