﻿using Notio.Common.Http;
using Notio.Common.Model;
using Notio.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Notio.Network.Http;

public class HttpServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly HttpRouter _router;
    private readonly CancellationTokenSource _cts;
    private readonly List<IMiddleware> _middleware;

    public HttpServer(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty.", nameof(url));

        _middleware = [];
        _router = new HttpRouter();
        _listener = new HttpListener();
        _listener.Prefixes.Add(url);
        _cts = new CancellationTokenSource();  
    }

    public void RegisterController<T>() where T : HttpController, new()
    {
        _router.RegisterController<T>();
    }

    public void UseMiddleware<T>() where T : IMiddleware, new()
    {
        _middleware.Add(new T());
    }

    public async Task StartAsync()
    {
        try
        {
            _listener.Start();
            NotioLog.Instance.Info("Server is running...");
            await HandleRequestsAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            NotioLog.Instance.Error("Error starting server", ex);
            throw;
        }
    }

    private async Task HandleRequestsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = ProcessRequestAsync(context);
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                // Lỗi xảy ra khi server dừng
            }
            catch (Exception ex)
            {
                NotioLog.Instance.Error("Error handling request", ex);
            }
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext listenerContext)
    {
        var context = new HttpContext(listenerContext);

        try
        {
            // Execute middleware
            foreach (var middleware in _middleware)
            {
                await middleware.InvokeAsync(context);
            }

            // Process route
            var response = await _router.RouteAsync(context);

            // Write response
            await WriteResponseAsync(context.Response, response);
        }
        catch (Exception ex)
        {
            NotioLog.Instance.Error("Error processing request", ex);
            context.Response.StatusCode = 500;
            await WriteResponseAsync(context.Response, HttpResult.Fail("Internal server error"));
        }
    }

    private static async Task WriteResponseAsync(HttpListenerResponse response, HttpResult apiResponse)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(apiResponse);
            var buffer = Encoding.UTF8.GetBytes(json);

            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;

            await response.OutputStream.WriteAsync(buffer.AsMemory());
        }
        finally
        {
            response.OutputStream.Close();
        }
    }

    public void Stop()
    {
        _cts.Cancel();
        _listener.Stop();
        NotioLog.Instance.Info("Server has been stopped.");
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Close();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}