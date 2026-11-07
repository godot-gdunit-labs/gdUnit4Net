// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text
namespace GdUnit4.Asserts;

using Constraints;

/// <summary>
///     An Assertion Tool to verify string values.
/// </summary>
public interface IStringAssert : IStringConstraint, IAssertMessage<IStringConstraint>
{
    /// <summary>
    ///     The comparator to compare string length by different modes.
    /// </summary>
#pragma warning disable SA1400
    enum Compare
#pragma warning restore SA1400
    {
        /// <summary>
        ///     Specifies that the string length should be exactly equal to the expected value.
        /// </summary>
        EQUAL,

        /// <summary>
        ///     Specifies that the string length should be less than the expected value.
        /// </summary>
        LESS_THAN,

        /// <summary>
        ///     Specifies that the string length should be less than or equal to the expected value.
        /// </summary>
        LESS_EQUAL,

        /// <summary>
        ///     Specifies that the string length should be greater than the expected value.
        /// </summary>
        GREATER_THAN,

        /// <summary>
        ///     Specifies that the string length should be greater than or equal to the expected value.
        /// </summary>
        GREATER_EQUAL
    }
}
