﻿using System;
using System.Runtime.CompilerServices;

namespace Notio.Shared.Extensions;

public static class EnumExtensions
{
    public static TValue As<TEnum, TValue>(this TEnum @this) where TEnum : Enum
    {
        if (Unsafe.SizeOf<TEnum>() != Unsafe.SizeOf<TValue>())
            throw new ArgumentException("Size of TEnum and TValue must be the same.", nameof(@this));

        return Unsafe.As<TEnum, TValue>(ref @this);
    }
}