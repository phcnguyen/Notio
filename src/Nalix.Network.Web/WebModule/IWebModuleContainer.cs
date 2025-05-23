using Nalix.Network.Web.Utilities;
using System;

namespace Nalix.Network.Web.WebModule;

/// <summary>
/// Represents an object that contains a collection of <see cref="IWebModule"/> interfaces.
/// </summary>
public interface IWebModuleContainer : IDisposable
{
    /// <summary>
    /// Gets the modules.
    /// </summary>
    /// <value>
    /// The modules.
    /// </value>
    IComponentCollection<IWebModule> Modules { get; }
}