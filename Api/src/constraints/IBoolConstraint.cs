// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Constraints;

using Asserts;

/// <summary>
///     A set of constrains to verify boolean values.
/// </summary>
public interface IBoolConstraint : IAssertBase<bool, IBoolConstraint>
{
    /// <summary>
    ///     Verifies that the current value is true.
    /// </summary>
    /// <returns>IBoolConstrains.</returns>
    IBoolConstraint IsTrue();

    /// <summary>
    ///     Verifies that the current value is false.
    /// </summary>
    /// <returns>IBoolConstrains.</returns>
    IBoolConstraint IsFalse();
}
