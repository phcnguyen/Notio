using System.Runtime.Serialization;

namespace Nalix.Common.Exceptions;

/// <summary>
/// Represents errors that occur during the configuration process in the Notio real-time server.
/// </summary>
[System.Serializable]
public class ConfigurationException : BaseException
{
    /// <summary>
    /// Gets the name of the configuration section where the error occurred.
    /// </summary>
    public string ConfigurationSection { get; }

    /// <summary>
    /// Gets the name of the configuration key where the error occurred.
    /// </summary>
    public string ConfigurationKey { get; }

    /// <summary>
    /// Gets the expected type of the configuration value.
    /// </summary>
    public System.Type ExpectedType { get; }

    /// <summary>
    /// Gets the actual value that caused the error.
    /// </summary>
    public object ActualValue { get; }

    /// <summary>
    /// Gets the configuration file path where the error occurred.
    /// </summary>
    public string ConfigFilePath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class.
    /// </summary>
    public ConfigurationException()
        : base("A configuration error occurred.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that describes the exception.</param>
    public ConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with a specified error
    /// message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that describes the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConfigurationException(string message, System.Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with detailed configuration information.
    /// </summary>
    /// <param name="message">The error message that describes the exception.</param>
    /// <param name="section">The configuration section where the error occurred.</param>
    /// <param name="key">The configuration key where the error occurred.</param>
    /// <param name="expectedType">The expected type of the configuration value.</param>
    /// <param name="actualValue">The actual value that caused the error.</param>
    /// <param name="configFilePath">The path to the configuration file.</param>
    public ConfigurationException(
        string message,
        string section,
        string key,
        System.Type expectedType = null,
        object actualValue = null,
        string configFilePath = null)
        : base($"{message} [Section: {section}, Key: {key}]")
    {
        ConfigurationSection = section;
        ConfigurationKey = key;
        ExpectedType = expectedType;
        ActualValue = actualValue;
        ConfigFilePath = configFilePath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with detailed configuration information
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that describes the exception.</param>
    /// <param name="section">The configuration section where the error occurred.</param>
    /// <param name="key">The configuration key where the error occurred.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <param name="expectedType">The expected type of the configuration value.</param>
    /// <param name="actualValue">The actual value that caused the error.</param>
    /// <param name="configFilePath">The path to the configuration file.</param>
    public ConfigurationException(
        string message,
        string section,
        string key,
        System.Exception innerException,
        System.Type expectedType = null,
        object actualValue = null,
        string configFilePath = null)
        : base($"{message} [Section: {section}, Key: {key}]", innerException)
    {
        ConfigurationSection = section;
        ConfigurationKey = key;
        ExpectedType = expectedType;
        ActualValue = actualValue;
        ConfigFilePath = configFilePath;
    }

    /// <summary>
    /// Sets the <see cref="SerializationInfo"/> with information about the exception.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data.</param>
    /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
    [System.Obsolete("This method is obsolete and will be removed in future versions.")]
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        System.ArgumentNullException.ThrowIfNull(info);

        base.GetObjectData(info, context);
        info.AddValue(nameof(ConfigurationSection), ConfigurationSection);
        info.AddValue(nameof(ConfigurationKey), ConfigurationKey);
        info.AddValue(nameof(ExpectedType), ExpectedType?.AssemblyQualifiedName);
        info.AddValue(nameof(ActualValue), ActualValue);
        info.AddValue(nameof(ConfigFilePath), ConfigFilePath);
    }
}
