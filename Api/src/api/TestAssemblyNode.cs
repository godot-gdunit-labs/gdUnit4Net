// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Api;

using System.Text.Json.Serialization;

/// <summary>
///     Represents a test assembly in the GdUnit4 testing framework.
///     An assembly is the highest level in the test hierarchy, containing multiple test suites.
/// </summary>
/// <remarks>
///     TestAssemblyNode serves as a container for all test suites within a single .NET assembly.
///     It tracks the physical location of the assembly file and organizes the contained test suites.
/// </remarks>
public record TestAssemblyNode : TestNode
{
    /// <summary>
    ///     Gets the file path to the assembly containing this test case.
    /// </summary>
    public required string AssemblyPath { get; init; }

    /// <summary>
    ///     Gets the collection of test suites contained within this assembly.
    ///     This property is ignored during JSON serialization.
    /// </summary>
    [JsonIgnore]
#pragma warning disable CA1002
    public List<TestSuiteNode> Suites { get; init; } = [];
#pragma warning restore CA1002
}
