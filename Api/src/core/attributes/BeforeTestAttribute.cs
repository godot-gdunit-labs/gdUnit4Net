// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

// ReSharper disable once CheckNamespace
// Need to be placed in the root namespace to be accessible by the test runner.
namespace GdUnit4;

using System;
using System.Runtime.CompilerServices;

/// <summary>
///     Attribute that marks methods to be executed before each test method in a test class.
///     Methods marked with this attribute are executed to set up the test environment
///     before each test method runs.
/// </summary>
/// <remarks>
///     Only one method in a test class should be marked with this attribute.
///     The method should not have parameters and should have a void return type.
/// </remarks>
/// <example>
///     <code>
/// [BeforeTest]
/// public void Setup()
/// {
///     // Setup code to run before each test
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class BeforeTestAttribute : TestStageAttribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BeforeTestAttribute" /> class.
    /// </summary>
    /// <param name="line">The line number where the attribute is applied (automatically provided).</param>
    /// <param name="name">The name of the method where the attribute is applied (automatically provided).</param>
#pragma warning disable CA1019
    public BeforeTestAttribute([CallerLineNumber] int line = 0, [CallerMemberName] string name = "")
#pragma warning restore CA1019
        : base(name, line)
    {
    }
}
