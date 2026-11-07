// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Api;

using System;
using System.Collections.Generic;

/// <summary>
///     Defines the core identification properties of a test event.
/// </summary>
public interface ITestEvent
{
    /// <summary>
    ///     Gets the type of test event.
    /// </summary>
    EventType Type { get; }

    /// <summary>
    ///     Gets the unique identifier for this test event.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    ///     Gets the full qualified test name, used for console logging.
    /// </summary>
    string FullyQualifiedName { get; }

    /// <summary>
    ///     Gets the test display name. Used for data-driven test e.g., DataPointAttribute.
    /// </summary>
    string? DisplayName { get; }

    /// <summary>
    ///     Gets a value indicating whether the test failed.
    /// </summary>
    bool IsFailed { get; }

    /// <summary>
    ///     Gets a value indicating whether the test encountered an error.
    /// </summary>
    bool IsError { get; }

    /// <summary>
    ///     Gets a value indicating whether the test completed successfully.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    ///     Gets a value indicating whether the test produced warnings.
    /// </summary>
    bool IsWarning { get; }

    /// <summary>
    ///     Gets a value indicating whether the test was skipped.
    /// </summary>
    bool IsSkipped { get; }

    /// <summary>
    ///     Gets the elapsed time of the test execution.
    /// </summary>
    TimeSpan ElapsedInMs { get; }

    /// <summary>
    ///     Gets the collection of reports associated with the test event.
    ///     Each report provides details about various aspects of the test's execution.
    /// </summary>
    ICollection<ITestReport> Reports { get; }
}
