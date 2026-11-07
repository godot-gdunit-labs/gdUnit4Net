// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Constraints;

using Asserts;

/// <summary>
///     A set of constrains to verify string values.
/// </summary>
public interface IStringConstraint : IAssertBase<string, IStringConstraint>
{
    /// <summary>
    ///     Verifies that the current String is equal to the given one, ignoring case considerations.
    /// </summary>
    /// <param name="expected">The value to be equal.</param>
    /// <returns>IStringAssert.</returns>
    IStringConstraint IsEqualIgnoringCase(string expected);

    /// <summary>
    ///     Verifies that the current String is not equal to the given one, ignoring case considerations.
    /// </summary>
    /// <param name="expected">The value to be NOT equal.</param>
    /// <returns>IStringAssert.</returns>
    IStringConstraint IsNotEqualIgnoringCase(string expected);

    /// <summary>
    ///     Verifies that the current String is empty; it has a length of 0.
    /// </summary>
    /// <returns>IStringAssert.</returns>
    IStringConstraint IsEmpty();

    /// <summary>
    ///     Verifies that the current String is not empty; it has a length of minimum 1.
    /// </summary>
    /// <returns>IStringAssert.</returns>
    IStringConstraint IsNotEmpty();

    /// <summary>
    ///     Verifies that the current String contains the given String.
    /// </summary>
    /// <param name="expected">The value to be contained.</param>
    /// <returns>IStringAssert.</returns>
    IStringConstraint Contains(string expected);

    /// <summary>
    ///     Verifies that the current String does not contain the given String.
    /// </summary>
    /// <param name="expected">The value to be NOT contained.</param>
    /// <returns>IStringAssert.</returns>
    IStringConstraint NotContains(string expected);

    /// <summary>
    ///     Verifies that the current String does not contain the given String, ignoring case considerations.
    /// </summary>
    /// <param name="expected">The value to be contained.</param>
    /// <returns>IStringAssert.</returns>
    IStringConstraint ContainsIgnoringCase(string expected);

    /// <summary>
    ///     Verifies that the current String does not contain the given String, ignoring case considerations.
    /// </summary>
    /// <param name="expected">The value to be NOT contained.</param>
    /// <returns>IStringAssert.</returns>
    IStringConstraint NotContainsIgnoringCase(string expected);

    /// <summary>
    ///     Verifies that the current String starts with the given prefix.
    /// </summary>
    /// <param name="expected">The value to be starts with.</param>
    /// <returns>IStringAssert.</returns>
    IStringConstraint StartsWith(string expected);

    /// <summary>
    ///     Verifies that the current String ends with the given suffix.
    /// </summary>
    /// <param name="expected">The value to be ends with.</param>
    /// <returns>IStringAssert.</returns>
    IStringConstraint EndsWith(string expected);

    /// <summary>
    ///     Verifies that the current String has the expected length by used comparator.
    /// </summary>
    /// <param name="length">The lengths to be.</param>
    /// <param name="comparator">They used comparator mode.</param>
    /// <returns>IStringAssert.</returns>
    IStringConstraint HasLength(int length, IStringAssert.Compare comparator = IStringAssert.Compare.EQUAL);
}
