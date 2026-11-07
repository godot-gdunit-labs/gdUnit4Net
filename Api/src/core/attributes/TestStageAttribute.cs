// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

// ReSharper disable once CheckNamespace
// Need to be placed in the root namespace to be accessible by the test runner.
namespace GdUnit4;

using System;

/// <summary>
///     Base attribute class for all test stage attributes in the GdUnit4 testing framework.
///     TestStageAttribute serves as the foundation for more specific test-related attributes
///     that mark different phases or types of test execution.
/// </summary>
/// <remarks>
///     This is an abstract base class not intended for direct use. Instead, use derived attributes
///     such as TestCaseAttribute, BeforeTestAttribute, AfterTestAttribute, etc.
///     All derived attributes inherit the ability to specify a description and timeout.
/// </remarks>
[AttributeUsage(AttributeTargets.Delegate)]
public abstract class TestStageAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TestStageAttribute" /> class.
    /// </summary>
    /// <param name="name">The name of the test stage, typically derived from the method name.</param>
    /// <param name="line">The source code line number where the attribute is applied.</param>
    protected TestStageAttribute(string name, int line)
    {
        Name = name;
        Line = line;
    }

    /// <summary>
    ///     Gets or sets describe the intention of the test, will be shown as a tool tip on the inspector node.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the timeout in ms to interrupt the test if the test execution takes longer as the given value.
    /// </summary>
    public long Timeout { get; set; } = -1;

    /// <summary>
    ///     Gets or sets the test name.
    /// </summary>
    internal string Name { get; set; }

    /// <summary>
    ///     Gets or sets the line of the annotated method.
    /// </summary>
    internal int Line { get; set; }
}
