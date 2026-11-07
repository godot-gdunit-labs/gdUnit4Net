// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

// ReSharper disable once CheckNamespace
namespace GdUnit4;

using System.Collections.Generic;

/// <summary>
///     Defines a contract for providing test data values in data-driven testing scenarios.
/// </summary>
/// <remarks>
///     <para>
///         This interface enables the creation of custom data providers that can supply test values
///         for parameterized tests. Implementations should return a collection of values that will
///         be used as individual test case parameters.
///     </para>
/// </remarks>
/// <seealso cref="Core.Data.DataPointValueProvider" />
public interface IValueProvider
{
    /// <summary>
    ///     Gets a collection of values to be used as test case parameters.
    /// </summary>
    /// <returns>
    ///     An <see cref="IEnumerable{T}" /> of objects representing the test values.
    ///     Each object in the collection will be used as a parameter for a separate test case execution.
    /// </returns>
    /// <example>
    ///     <code>
    ///     public IEnumerable&lt;object&gt; GetValues()
    ///     {
    ///         // Single parameter values
    ///         yield return "test1";
    ///         yield return "test2";
    ///         yield return "test3";
    ///
    ///         // Or complex objects
    ///         yield return new TestConfiguration { Name = "Config1", Enabled = true };
    ///         yield return new TestConfiguration { Name = "Config2", Enabled = false };
    ///     }
    ///     </code>
    /// </example>
    IEnumerable<object> GetValues();
}
