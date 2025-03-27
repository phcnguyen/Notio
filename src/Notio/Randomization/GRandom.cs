using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Notio.Randomization;

/// <summary>
/// A high-performance class that supports generating random numbers with various data types and ranges.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GRandom"/> class with a user-provided seed value.
/// </remarks>
/// <param name="seed">The seed to initialize the random number generator.</param>
public sealed class GRandom(int seed)
{
    /// <summary>
    /// The maximum possible generated integer value.
    /// </summary>
    public const int RandMax = 0x7FFFFFFF;

    /// <summary>
    /// Inverse of RandMax as a double for faster calculations.
    /// </summary>
    public const double InvRandMax = 1.0 / RandMax;

    /// <summary>
    /// Current seed value for the random number generator.
    /// </summary>
    private int _seed = seed;

    /// <summary>
    /// Random number generator instance.
    /// </summary>
    private readonly Rand _rand = new((uint)seed);

    /// <summary>
    /// Initializes a new instance of the <see cref="GRandom"/> class with the default seed value of 0.
    /// </summary>
    public GRandom() : this(0)
    {
    }

    /// <summary>
    /// Resets the seed for the random number generator.
    /// </summary>
    /// <param name="seed">The new seed value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Seed(int seed)
    {
        _seed = seed;
        _rand.SetSeed((uint)seed);
    }

    /// <summary>
    /// Gets the current seed value.
    /// </summary>
    /// <returns>The current seed value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSeed() => _seed;

    /// <summary>
    /// Generates a random integer in the range [0, RandMax].
    /// </summary>
    /// <returns>A random integer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Next() => (int)(_rand.Get() & RandMax);

    /// <summary>
    /// Generates a random integer in the range [0, max).
    /// </summary>
    /// <param name="max">The exclusive upper bound.</param>
    /// <returns>A random integer.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when max is less than or equal to 0.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Next(int max)
    {
        if (max <= 0)
            throw new ArgumentOutOfRangeException(nameof(max), "Max must be positive");

        // Fast path for power of 2
        if ((max & (max - 1)) == 0)
            return (int)(((long)(_rand.Get() & RandMax) * max) >> 31);

        // Avoid modulo bias by rejecting values in the unfair region
        uint threshold = (uint)((RandMax - (RandMax % max)) & RandMax);
        uint result;
        do
        {
            result = _rand.Get() & RandMax;
        } while (result >= threshold);

        return (int)(result % max);
    }

    /// <summary>
    /// Generates a random integer in the range [min, max).
    /// </summary>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The exclusive upper bound.</param>
    /// <returns>A random integer.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when min is greater than or equal to max.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Next(int min, int max)
    {
        if (min >= max)
        {
            if (min == max)
                return min;
            throw new ArgumentOutOfRangeException(nameof(min), "Min must be less than max");
        }

        long range = (long)max - min;
        if (range <= int.MaxValue)
            return min + Next((int)range);

        // Handle large ranges that exceed int.MaxValue
        return min + (int)(NextDouble() * range);
    }

    /// <summary>
    /// Generates a random floating-point number in the range [0.0f, 1.0f].
    /// </summary>
    /// <returns>A random float.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float NextFloat() => _rand.GetFloat();

    /// <summary>
    /// Generates a random floating-point number in the range [0.0f, max).
    /// </summary>
    /// <param name="max">The exclusive upper bound.</param>
    /// <returns>A random float.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float NextFloat(float max) => NextFloat() * max;

    /// <summary>
    /// Generates a random floating-point number in the range [min, max).
    /// </summary>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The exclusive upper bound.</param>
    /// <returns>A random float.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float NextFloat(float min, float max) => min + NextFloat() * (max - min);

    /// <summary>
    /// Generates a random double-precision floating-point number in the range [0.0, 1.0].
    /// </summary>
    /// <returns>A random double.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double NextDouble() => _rand.GetDouble();

    /// <summary>
    /// Generates a random double-precision floating-point number in the range [0.0, max).
    /// </summary>
    /// <param name="max">The exclusive upper bound.</param>
    /// <returns>A random double.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double NextDouble(double max) => NextDouble() * max;

    /// <summary>
    /// Generates a random double-precision floating-point number in the range [min, max).
    /// </summary>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The exclusive upper bound.</param>
    /// <returns>A random double.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double NextDouble(double min, double max) => min + NextDouble() * (max - min);

    /// <summary>
    /// Performs a random check with a given percentage probability.
    /// </summary>
    /// <param name="pct">The percentage probability (0-100).</param>
    /// <returns>True if the random check passed based on the specified percentage.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool NextPct(int pct)
    {
        if (pct <= 0) return false;
        if (pct >= 100) return true;
        return Next(100) < pct;
    }

    /// <summary>
    /// Performs a random check with a given probability.
    /// </summary>
    /// <param name="probability">The probability (0.0-1.0).</param>
    /// <returns>True if the random check passed based on the specified probability.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool NextProbability(double probability)
    {
        if (probability <= 0.0) return false;
        if (probability >= 1.0) return true;
        return NextDouble() < probability;
    }

    /// <summary>
    /// Randomly shuffles a list using the Fisher-Yates algorithm.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list to shuffle.</param>
    public void ShuffleList<T>(IList<T> list)
    {
        ArgumentNullException.ThrowIfNull(list);

        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    /// <summary>
    /// Randomly shuffles a span using the Fisher-Yates algorithm.
    /// </summary>
    /// <typeparam name="T">The type of elements in the span.</typeparam>
    /// <param name="span">The span to shuffle.</param>
    public void ShuffleSpan<T>(Span<T> span)
    {
        int n = span.Length;
        while (n > 1)
        {
            n--;
            int k = Next(n + 1);
            (span[k], span[n]) = (span[n], span[k]);
        }
    }

    /// <summary>
    /// Returns a random item from the list.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list to pick from.</param>
    /// <returns>A random item from the list.</returns>
    /// <exception cref="ArgumentException">Thrown when the list is empty.</exception>
    public T Choose<T>(IList<T> list)
    {
        if (list == null || list.Count == 0)
            throw new ArgumentException("Cannot choose from an empty list", nameof(list));

        return list[Next(list.Count)];
    }

    /// <summary>
    /// Returns a random item from the span.
    /// </summary>
    /// <typeparam name="T">The type of elements in the span.</typeparam>
    /// <param name="span">The span to pick from.</param>
    /// <returns>A random item from the span.</returns>
    /// <exception cref="ArgumentException">Thrown when the span is empty.</exception>
    public T Choose<T>(ReadOnlySpan<T> span)
    {
        if (span.IsEmpty)
            throw new ArgumentException("Cannot choose from an empty span", nameof(span));

        return span[Next(span.Length)];
    }

    /// <summary>
    /// Fills the specified buffer with random bytes.
    /// </summary>
    /// <param name="buffer">The buffer to fill.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void NextBytes(Span<byte> buffer)
    {
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = (byte)Next(256);
    }

    /// <summary>
    /// Returns a string representation of the random number generator state.
    /// </summary>
    /// <returns>A string representation of the RNG.</returns>
    public override string ToString() => $"GRandom(seed={_seed}): {_rand}";
}
