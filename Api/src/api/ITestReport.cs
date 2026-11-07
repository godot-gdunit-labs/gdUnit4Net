// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Api;

using System.Collections.Generic;

/// <summary>
///     Represents an interface for a test report, providing details about the outcome of a test execution.
/// </summary>
public interface ITestReport
{
    /// <summary>
    ///     Gets the type of the test report, indicating the outcome or status of the test.
    /// </summary>
    ReportType Type { get; }

    /// <summary>
    ///     Gets the line number in the test script or code where the report originated.
    /// </summary>
    int LineNumber { get; }

    /// <summary>
    ///     Gets the message associated with the test report, providing additional details.
    /// </summary>
    string Message { get; }

    /// <summary>
    ///     Gets the stack trace information in case of test failure or error, providing contextual details.
    /// </summary>
    string? StackTrace { get; }

    /// <summary>
    ///     Gets a value indicating whether the test report denotes an error occurred during execution.
    /// </summary>
    bool IsError { get; }

    /// <summary>
    ///     Gets a value indicating whether the test report denotes a failure.
    /// </summary>
    bool IsFailure { get; }

    /// <summary>
    ///     Gets a value indicating whether the test report contains warnings.
    /// </summary>
    bool IsWarning { get; }

    /// <summary>
    ///     Serializes the test report into a dictionary representation containing its properties and values.
    /// </summary>
    /// <returns>The report serialized as a dictionary.</returns>
    IDictionary<string, object> Serialize();
}
