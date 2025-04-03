using System;

namespace Notio.Common.Attributes;

/// <summary>
/// An attribute that indicates that a property should be ignored during configuration container initialization.
/// </summary>
/// <remarks>
/// Properties marked with this attribute will not be set when loading values from a configuration file.
/// You can optionally provide a reason for why the property is ignored.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="ConfiguredIgnoreAttribute"/> class.
/// </remarks>
/// <param name="reason">The optional reason for ignoring the property.</param>
[AttributeUsage(AttributeTargets.Property)]
public class ConfiguredIgnoreAttribute(string reason = null) : Attribute
{
    /// <summary>
    /// Optional reason for ignoring the property during configuration.
    /// </summary>
    public string Reason { get; } = reason;
}
