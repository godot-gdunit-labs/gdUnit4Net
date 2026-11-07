// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Api;

using System;

/// <summary>
///     Base record class for all test node types in the GdUnit4 testing framework.
///     Represents a node in the test hierarchy and provides common identification properties.
/// </summary>
/// <remarks>
///     TestNode serves as the foundation for test assemblies, suites, and individual test cases,
///     establishing the parent-child relationship between different test elements.
/// </remarks>
public record TestNode
{
    /// <summary>
    ///     Gets the unique identifier for this test node.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    ///     Gets or sets the unique identifier of the parent node in the test hierarchy.
    ///     This establishes the tree structure of the test organization.
    /// </summary>
    public required Guid ParentId { get; set; }
}
