using System;
using System.Threading.Tasks;

namespace Nalix.Extensions;

/// <summary>
/// Provides extension methods for <see cref="Task"/> and <see cref="Task{TResult}"/>.
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// <para>Suspends execution until the specified <see cref="Task"/> is completed.</para>
    /// <para>This method operates similarly to the <see langword="await"/> C# operator,
    /// but is meant to be called from a non-<see langword="async"/> method.</para>
    /// </summary>
    /// <param name="this">The <see cref="Task"/> on which this method is called.</param>
    /// <exception cref="ArgumentNullException"><paramref name="this"/> is <see langword="null"/>.</exception>
    public static void Await(this Task @this)
    {
        ArgumentNullException.ThrowIfNull(@this);

        @this.GetAwaiter().GetResult();
    }

    /// <summary>
    /// <para>Suspends execution until the specified <see cref="Task"/> is completed
    /// and returns its result.</para>
    /// <para>This method operates similarly to the <see langword="await"/> C# operator,
    /// but is meant to be called from a non-<see langword="async"/> method.</para>
    /// </summary>
    /// <typeparam name="TResult">The type of the task's result.</typeparam>
    /// <param name="this">The <see cref="Task{TResult}"/> on which this method is called.</param>
    /// <returns>The result of <paramref name="this"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="this"/> is <see langword="null"/>.</exception>
    public static TResult Await<TResult>(this Task<TResult> @this)
    {
        ArgumentNullException.ThrowIfNull(@this);

        return @this.GetAwaiter().GetResult();
    }

    /// <summary>
    /// <para>Suspends execution until the specified <see cref="Task"/> is completed.</para>
    /// <para>This method operates similarly to the <see langword="await" /> C# operator,
    /// but is meant to be called from a non-<see langword="async" /> method.</para>
    /// </summary>
    /// <param name="this">The <see cref="Task" /> on which this method is called.</param>
    /// <param name="continueOnCapturedContext">If set to <see langword="true"/>,
    /// attempts to marshal the continuation back to the original context captured.
    /// This parameter has the same effect as calling the <see cref="Task.ConfigureAwait(bool)"/>
    /// method.</param>
    /// <exception cref="ArgumentNullException"><paramref name="this"/> is <see langword="null"/>.</exception>
    public static void Await(this Task @this, bool continueOnCapturedContext)
    {
        ArgumentNullException.ThrowIfNull(@this);

        @this.ConfigureAwait(continueOnCapturedContext).GetAwaiter().GetResult();
    }
}
