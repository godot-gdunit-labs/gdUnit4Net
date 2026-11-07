// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Asserts;

/// <summary>
///     The main interface of all GdUnit4 assertions.
///     Provides the core functionality for assertion-based testing in the GdUnit4 framework.
/// </summary>
/// <remarks>
///     This interface defines the base contract that all assertion types must implement,
///     enabling consistent behavior across different types of assertions.
/// </remarks>
#pragma warning disable CA1040
public interface IAssert
#pragma warning restore CA1040
{
}

/// <summary>
///     The base interface of all GdUnit asserts.
///     Provides common assertion methods that apply to most value types.
/// </summary>
/// <typeparam name="TValue">The type of value being tested by this assertion.</typeparam>
/// <typeparam name="TAssert">The type of used assertion.</typeparam>
/// <remarks>
///     This generic interface extends the core IAssert interface with type-specific
///     assertion methods for comparing values and checking nullability.
/// </remarks>
public interface IAssertBase<in TValue, out TAssert> : IAssert
    where TAssert : IAssert
{
    /// <summary>
    ///     Verifies that the current value is null.
    /// </summary>
    /// <returns>IAssertBase.</returns>
    TAssert IsNull();

    /// <summary>
    ///     Verifies that the current value is not null.
    /// </summary>
    /// <returns>IAssertBase.</returns>
    TAssert IsNotNull();

    /// <summary>
    ///     Verifies that the current value is equal to the expected one.
    /// </summary>
    /// <param name="expected">The value to be equal.</param>
    /// <returns>IAssertBase.</returns>
    TAssert IsEqual(TValue expected);

    /// <summary>
    ///     Verifies that the current value is not equal to the expected one.
    /// </summary>
    /// <param name="expected">The value to be NOT equal.</param>
    /// <returns>IAssertBase.</returns>
    TAssert IsNotEqual(TValue expected);
}

/// <summary>
///     The interface that provides message customization methods.
///     This interface is only available at the start of the assertion chain.
/// </summary>
/// <typeparam name="TAssert">The assertion type that will be returned after configuration.</typeparam>
public interface IAssertMessage<out TAssert>
    where TAssert : IAssert
{
    /// <summary>
    ///     Overrides the default failure message with the given custom message.
    /// </summary>
    /// <param name="message">A custom failure message to use instead of the default message.</param>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     Use this method when the default failure message doesn't provide enough context
    ///     about the purpose of the test or why a specific validation is important.
    /// </remarks>
    TAssert OverrideFailureMessage(string message);

    /// <summary>
    ///     Appends additional information to the default failure message.
    /// </summary>
    /// <param name="message">Additional context or information to append to the failure message.</param>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     Use this method to add extra context while preserving the original failure message.
    ///     The appended message will be displayed after the default message, typically
    ///     in an "Additional info:" section. This is useful for providing debugging hints,
    ///     test context, or expected behavior explanations without losing the built-in
    ///     assertion details.
    /// </remarks>
    TAssert AppendFailureMessage(string message);
}
