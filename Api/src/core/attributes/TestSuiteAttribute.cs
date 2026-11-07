// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

// ReSharper disable once CheckNamespace
// Need to be placed in the root namespace to be accessible by the test runner.
namespace GdUnit4;

using System;
using System.Runtime.CompilerServices;

/// <summary>
///     Attribute that marks a class as a test suite in the GdUnit4 testing framework.
///     A test suite is a collection of related test methods organized in a class.
/// </summary>
/// <remarks>
///     This attribute should be applied to classes containing test methods.
///     Test suites can contain setup and teardown methods marked with Before/After attributes.
/// </remarks>
/// <example>
///     <code>
/// [TestSuite]
/// public class PlayerTests
/// {
///     // Test methods go here
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestSuiteAttribute : TestStageAttribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TestSuiteAttribute" /> class.
    /// </summary>
    /// <param name="line">The line number where the attribute is applied (automatically provided).</param>
    /// <param name="name">The name of the class where the attribute is applied (automatically provided).</param>
#pragma warning disable CA1019
    public TestSuiteAttribute([CallerLineNumber] int line = 0, [CallerMemberName] string name = "")
#pragma warning restore CA1019
        : base(name, line)
    {
    }
}
