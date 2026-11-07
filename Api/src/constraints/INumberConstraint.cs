// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Constraints;

using System.Numerics;

using Asserts;

/// <summary>
///     Base interface for numeric value constraints in the GdUnit4 testing framework.
///     Provides specialized methods for comparing and validating numeric values.
/// </summary>
/// <typeparam name="TValue">
///     The numeric type being tested. Must implement IComparable, IComparable{TValue}, IEquatable{TValue},
///     IAdditionOperators{TValue, TValue, TValue}, and ISubtractionOperators{TValue, TValue, TValue}.
/// </typeparam>
public interface INumberConstraint<in TValue> : IAssertBase<TValue, INumberConstraint<TValue>>
    where TValue : IComparable, IComparable<TValue>, IEquatable<TValue>,
    IAdditionOperators<TValue, TValue, TValue>, ISubtractionOperators<TValue, TValue, TValue>
{
    /// <summary>
    ///     Verifies that the current value is less than the given one.
    /// </summary>
    /// <param name="expected">The value that the current value should be less than.</param>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     The test will fail if the current value is greater than or equal to the expected value.
    /// </remarks>
    INumberConstraint<TValue> IsLess(TValue expected);

    /// <summary>
    ///     Verifies that the current value is less than or equal to the given one.
    /// </summary>
    /// <param name="expected">The value that the current value should be less than or equal to.</param>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     The test will fail if the current value is greater than the expected value.
    /// </remarks>
    INumberConstraint<TValue> IsLessEqual(TValue expected);

    /// <summary>
    ///     Verifies that the current value is greater than the given one.
    /// </summary>
    /// <param name="expected">The value that the current value should be greater than.</param>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     The test will fail if the current value is less than or equal to the expected value.
    /// </remarks>
    INumberConstraint<TValue> IsGreater(TValue expected);

    /// <summary>
    ///     Verifies that the current value is greater than or equal to the given one.
    /// </summary>
    /// <param name="expected">The value that the current value should be greater than or equal to.</param>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     The test will fail if the current value is less than the expected value.
    /// </remarks>
    INumberConstraint<TValue> IsGreaterEqual(TValue expected);

    /// <summary>
    ///     Verifies that the current and expected values are approximately equal,
    ///     within the specified approximation range.
    /// </summary>
    /// <param name="expected">The value to compare with.</param>
    /// <param name="approx">The allowed difference between the values.</param>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     This method checks if the current value is within the range [expected-approx, expected+approx].
    ///     It's particularly useful for floating-point comparisons where exact equality can be problematic.
    /// </remarks>
    INumberConstraint<TValue> IsEqualApprox(TValue expected, TValue approx);

    /// <summary>
    ///     Verifies that the current value is an even number.
    /// </summary>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     The test will fail if the current value is odd (not divisible by 2).
    /// </remarks>
    INumberConstraint<TValue> IsEven();

    /// <summary>
    ///     Verifies that the current value is an odd number.
    /// </summary>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     The test will fail if the current value is even (divisible by 2).
    /// </remarks>
    INumberConstraint<TValue> IsOdd();

    /// <summary>
    ///     Verifies that the current value is negative (less than zero).
    /// </summary>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     The test will fail if the current value is zero or positive.
    /// </remarks>
    INumberConstraint<TValue> IsNegative();

    /// <summary>
    ///     Verifies that the current value is not negative (greater than or equal to zero).
    /// </summary>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     The test will fail if the current value is negative.
    /// </remarks>
    INumberConstraint<TValue> IsNotNegative();

    /// <summary>
    ///     Verifies that the current value is equal to zero.
    /// </summary>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     The test will fail if the current value is not exactly zero.
    /// </remarks>
    INumberConstraint<TValue> IsZero();

    /// <summary>
    ///     Verifies that the current value is not equal to zero.
    /// </summary>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     The test will fail if the current value is exactly zero.
    /// </remarks>
    INumberConstraint<TValue> IsNotZero();

    /// <summary>
    ///     Verifies that the current value is contained in the given set of values.
    /// </summary>
    /// <param name="expected">A set of possible values that the current value should equal.</param>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     The test will fail if the current value is not found in the expected array.
    /// </remarks>
    INumberConstraint<TValue> IsIn(params TValue[] expected);

    /// <summary>
    ///     Verifies that the current value is not contained in the given set of values.
    /// </summary>
    /// <param name="expected">A set of values that the current value should not equal.</param>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     The test will fail if the current value is found in the expected array.
    /// </remarks>
    INumberConstraint<TValue> IsNotIn(params TValue[] expected);

    /// <summary>
    ///     Verifies that the current value is between the given boundaries (inclusive).
    /// </summary>
    /// <param name="min">The minimum acceptable value.</param>
    /// <param name="max">The maximum acceptable value.</param>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     The test will fail if the current value is less than min or greater than max.
    ///     The range check is inclusive, meaning that min and max themselves are considered within the range.
    /// </remarks>
    INumberConstraint<TValue> IsBetween(TValue min, TValue max);
}
