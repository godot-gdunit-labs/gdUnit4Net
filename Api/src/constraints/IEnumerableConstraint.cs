// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Constraints;

using Asserts;

using Godot.Collections;

/// <summary>
///     A set of constrains to verify enumerating.
/// </summary>
/// <typeparam name="TValue">The type of elements in the enumerable being asserted.</typeparam>
public interface IEnumerableConstraint<in TValue> : IAssertBase<IEnumerable<TValue?>, IEnumerableConstraint<TValue>>
{
    /// <summary>
    ///     Verifies that the current enumerable is equal to the given one, ignoring case considerations.
    /// </summary>
    /// <param name="expected">The expecting enumerable to be equal.</param>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> IsEqualIgnoringCase(IEnumerable<TValue?> expected);

    /// <summary>
    ///     Verifies that the current enumerable is not equal to the given one, ignoring case considerations.
    /// </summary>
    /// <param name="expected">The expecting enumerable value to be NOT equal.</param>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> IsNotEqualIgnoringCase(IEnumerable<TValue?> expected);

    /// <summary>
    ///     Verifies that the current enumerable is empty; it has a size of 0.
    /// </summary>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> IsEmpty();

    /// <summary>
    ///     Verifies that the current enumerable is not empty; it has a size of minimum 1.
    /// </summary>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> IsNotEmpty();

    /// <summary>
    ///     Verifies that the current enumerable is the same.
    /// </summary>
    /// <remarks>
    ///     Compares the current by object reference equals, for deep comparison use <see cref="IAssertBase{TValue,TAssert}.IsEqual(TValue)" />.
    /// </remarks>
    /// <param name="expected">The value to be the same.</param>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> IsSame(IEnumerable<TValue?> expected);

    /// <summary>
    ///     Verifies that the current enumerable is NOT the same.
    /// </summary>
    /// <remarks>
    ///     Compares the current by object reference equals, for deep comparison use <see cref="IAssertBase{TValue,TAssert}.IsNotEqual(TValue)" />.
    /// </remarks>
    /// <param name="expected">The value to be NOT the same.</param>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> IsNotSame(IEnumerable<TValue?> expected);

    /// <summary>
    ///     Verifies that the current enumerable has a size of given value.
    /// </summary>
    /// <param name="expected">The expected size.</param>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> HasSize(int expected);

    /// <summary>
    ///     Verifies that the current enumerable contains the given values, in any order.
    /// </summary>
    /// <param name="expected">The values to be contained.</param>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> Contains(params TValue?[] expected);

    /// <summary>
    ///     Verifies that the current enumerable contains the given values, in any order.
    /// </summary>
    /// <param name="expected">The values to be contained.</param>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> Contains(IEnumerable<TValue?> expected);

    /// <summary>
    ///     Verifies that the current enumerable contains the given values, in any order.
    /// </summary>
    /// <param name="expected">The values to be contained.</param>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> Contains(Array expected);

    /// <summary>
    ///     Verifies that the current enumerable contains the given values, in any order by object reference equals.
    /// </summary>
    /// <param name="expected">The values to be contained.</param>
    /// <remarks>
    ///     Compares the current by object reference equals, for deep comparison use <see cref="Contains(TValue?[])" />.
    /// </remarks>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> ContainsSame(params TValue?[] expected);

    /// <summary>
    ///     Verifies that the current enumerable contains the given values, in any order by object reference equals.
    /// </summary>
    /// <param name="expected">The values to be contained.</param>
    /// <remarks>
    ///     Compares the current by object reference equals, for deep comparison use <see cref="Contains(TValue?[])" />.
    /// </remarks>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> ContainsSame(IEnumerable<TValue?> expected);

    /// <summary>
    ///     Verifies that the current enumerable contains the given values, in any order by object reference equals.
    /// </summary>
    /// <param name="expected">The values to be contained.</param>
    /// <remarks>
    ///     Compares the current by object reference equals, for deep comparison use <see cref="Contains(Array)" />.
    /// </remarks>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> ContainsSame(Array expected);

    /// <summary>
    ///     Verifies that the current enumerable contains exactly only the given values and nothing else, in the same order.
    /// </summary>
    /// <param name="expected">The values to be contained exactly.</param>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> ContainsExactly(params TValue?[] expected);

    /// <summary>
    ///     Verifies that the current enumerable contains exactly only the given values and nothing else, in the same order.
    /// </summary>
    /// <param name="expected">The values to be contained exactly.</param>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> ContainsExactly(IEnumerable<TValue?> expected);

    /// <summary>
    ///     Verifies that the current enumerable contains exactly only the given values and nothing else, in the same order.
    /// </summary>
    /// <param name="expected">The values to be contained exactly.</param>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> ContainsExactly(Array expected);

    /// <summary>
    ///     Verifies that the current enumerable contains exactly only the given values and nothing else, in the same order by object reference equals.
    /// </summary>
    /// <param name="expected">The values to be contained exactly.</param>
    /// <remarks>
    ///     Compares the current by object reference equals, for deep comparison use <see cref="ContainsExactly(TValue?[])" />.
    /// </remarks>
    /// ///
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> ContainsSameExactly(params TValue?[] expected);

    /// <summary>
    ///     Verifies that the current enumerable contains exactly only the given values and nothing else, in the same order by object reference equals.
    /// </summary>
    /// <param name="expected">The values to be contained exactly.</param>
    /// <remarks>
    ///     Compares the current by object reference equals, for deep comparison use <see cref="ContainsExactly(TValue?[])" />.
    /// </remarks>
    /// ///
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> ContainsSameExactly(IEnumerable<TValue?> expected);

    /// <summary>
    ///     Verifies that the current enumerable contains exactly only the given values and nothing else, in the same order by object reference equals.
    /// </summary>
    /// <param name="expected">The values to be contained exactly.</param>
    /// <remarks>
    ///     Compares the current by object reference equals, for deep comparison use <see cref="ContainsExactly(Array)" />.
    /// </remarks>
    /// ///
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> ContainsSameExactly(Array expected);

    /// <summary>
    ///     Verifies that the current enumerable contains exactly only the given values and nothing else, in any order.
    /// </summary>
    /// <param name="expected">The values to be contained exactly in any order.</param>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> ContainsExactlyInAnyOrder(params TValue?[] expected);

    /// <summary>
    ///     Verifies that the current enumerable contains exactly only the given values and nothing else, in any order.
    /// </summary>
    /// <param name="expected">The values to be contained exactly in any order.</param>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> ContainsExactlyInAnyOrder(IEnumerable<TValue?> expected);

    /// <summary>
    ///     Verifies that the current enumerable contains exactly only the given values and nothing else, in any order.
    /// </summary>
    /// <param name="expected">The values to be contained exactly in any order.</param>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> ContainsExactlyInAnyOrder(Array expected);

    /// <summary>
    ///     Verifies that the current enumerable contains exactly only the given values and nothing else, in any order by object reference equals.
    /// </summary>
    /// ///
    /// <param name="expected">The values to be contained exactly in any order.</param>
    /// <remarks>
    ///     Compares the current by object reference equals, for deep comparison use <see cref="ContainsExactlyInAnyOrder(TValue?[])" />.
    /// </remarks>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> ContainsSameExactlyInAnyOrder(params TValue?[] expected);

    /// <summary>
    ///     Verifies that the current enumerable contains exactly only the given values and nothing else, in any order by object reference equals.
    /// </summary>
    /// ///
    /// <param name="expected">The values to be contained exactly in any order.</param>
    /// <remarks>
    ///     Compares the current by object reference equals, for deep comparison use <see cref="ContainsExactlyInAnyOrder(TValue?[])" />.
    /// </remarks>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> ContainsSameExactlyInAnyOrder(IEnumerable<TValue?> expected);

    /// <summary>
    ///     Verifies that the current enumerable contains exactly only the given values and nothing else, in any order by object reference equals.
    /// </summary>
    /// ///
    /// <param name="expected">The values to be contained exactly in any order.</param>
    /// <remarks>
    ///     Compares the current by object reference equals, for deep comparison use <see cref="ContainsExactlyInAnyOrder(Array)" />.
    /// </remarks>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> ContainsSameExactlyInAnyOrder(Array expected);

    /// <summary>
    ///     Verifies that the current enumerable do NOT contain the given values, in any order.
    /// </summary>
    /// <param name="expected">The values to DON'T contain.</param>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> NotContains(params TValue?[] expected);

    /// <summary>
    ///     Verifies that the current enumerable do NOT contain the given values, in any order.
    /// </summary>
    /// <param name="expected">The values to DON'T contain.</param>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> NotContains(IEnumerable<TValue?> expected);

    /// <summary>
    ///     Verifies that the current enumerable do NOT contain the given values, in any order.
    /// </summary>
    /// <param name="expected">The values to DON'T contain.</param>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> NotContains(Array expected);

    /// <summary>
    ///     Verifies that the current enumerable do NOT contain the given values, in any order by object reference equals.
    /// </summary>
    /// <param name="expected">The values to DON'T contain.</param>
    /// <remarks>
    ///     Compares the current by object reference equals, for deep comparison use <see cref="NotContains(TValue?[])" />.
    /// </remarks>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> NotContainsSame(params TValue?[] expected);

    /// <summary>
    ///     Verifies that the current enumerable do NOT contain the given values, in any order by object reference equals.
    /// </summary>
    /// <param name="expected">The values to DON'T contain.</param>
    /// <remarks>
    ///     Compares the current by object reference equals, for deep comparison use <see cref="NotContains(TValue?[])" />.
    /// </remarks>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> NotContainsSame(IEnumerable<TValue?> expected);

    /// <summary>
    ///     Verifies that the current enumerable do NOT contain the given values, in any order by object reference equals.
    /// </summary>
    /// <param name="expected">The values to DON'T contain.</param>
    /// <remarks>
    ///     Compares the current by object reference equals, for deep comparison use <see cref="NotContains(Array)" />.
    /// </remarks>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<TValue?> NotContainsSame(Array expected);

    /// <summary>
    ///     Extracts all values by given function name and optional arguments into a new EnumerableAssert<br />
    ///     If the elements not accessible by `methodName,` the value is converted to `"n.a"`, expecting null values.
    /// </summary>
    /// <param name="methodName">The name of the method to extract.</param>
    /// <param name="args">The method arguments are needed.</param>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<object?> Extract(string methodName, params object[] args);

    /// <summary>
    ///     Extracts all values by a given extractor into a new EnumerableAssert<br />
    ///     If the elements not extractable than the value is converted to `"n.a"`, expecting null values.
    /// </summary>
    /// <param name="extractors">A set of value extractors.</param>
    /// <returns>IEnumerableAssert.</returns>
    IEnumerableConstraint<object?> ExtractV(params IValueExtractor[] extractors);
}
