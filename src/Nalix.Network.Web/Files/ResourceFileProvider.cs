using Nalix.Network.Web.MimeTypes;
using Nalix.Network.Web.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Nalix.Network.Web.Files;

/// <summary>
/// Provides access to embedded resources to a <see cref="FileModule"/>.
/// </summary>
/// <seealso cref="IFileProvider" />
/// <remarks>
/// Initializes a new instance of the <see cref="ResourceFileProvider"/> class.
/// </remarks>
/// <param name="assembly">The assembly where served files are contained as embedded resources.</param>
/// <param name="pathPrefix">A string to prepend to provider-specific paths
/// to form the name of a manifest resource in <paramref name="assembly"/>.</param>
/// <exception cref="ArgumentNullException"><paramref name="assembly"/> is <see langword="null"/>.</exception>
public class ResourceFileProvider(Assembly assembly, string pathPrefix) : IFileProvider
{
    private readonly DateTime _fileTime = DateTime.UtcNow;

    /// <inheritdoc />
    public event Action<string> ResourceChanged
    {
        add { }
        remove { }
    }

    /// <summary>
    /// Gets the assembly where served files are contained as embedded resources.
    /// </summary>
    public Assembly Assembly { get; } = Validate.NotNull(nameof(assembly), assembly);

    /// <summary>
    /// Gets a string that is prepended to provider-specific paths to form the name of a manifest resource in <see cref="Assembly"/>.
    /// </summary>
    public string PathPrefix { get; } = pathPrefix ?? string.Empty;

    /// <inheritdoc />
    public bool IsImmutable => true;

    /// <inheritdoc />
    public void Start(CancellationToken cancellationToken)
    {
    }

    /// <inheritdoc />
    public MappedResourceInfo? MapUrlPath(string urlPath, IMimeTypeProvider mimeTypeProvider)
    {
        string resourceName = PathPrefix + urlPath.Replace('/', '.');

        long size;
        try
        {
            using Stream? stream = Assembly.GetManifestResourceStream(resourceName);
            if (stream == null || stream == Stream.Null)
            {
                return null;
            }

            size = stream.Length;
        }
        catch (FileNotFoundException)
        {
            return null;
        }

        int lastSlashPos = urlPath.LastIndexOf('/');
        string name = urlPath[(lastSlashPos + 1)..];

        return MappedResourceInfo.ForFile(
            resourceName,
            name,
            _fileTime,
            size,
            mimeTypeProvider.GetMimeType(Path.GetExtension(name)));
    }

    /// <inheritdoc />
    public Stream OpenFile(string path) => Assembly.GetManifestResourceStream(path) ?? Stream.Null;

    /// <inheritdoc />
    public IEnumerable<MappedResourceInfo> GetDirectoryEntries(string path, IMimeTypeProvider mimeTypeProvider) => [];
}