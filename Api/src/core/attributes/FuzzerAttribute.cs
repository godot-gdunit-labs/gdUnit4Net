// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

// ReSharper disable once CheckNamespace
// Need to be placed in the root namespace to be accessible by the test runner.
namespace GdUnit4;

using System;
using System.Collections.Generic;

/// <summary>
///     [PROTOTYPE] Attribute for generating test values through fuzzing techniques.
///     This is an early prototype and is not fully implemented yet.
/// </summary>
/// <remarks>
///     This prototype attribute is intended to eventually support fuzzing, which is a testing technique
///     that automatically generates semi-random data as inputs to a program to discover edge cases.
///     Currently, provides a minimal implementation that only increments an initial value.
/// </remarks>
/// <example>
///     <code>
/// // Example of intended future usage - not fully functional yet
/// [TestCase(Iterations = 40)]
/// public void IsBetween([Fuzzer(-20)] int value)
///    => AssertThat(value).IsBetween(-20, 20);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FuzzerAttribute : Attribute, IValueProvider
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FuzzerAttribute" /> class with a starting value.
    /// </summary>
    /// <param name="value">The initial value for the fuzzer prototype.</param>
    public FuzzerAttribute(int value) => Value = value;

    /// <summary>
    ///     Gets the current value of the fuzzer.
    /// </summary>
    public int Value { get; private set; }

    /// <summary>
    ///     Gets a sequence of test values to be used for the annotated parameter.
    ///     This is a prototype implementation that simply increments the initial value.
    ///     Future versions will implement more sophisticated fuzzing algorithms.
    /// </summary>
    /// <returns>An enumerable sequence containing the incremented value.</returns>
    public IEnumerable<object> GetValues()
    {
        Value += 1;
        yield return Value;
    }
}
