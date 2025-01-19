﻿using System.Threading.Tasks;

namespace Notio.Http.Core;

public abstract class HttpController
{
    /// <summary>
    /// Trả về một kết quả thành công với dữ liệu.
    /// </summary>
    /// <param name="data">Dữ liệu phản hồi.</param>
    /// <returns>Kết quả HTTP thành công.</returns>
    protected static Task<HttpResponse> Ok(object data = null)
        => Task.FromResult(HttpResponse.Ok(data));

    /// <summary>
    /// Trả về một kết quả thất bại với thông báo lỗi.
    /// </summary>
    /// <param name="message">Thông báo lỗi.</param>
    /// <returns>Kết quả HTTP thất bại.</returns>
    protected static Task<HttpResponse> Fail(string message)
        => Task.FromResult(HttpResponse.Fail(message));
}