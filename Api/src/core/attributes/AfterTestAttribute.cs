// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

// ReSharper disable once CheckNamespace
// Need to be placed in the root namespace to be accessible by the test runner.
namespace GdUnit4;

using System;
using System.Runtime.CompilerServices;

/// <summary>
///     Attribute that marks methods to be executed after each test method in a test class.
///     Methods marked with this attribute are executed to clean up the test environment
///     after each test method has completed.
/// </summary>
/// <remarks>
///     Only one method in a test class should be marked with this attribute.
///     The method should not have parameters and should have a void return type.
///     This method will be executed even if the test method throws an exception.
/// </remarks>
/// <example>
///     <code>
/// [AfterTest]
/// public void Cleanup()
/// {
///     // Cleanup code to run after each test
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class AfterTestAttribute : TestStageAttribute
{
#pragma warning disable CA1019
    /// <summary>
    ///     Initializes a new instance of the <see cref="AfterTestAttribute" /> class.
    /// </summary>
    /// <param name="line">The line number where the attribute is applied (automatically provided).</param>
    /// <param name="name">The name of the method where the attribute is applied (automatically provided).</param>
    public AfterTestAttribute([CallerLineNumber] int line = 0, [CallerMemberName] string name = "")
#pragma warning restore CA1019
        : base(name, line)
    {
    }
}
