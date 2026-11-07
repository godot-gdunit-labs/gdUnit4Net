// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Constraints;

using Asserts;

/// <summary>
///     An set of vector constrains to verify Godot.Vector values in the GdUnit4 testing framework.
///     Provides specialized methods for comparing and validating vector types from the Godot engine.
/// </summary>
/// <typeparam name="TValue">
///     The vector type being tested. Must implement IEquatable{TValue} to enable value comparisons.
///     Typically used with Godot.Vector2, Godot.Vector3, or Godot.Vector4 types.
/// </typeparam>
public interface IVectorConstraint<in TValue> : IAssertBase<TValue, IVectorConstraint<TValue>>
    where TValue : IEquatable<TValue>
{
    /// <summary>
    ///     Verifies that the current vector value is equal to the expected one.
    /// </summary>
    /// <param name="expected">The vector value that the current value should equal.</param>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     This method overrides the base IsEqual method to provide a more specific return type.
    ///     The test will fail if the vectors are not considered equal according to their equality implementation.
    /// </remarks>
    new IVectorConstraint<TValue> IsEqual(TValue expected);

    /// <summary>
    ///     Verifies that the current vector value is not equal to the expected one.
    /// </summary>
    /// <param name="expected">The vector value that the current value should not equal.</param>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     This method overrides the base IsNotEqual method to provide a more specific return type.
    ///     The test will fail if the vectors are considered equal according to their equality implementation.
    /// </remarks>
    new IVectorConstraint<TValue> IsNotEqual(TValue expected);

    /// <summary>
    ///     Verifies that the current and expected vector values are approximately equal.
    ///     This is useful when comparing floating-point vector values where exact equality is rarely achieved.
    /// </summary>
    /// <param name="expected">The vector value to compare with.</param>
    /// <param name="approx">The acceptable difference for each component of the vector.</param>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     The approximation is applied to each component of the vector individually.
    ///     The test will fail if any component differs by more than the specified approximation value.
    /// </remarks>
    IVectorConstraint<TValue> IsEqualApprox(TValue expected, TValue approx);

    /// <summary>
    ///     Verifies that the current vector value is less than the given one.
    ///     This typically compares the length or magnitude of the vectors.
    /// </summary>
    /// <param name="expected">The vector value that the current vector should be less than.</param>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     The comparison is typically based on the vector's length or magnitude.
    ///     The test will fail if the current vector's length is greater than or equal to the expected vector's length.
    /// </remarks>
    IVectorConstraint<TValue> IsLess(TValue expected);

    /// <summary>
    ///     Verifies that the current vector value is less than or equal to the given one.
    ///     This typically compares the length or magnitude of the vectors.
    /// </summary>
    /// <param name="expected">The vector value that the current vector should be less than or equal to.</param>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     The comparison is typically based on the vector's length or magnitude.
    ///     The test will fail if the current vector's length is greater than the expected vector's length.
    /// </remarks>
    IVectorConstraint<TValue> IsLessEqual(TValue expected);

    /// <summary>
    ///     Verifies that the current vector value is greater than the given one.
    ///     This typically compares the length or magnitude of the vectors.
    /// </summary>
    /// <param name="expected">The vector value that the current vector should be greater than.</param>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     The comparison is typically based on the vector's length or magnitude.
    ///     The test will fail if the current vector's length is less than or equal to the expected vector's length.
    /// </remarks>
    IVectorConstraint<TValue> IsGreater(TValue expected);

    /// <summary>
    ///     Verifies that the current vector value is greater than or equal to the given one.
    ///     This typically compares the length or magnitude of the vectors.
    /// </summary>
    /// <param name="expected">The vector value that the current vector should be greater than or equal to.</param>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     The comparison is typically based on the vector's length or magnitude.
    ///     The test will fail if the current vector's length is less than the expected vector's length.
    /// </remarks>
    IVectorConstraint<TValue> IsGreaterEqual(TValue expected);

    /// <summary>
    ///     Verifies that the current vector value is between the given minimum and maximum boundaries (inclusive).
    ///     This typically compares each component of the vector individually.
    /// </summary>
    /// <param name="min">The minimum acceptable vector value for each component.</param>
    /// <param name="max">The maximum acceptable vector value for each component.</param>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     The comparison checks that each component of the current vector is between the corresponding components
    ///     of the min and max vectors (inclusive). The test will fail if any component falls outside these bounds.
    /// </remarks>
    IVectorConstraint<TValue> IsBetween(TValue min, TValue max);

    /// <summary>
    ///     Verifies that the current vector value is not between the given minimum and maximum boundaries (inclusive).
    ///     This typically compares each component of the vector individually.
    /// </summary>
    /// <param name="min">The minimum vector value that should not bound the current vector.</param>
    /// <param name="max">The maximum vector value that should not bound the current vector.</param>
    /// <returns>The same assertion instance to enable fluent method chaining.</returns>
    /// <remarks>
    ///     The test will fail if all components of the current vector are between the corresponding components
    ///     of the min and max vectors (inclusive). At least one part must be outside these bounds for the test to pass.
    /// </remarks>
    IVectorConstraint<TValue> IsNotBetween(TValue min, TValue max);
}
