// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text
namespace GdUnit4.Api;

/// <summary>
///     Defines the different types of test events that occur during test execution.
///     These events represent the lifecycle stages of tests and test suites in the GdUnit4 framework.
/// </summary>
public enum EventType
{
    /// <summary>
    ///     Initial event fired when the test engine is initialized.
    ///     This is typically the first event in the test execution lifecycle.
    /// </summary>
    Init,

    /// <summary>
    ///     Event fired when the test engine stops execution.
    ///     This is typically the last event in the test execution lifecycle.
    /// </summary>
    Stop,

    /// <summary>
    ///     Event fired before a test suite begins execution.
    ///     This event occurs once per test suite, before any tests in the suite are run.
    ///     Setup methods marked with the [Before] attribute execute during this phase.
    /// </summary>
    SuiteBefore,

    /// <summary>
    ///     Event fired after a test suite completes execution.
    ///     This event occurs once per test suite, after all tests in the suite have run.
    ///     Cleanup methods marked with the [After] attribute executed during this phase.
    /// </summary>
    SuiteAfter,

    /// <summary>
    ///     Event fired immediately before an individual test method begins execution.
    ///     Setup methods marked with the [BeforeTest] attribute execute during this phase.
    /// </summary>
    TestBefore,

    /// <summary>
    ///     Event fired immediately after an individual test method completes execution.
    ///     Cleanup methods marked with the [AfterTest] attribute execute during this phase.
    ///     This event fires regardless of whether the test succeeded, failed, or was skipped.
    /// </summary>
    TestAfter,

    /// <summary>
    ///     Event fired when the test discovery process begins.
    ///     Test discovery is the phase where test methods and test suites are identified.
    /// </summary>
    DiscoverStart,

    /// <summary>
    ///     Event fired when the test discovery process completes.
    ///     At this point, all test methods and test suites have been identified and are ready for execution.
    /// </summary>
    DiscoverEnd
}
