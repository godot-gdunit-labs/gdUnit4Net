// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text
namespace GdUnit4.Api;

/// <summary>
///     Enum to categorize the type of test report, supporting multiple flags for combined states.
/// </summary>
public enum ReportType
{
    /// <summary>
    ///     Indicates that the test was executed successfully without any issues.
    /// </summary>
    Success,

    /// <summary>
    ///     Indicates that the test finished with warnings, but no failures occurred.
    /// </summary>
    Warning,

    /// <summary>
    ///     Indicates that the test failed due to some issues or errors.
    /// </summary>
    Failure,

    /// <summary>
    ///     Indicates that the test found orphan nodes.
    /// </summary>
    Orphan,

    /// <summary>
    ///     Denotes that the test was forcibly terminated before it completed execution.
    /// </summary>
    Terminated,

    /// <summary>
    ///     Indicates that the test execution was interrupted, possibly timeout or due to runtime conditions.
    /// </summary>
    Interrupted,

    /// <summary>
    ///     Indicates that the test was aborted, typically due to unrecoverable errors.
    /// </summary>
    Abort,

    /// <summary>
    ///     Marks the test as skipped and not executed during the test run.
    /// </summary>
    Skipped,

    /// <summary>
    ///     Represents standard output logs produced during the test execution.
    /// </summary>
    Stdout
}
