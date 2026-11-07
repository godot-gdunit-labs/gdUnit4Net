// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Api;

/// <summary>
///     Represents the configuration for the test runner, holding information about which test suites should be run.
///     For each test suite, a set of 'TestCaseConfig' instances is attached to specify included tests.
/// </summary>
public class TestRunnerConfig
{
    /// <summary>
    ///     Gets the included test suites along with their associated test case configurations.
    /// </summary>
    public Dictionary<string, IEnumerable<TestCaseConfig>> Included { get; } = [];

    /// <summary>
    ///     Gets holds test run properties to control the test execution.
    /// </summary>
    public Dictionary<string, object> Properties { get; } = [];

    /// <summary>
    ///     Gets or sets a value indicating whether when set to true, standard output (stdout) from test cases is captured and included in the test result. This can be
    ///     useful for debugging.
    ///     Default: false.
    /// </summary>
    public bool CaptureStdOut { get; set; }

    /// <summary>
    ///     Gets or sets the version of the test runner configuration.
    /// </summary>
    public string Version { get; set; } = "2.0";
}

/// <summary>
///     Represents configuration specific to a test case.
/// </summary>
#pragma warning disable SA1402
public class TestCaseConfig
#pragma warning restore SA1402
{
    /// <summary>
    ///     Gets or sets the name of the test case.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a value indicating whether the test case is skipped.
    /// </summary>
    public bool Skipped { get; set; }
}
