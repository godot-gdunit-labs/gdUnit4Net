// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Api;

/// <summary>
///     Represents a test case in the GdUnit4 testing framework.
///     Provides essential information about a test's location and identity.
/// </summary>
/// <remarks>
///     TestCaseNode represents an individual test method within a test suite.
///     It contains details about the method's location, identity, and execution requirements.
/// </remarks>
public record TestCaseNode : TestNode
{
    /// <summary>
    ///     Gets the name of the test method.
    /// </summary>
    public required string ManagedMethod { get; init; }

    /// <summary>
    ///     Gets the line number in the source file where this test method is defined.
    ///     This is useful for source code navigation and error reporting.
    /// </summary>
    public required int LineNumber { get; init; }

    /// <summary>
    ///     Gets the index of the attribute within the method if multiple test attributes exist.
    ///     This is used to distinguish between multiple test cases on the same method.
    /// </summary>
    public required int AttributeIndex { get; init; }

    /// <summary>
    ///     Gets a value indicating whether this test requires a running Godot engine to execute.
    ///     Tests that interact with Godot engine functionality need this flag set to true.
    /// </summary>
    public required bool RequireRunningGodotEngine { get; init; }
}
