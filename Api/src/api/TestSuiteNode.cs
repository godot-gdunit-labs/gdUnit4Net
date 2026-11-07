// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Api;

using System.Collections.Generic;

/// <summary>
///     Represents a test suite in the GdUnit4 testing framework.
///     A test suite corresponds to a test class and contains multiple test cases.
/// </summary>
/// <remarks>
///     TestSuiteNode serves as an organizational unit that groups related test cases.
///     It provides information about the test class type and manages a collection of individual tests.
/// </remarks>
public record TestSuiteNode : TestNode
{
    /// <summary>
    ///     Gets the fully qualified name of the test class type.
    /// </summary>
    public required string ManagedType { get; init; }

    /// <summary>
    ///     Gets the collection of test cases contained within this test suite.
    /// </summary>
#pragma warning disable CA1002
    public required List<TestCaseNode> Tests { get; init; } = [];
#pragma warning restore CA1002

    /// <summary>
    ///     Gets the file path to the assembly containing this test suite.
    /// </summary>
    public required string AssemblyPath { get; init; }

    /// <summary>
    ///     Gets the file path to the source code file that contains this test suite.
    /// </summary>
    public required string SourceFile { get; init; }
}
