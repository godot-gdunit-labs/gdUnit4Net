// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Discovery;

/// <summary>
///     Value type representing source code navigation information for a test method.
/// </summary>
internal readonly struct CodeNavigation
{
    /// <summary>
    ///     Gets the method this navigation data refers to.
    /// </summary>
    public required string MethodName { get; init; }

    /// <summary>
    ///     Gets the line number in the source file where the method is defined.
    /// </summary>
    public required int LineNumber { get; init; }

    /// <summary>
    ///     Gets the source code file path containing the method.
    /// </summary>
    public required string? CodeFilePath { get; init; }

    /// <summary>
    ///     Gets a value indicating whether indicates if this navigation data contains valid source information.
    /// </summary>
    public readonly bool IsValid => CodeFilePath != null;

    /// <summary>
    ///     Returns a JSON string representation of the test case descriptor.
    /// </summary>
    /// <returns>A formatted JSON string containing all properties of the test case descriptor.</returns>
    /// <remarks>
    ///     This method is primarily used for debugging and logging purposes.
    ///     The JSON output includes all properties with indented formatting for readability.
    /// </remarks>
    public override string ToString()
        => $"""
            CodeNavigation:
              Name: '{MethodName}'
              Line: {LineNumber}
              CodeFilePath: '{CodeFilePath}';
            """;
}
