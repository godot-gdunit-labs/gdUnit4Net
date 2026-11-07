// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

// ReSharper disable once CheckNamespace
namespace GdUnit4;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Asserts;

using Constraints;

using Core.Execution.Exceptions;
using Core.Signals;

using Godot;

/// <summary>
///     Extension methods for awaiting Godot operations in tests.
/// </summary>
public static class GodotAwaiterExtension
{
    /// <summary>
    ///     Waits for a given signal is emitted by the specified node.
    /// </summary>
    /// <param name="emitter">The signal emitter node to monitor.</param>
    /// <param name="signal">The name of the signal to wait for.</param>
    /// <param name="args">An optional set of signal arguments to match. If provided, the signal must be emitted with exactly these arguments.</param>
    /// <returns>A task that completes with an <see cref="ISignalConstraint" /> when the signal is emitted, allowing for fluent assertion chaining.</returns>
    /// <exception cref="TestFailedException">Thrown if the signal is not emitted or if the signal doesn't exist on the emitter.</exception>
    /// <example>
    ///     <code>
    ///     // Waits for signal "mySignal" is emitted by the node.
    ///     await node.AwaitSignal("mySignal");
    ///
    ///     // Waits for signal with specific arguments.
    ///     await node.AwaitSignal("player_scored", 100, "Player1");
    ///
    ///     // Chain with timeout for better test control.
    ///     await node.AwaitSignal("ready").WithTimeout(5000);
    ///     </code>
    /// </example>
    /// <remarks>
    ///     This method automatically starts monitoring the emitter for signals.
    ///     The signal monitoring continues until the expected signal is emitted or the operation is cancelled.
    ///     Use <see cref="WithTimeout{TVariant}" /> to prevent indefinite waiting.
    /// </remarks>
    public static Task<ISignalConstraint> AwaitSignal(this Node emitter, string signal, params Variant[] args)
        => new SignalAssert(emitter).IsEmitted(signal, args);

    /// <summary>
    ///     Adds a timeout to any awaitable task, preventing indefinite waiting.
    /// </summary>
    /// <typeparam name="TVariant">The type of the awaitable task result, which must implement <see cref="IGdUnitAwaitable" />.</typeparam>
    /// <param name="task">The awaitable task to apply the timeout to.</param>
    /// <param name="timeoutMillis">The timeout duration in milliseconds. Must be greater than 0.</param>
    /// <returns>The original task result if it completes before the timeout; otherwise, the task continues with cancellation applied.</returns>
    /// <exception cref="ExecutionTimeoutException">Thrown if the timeout is reached and no cancellation token is available for graceful cancellation.</exception>
    /// <exception cref="TestFailedException">Thrown if the underlying assertion fails due to timeout cancellation.</exception>
    /// <example>
    ///     <code>
    ///     // Wait for signal with 2-second timeout
    ///     await node.AwaitSignal("draw").WithTimeout(2000);
    ///
    ///     // Wait for a method result with timeout
    ///     await AwaitMethod(node, "get_health").WithTimeout(1000);
    ///
    ///     // Chain multiple operations with timeouts
    ///     await node.AwaitSignal("started")
    ///               .WithTimeout(5000)
    ///               .ContinueWith(_ => node.AwaitSignal("finished"))
    ///               .WithTimeout(10000);
    ///     </code>
    /// </example>
    /// <remarks>
    ///     <para>
    ///         This method allows tests to specify a maximum wait time for any awaitable operation.
    ///         If the timeout is reached, the method attempts to cancel the underlying operation gracefully
    ///         using the signal cancellation token mechanism.
    ///     </para>
    ///     <para>
    ///         If no cancellation token is available (indicating the operation doesn't support cancellation),
    ///         an <see cref="ExecutionTimeoutException" /> is thrown immediately.
    ///     </para>
    ///     <para>
    ///         This is essential for preventing tests from hanging indefinitely when expected signals
    ///         or operations do not complete as expected.
    ///     </para>
    /// </remarks>
    [SuppressMessage("Performance", "CA1849", Justification = "Call async methods when in an async method")]
    public static async Task<TVariant> WithTimeout<TVariant>(this Task<TVariant> task, int timeoutMillis)
        where TVariant : IGdUnitAwaitable
    {
        Debug.Assert(task != null, nameof(task) + " != null");

        using var timeoutCts = new CancellationTokenSource();
        try
        {
            var timeoutTask = Task.Delay(timeoutMillis, timeoutCts.Token);
            var completedTask = await Task.WhenAny(task, timeoutTask).ConfigureAwait(true);
            if (completedTask == task)
                return await task.ConfigureAwait(false);

            if (GodotSignalCollector.TaskCancellations.TryRemove(task.Id, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }

            Debug.Assert(cts != null, $"Cant get cancellation token for task {task.Id}");

            return await task.ConfigureAwait(false);
        }
        catch (ExecutionTimeoutException e)
        {
            throw new ExecutionTimeoutException($"Assertion: Timed out after {timeoutMillis}ms.", e);
        }
        finally
        {
            if (!timeoutCts.IsCancellationRequested)
                timeoutCts.Cancel();
        }
    }
}
