// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

// ReSharper disable once CheckNamespace
// Need to be placed in the root namespace to be accessible by the test runner.
namespace GdUnit4;

using System;
using System.Runtime.CompilerServices;

/// <summary>
///     Attribute that marks methods to be executed once after all tests in the test suite have run.
///     Methods marked with this attribute are used for test suite cleanup.
/// </summary>
/// <remarks>
///     Only one method in a test class should be marked with this attribute.
///     This differs from AfterTest in that it runs only once for the entire test suite,
///     not after each test.
/// </remarks>
/// <example>
///     <code>
/// [After]
/// public void CleanupTestSuite()
/// {
///     // One-time cleanup code for the entire test suite
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class AfterAttribute : TestStageAttribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AfterAttribute" /> class.
    /// </summary>
    /// <param name="line">The line number where the attribute is applied (automatically provided).</param>
    /// <param name="name">The name of the method where the attribute is applied (automatically provided).</param>
#pragma warning disable CA1019
    public AfterAttribute([CallerLineNumber] int line = 0, [CallerMemberName] string name = "")
#pragma warning restore CA1019
        : base(name, line)
    {
    }
}
