// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Asserts;

/// <summary>
///     Interface for extracting values from objects in the GdUnit4 testing framework.
///     Provides a standard mechanism for accessing specific values from various object types.
/// </summary>
/// <remarks>
///     This interface defines a strategy pattern for value extraction, allowing different
///     implementations to handle different object types or extraction techniques.
///     Implementations can extract values based on properties, indices, or custom logic.
/// </remarks>
public interface IValueExtractor
{
    /// <summary>
    ///     Extracts a value by a given implementation.
    /// </summary>
    /// <param name="value">The object containing the value to be extracted.</param>
    /// <returns>The extracted value, which may be null if the extraction fails or the source contains a null value.</returns>
    /// <remarks>
    ///     The extraction process is implementation-specific and may involve property access,
    ///     reflection, or conversion operations depending on the object type and extraction strategy.
    ///     Implementations should handle null input values gracefully.
    /// </remarks>
    object? ExtractValue(object? value);
}
