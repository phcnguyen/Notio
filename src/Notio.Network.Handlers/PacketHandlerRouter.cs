using Notio.Common.Connection;
using Notio.Common.Exceptions;
using Notio.Common.Logging;
using Notio.Common.Models;
using Notio.Diagnostics;
using Notio.Network.Handlers.Metadata;
using Notio.Network.Package;
using Notio.Shared.Injection;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Notio.Network.Handlers;

/// <summary>
/// A router that handles incoming packets and routes them to the appropriate handler method
/// based on the command specified in the packet.
/// </summary>
/// <param name="logger">An optional logger to log the operations of the packet handler router.</param>
public sealed class PacketHandlerRouter(ILogger? logger = null) : IDisposable
{
    private readonly ILogger? _logger = logger;
    private readonly InstanceManager _instanceManager = new();
    private readonly PerformanceMonitor _performanceMonitor = new();
    private readonly PacketHandlerResolver _handlerResolver = new(logger);

    private bool _isDisposed;

    /// <summary>
    /// Registers a packet handler type with the router.
    /// </summary>
    /// <typeparam name="T">The type of the handler, which must be decorated with <see cref="PacketControllerAttribute"/>.</typeparam>
    /// <exception cref="InvalidOperationException">Thrown if the handler type is not decorated with <see cref="PacketControllerAttribute"/>.</exception>
    public void RegisterHandler<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicMethods |
        DynamicallyAccessedMemberTypes.NonPublicMethods)] T>() where T : class
    {
        ArgumentNullException.ThrowIfNull(_logger);

        Type type = typeof(T);
        var controllerAttribute = type.GetCustomAttribute<PacketControllerAttribute>()
            ?? throw new InvalidOperationException($"Class {type.Name} must be marked with PacketControllerAttribute.");

        _handlerResolver.RegisterHandlers(type, controllerAttribute);
    }

    /// <summary>
    /// Routes the incoming packet to the appropriate handler based on the command.
    /// </summary>
    /// <param name="connection">The connection from which the packet was received.</param>
    public void RoutePacket(IConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ThrowIfDisposed();

        // Use a local performance monitor if needed
        var performanceMonitor = new PerformanceMonitor();
        performanceMonitor.Start();

        try
        {
            Packet? packet = connection.IncomingPacket?.Deserialize();
            if (packet is null) return;

            if (!_handlerResolver.TryGetHandler(packet?.Command ?? default, out PacketHandlerInfo? handlerInfo)
                || handlerInfo == null)
            {
                _logger?.Warn($"No handler found for command <{packet?.Command}>");
                return;
            }

            if (!ValidateAuthority(connection, handlerInfo.RequiredAuthority))
            {
                _logger?.Warn($"Access denied for command <{packet?.Command}>. Required: <{handlerInfo.RequiredAuthority}>, User: <{connection.Authority}>");
                return;
            }

            var instance = _instanceManager.GetOrCreateInstance(handlerInfo.ControllerType);
            Packet? response = (Packet?)handlerInfo.Method.Invoke(instance, [connection, packet]);

            if (response != null)
            {
                connection.Send(response.Value.Serialize());
                performanceMonitor.Stop();
                _logger?.Debug($"Command <{packet?.Command}> processed in {performanceMonitor.ElapsedMilliseconds}ms");
            }
        }
        catch (PackageException ex)
        {
            _logger?.Warn($"ID:{connection.Id}/IP:{connection.RemoteEndPoint}/Er:{ex}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.Error($"Error processing packet: {ex}");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    private static bool ValidateAuthority(IConnection connection, Authoritys required)
        => connection.Authority >= required;

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, nameof(PacketHandlerRouter));
}
