﻿using System;

namespace Notio.Security;

/// <summary>
/// Exception xảy ra khi có lỗi trong quá trình mã hóa/giải mã
/// </summary>
public class CryptoOperationException : Exception
{
    public CryptoOperationException(string message) : base(message) { }
    public CryptoOperationException(string message, Exception innerException)
        : base(message, innerException) { }
}
