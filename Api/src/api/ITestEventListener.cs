// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Api;

/// <summary>
///     Interface for listening to test execution events and maintaining test execution state.
/// </summary>
/// <remarks>
///     The test event listener:
///     <list type="bullet">
///         <item>Tracks test execution status and completion counts</item>
///         <item>Receives and processes test events during execution</item>
///     </list>
/// </remarks>
public interface ITestEventListener
{
    /// <summary>
    ///     Gets or sets a value indicating whether gets or sets whether any tests have failed.
    /// </summary>
    bool IsFailed { get; protected set; }

    /// <summary>
    ///     Gets or sets the number of completed test cases.
    /// </summary>
    int CompletedTests { get; protected set; }

    /// <summary>
    ///     Processes and publishes a test event.
    /// </summary>
    /// <param name="testEvent">The test event to publish.</param>
    void PublishEvent(ITestEvent testEvent);

    /// <summary>
    ///     Reports why the given test cases may not be executed, e.g. the Godot runtime crashed or the test session ran into its timeout.
    /// </summary>
    /// <param name="testIds">The ids of the test cases whose execution was stopped.</param>
    /// <param name="cause">The cause, phrased to complete the sentence "The test was not executed, ...".</param>
    void ReportNotExecutedCause(IEnumerable<Guid> testIds, string cause)
    {
    }
}
