// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Constraints;

using Asserts;

using Godot;

/// <summary>
///     A set of constrains to verify Godot signals.
/// </summary>
public interface ISignalConstraint : IAssertBase<GodotObject, ISignalConstraint>, IGdUnitAwaitable
{
    /// <summary>
    ///     Starts the monitoring of emitted signals during the test runtime.
    ///     It should be called first if you want to collect all emitted signals after the emitter has been created.
    /// </summary>
    /// <returns>ISignalAssert.</returns>
    ISignalConstraint StartMonitoring();

    /// <summary>
    ///     Verifies that a given signal is emitted until waiting time.
    /// </summary>
    /// <param name="signal">The signal name.</param>
    /// <param name="args">Optional signal arguments.</param>
    /// Example: it waits a maximum of 2000 ms before is failing if the signal is not emitted
    /// await AssertSignal(node).IsEmitted("draw").WithTimeout(2000);
    /// <returns>Task.</returns>
    Task<ISignalConstraint> IsEmitted(string signal, params Variant[] args);

    /// <summary>
    ///     Verifies that the given signal is NOT emitted until waiting time.
    /// </summary>
    /// <param name="signal">The signal name.</param>
    /// <param name="args">Optional signal arguments.</param>
    /// Example: it waits until 2000 ms and is failing if the signal is emitted in this time
    /// await AssertSignal(node).IsNotEmitted("draw").WithTimeout(2000);
    /// <returns>Task.</returns>
    Task<ISignalConstraint> IsNotEmitted(string signal, params Variant[] args);

    /// <summary>
    ///     Verifies if the signal exists on the emitter.
    /// </summary>
    /// <param name="signal">The signal name.</param>
    /// <returns>ISignalAssert.</returns>
    ISignalConstraint IsSignalExists(string signal);

    /// <summary>
    ///     Verifies if the signal emits counted.
    /// </summary>
    /// <param name="expectedCount">The expected count how often the signal should be emitted.</param>
    /// <param name="signal">The signal name.</param>
    /// <param name="args">Optional signal arguments.</param>
    /// <returns>ISignalAssert.</returns>
    ISignalConstraint IsCountEmitted(int expectedCount, string signal, params Variant[] args);
}
