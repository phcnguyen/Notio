using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Notio.Runtime;

/// <summary>
/// Provides utility methods related to the operating system.
/// </summary>
public static class RuntimeOS
{
    private static readonly Lazy<OSPlatform> OsLazy = new(() =>
    {
        string windir = Environment.GetEnvironmentVariable("windir");
        if (!string.IsNullOrEmpty(windir) && windir.Contains('\\') && Directory.Exists(windir))
        {
            return OSPlatform.Windows;
        }
        // Check for Unix-like system by the existence of /proc/sys/kernel/ostype
        return File.Exists("/proc/sys/kernel/ostype") ? OSPlatform.Linux : OSPlatform.OSX;
    });

    /// <summary>
    /// Gets the current operating system.
    /// </summary>
    public static OSPlatform OS => OsLazy.Value;
}
