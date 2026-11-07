// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Asserts;

using System;
using System.Collections.Generic;

/// <summary>
///     A tuple implementation to hold two or many values in the GdUnit4 testing framework.
///     Provides a generic container for storing multiple heterogeneous values as a single unit.
/// </summary>
/// <remarks>
///     This interface defines a lightweight tuple data structure used internally by the testing framework
///     for storing and comparing multiple values. It implements IEquatable to support value comparison
///     between tuples, which is essential for assertion-based testing.
/// </remarks>
public interface ITuple : IEquatable<object?>
{
    /// <summary>
    ///     Gets or sets the collection of values stored in this tuple.
    /// </summary>
    /// <remarks>
    ///     The values can be of any type, including null references.
    ///     The order of values in the collection is preserved and significant for equality comparisons.
    /// </remarks>
    IEnumerable<object?> Values { get; set; }
}
