// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Constraints;

using Asserts;

/// <summary>
///     A set of constrains to verify object values.
/// </summary>
/// <typeparam name="TValue">
///     The object type being tested.
/// </typeparam>
public interface IObjectConstraint<TValue> : IAssertBase<TValue, IObjectConstraint<TValue>>
{
    /// <summary>
    ///     Verifies that the current value is the same as the given one.
    /// </summary>
    /// <param name="expected">The object to be the same.</param>
    /// <returns>IObjectAssert.</returns>
    IObjectConstraint<TValue> IsSame(object expected);

    /// <summary>
    ///     Verifies that the current value is not the same as the given one.
    /// </summary>
    /// <param name="expected">The object to be NOT the same.</param>
    /// <returns>IObjectAssert.</returns>
    IObjectConstraint<TValue> IsNotSame(object expected);

    /// <summary>
    ///     Verifies that the current value is an instance of the given type.
    /// </summary>
    /// <typeparam name="TExpectedType">The type of instance to be expected.</typeparam>
    /// <returns>IObjectAssert.</returns>
    IObjectConstraint<TValue> IsInstanceOf<TExpectedType>();

    /// <summary>
    ///     Verifies that the current value is not an instance of the given type.
    /// </summary>
    /// <typeparam name="TExpectedType">The type of instance to be NOT expected.</typeparam>
    /// <returns>IObjectAssert.</returns>
    IObjectConstraint<TValue> IsNotInstanceOf<TExpectedType>();
}
