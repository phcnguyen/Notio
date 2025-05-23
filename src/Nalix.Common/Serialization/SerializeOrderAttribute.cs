namespace Nalix.Common.Serialization;

/// <summary>
/// Specifies that a field or property should be included in serialization, with a defined order.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Property)]
public class SerializeOrderAttribute : System.Attribute
{
    /// <summary>
    /// Gets the serialization order of the field or property.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializeOrderAttribute"/> class with the specified serialization order.
    /// </summary>
    /// <param name="order">The order in which the field or property should be serialized.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Style", "IDE0290:Use primary constructor", Justification = "<Pending>")]
    public SerializeOrderAttribute(int order) => Order = order;
}
