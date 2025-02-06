﻿using Notio.Common.Logging.Debugging;
using Notio.Lite.Formatters;
using Notio.Network.Web.Http;
using Notio.Network.Web.Http.Exceptions;
using Notio.Network.Web.Http.Extensions;
using System;
using System.Threading.Tasks;

namespace Notio.Network.Web.Request;

/// <summary>
/// Provides standard request deserialization callbacks.
/// </summary>
public static class RequestDeserializer
{
    /// <summary>
    /// <para>The default request deserializer used by Notio.</para>
    /// <para>Equivalent to <see cref="Json{TData}"/>.</para>
    /// </summary>
    /// <typeparam name="TData">The expected type of the deserialized data.</typeparam>
    /// <param name="context">The <see cref="IHttpContext"/> whose request body is to be deserialized.</param>
    /// <returns>A <see cref="Task{TResult}">Task</see>, representing the ongoing operation,
    /// whose result will be the deserialized data.</returns>
    public static Task<TData> Default<TData>(IHttpContext context)
    {
        return Json<TData>(context);
    }

    /// <summary>
    /// Asynchronously deserializes a request body in JSON format.
    /// </summary>
    /// <typeparam name="TData">The expected type of the deserialized data.</typeparam>
    /// <param name="context">The <see cref="IHttpContext"/> whose request body is to be deserialized.</param>
    /// <returns>A <see cref="Task{TResult}">Task</see>, representing the ongoing operation,
    /// whose result will be the deserialized data.</returns>
    public static Task<TData> Json<TData>(IHttpContext context)
    {
        return JsonInternal<TData>(context, default);
    }

    /// <summary>
    /// Returns a <see cref="RequestDeserializerCallback{TData}">RequestDeserializerCallback</see>
    /// that will deserialize an HTTP request body in JSON format, using the specified property name casing.
    /// </summary>
    /// <typeparam name="TData">The expected type of the deserialized data.</typeparam>
    /// <param name="jsonSerializerCase">The <see cref="JsonSerializerCase"/> to use.</param>
    /// <returns>A <see cref="RequestDeserializerCallback{TData}"/> that can be used to deserialize
    /// a JSON request body.</returns>
    public static RequestDeserializerCallback<TData> Json<TData>(JsonSerializerCase jsonSerializerCase)
    {
        return context => JsonInternal<TData>(context, jsonSerializerCase);
    }

    private static async Task<TData> JsonInternal<TData>(IHttpContext context, JsonSerializerCase jsonSerializerCase)
    {
        string body;
        using (System.IO.TextReader reader = context.OpenRequestText())
        {
            body = await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        try
        {
            return Lite.Formatters.Json.Deserialize<TData>(body, jsonSerializerCase);
        }
        catch (FormatException)
        {
            $"[{context.Id}] Cannot convert JSON request body to {typeof(TData).Name}, sending 400 Bad Request..."
                .Warn($"{nameof(RequestDeserializer)}.{nameof(Json)}");

            throw HttpException.BadRequest("Incorrect request data format.");
        }
    }
}