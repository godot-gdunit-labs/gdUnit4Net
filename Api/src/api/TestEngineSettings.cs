// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Api;

/// <summary>
///     Configuration settings for the GdUnit4 test engine.
///     Contains runtime parameters and execution options that control test behavior.
/// </summary>
/// <remarks>
///     Example usage:
///     <code>
/// var settings = new TestEngineSettings
/// {
///     Parameters = "--verbose",
///     CaptureStdOut = true,
///     MaxCpuCount = 4,
///     SessionTimeout = 60000  // 1 minute timeout,
///     CompileProcessTimeout = 20000  // 20 seconds timeout for compilation
/// };
/// </code>
/// </remarks>
public sealed class TestEngineSettings
{
    /// <summary>
    ///     Gets additional Godot runtime parameters. These are passed to the Godot executable when running tests.
    /// </summary>
    /// <remarks>
    ///     These parameters are appended to the Godot command line when launching the test process.
    ///     Can include any valid Godot command line arguments.
    /// </remarks>
    public string? Parameters { get; init; }

    /// <summary>
    ///     Gets a value indicating whether controls whether standard output (stdout) from test cases is captured.
    /// </summary>
    /// <remarks>
    ///     When set to true, stdout is captured and included in the test result.
    ///     This can be useful for debugging test failures or analyzing test behavior.
    ///     Default value is false to optimize performance.
    /// </remarks>
    public bool CaptureStdOut { get; init; }

    /// <summary>
    ///     Gets specifies the maximum number of CPU cores to use for parallel test execution.
    /// </summary>
    /// <remarks>
    ///     Controls the degree of test parallelization. A value of 0 or 1 runs tests sequentially.
    ///     Higher values enable parallel test execution up to the specified core count.
    /// </remarks>
    public int MaxCpuCount { get; init; }

    /// <summary>
    ///     Gets the maximum duration allowed for a complete test session in milliseconds.
    /// </summary>
    /// <remarks>
    ///     After this timeout period expires, the test session is forcefully terminated.
    ///     Default value is 30000 milliseconds (30 seconds).
    ///     Set to a higher value for test suites that require more time to complete.
    /// </remarks>
    public int SessionTimeout { get; init; } = 30000;

    /// <summary>
    ///     Gets the maximum duration allowed for a compilation process in milliseconds.
    /// </summary>
    /// <remarks>
    ///     After this timeout period expires, the compilation process is forcefully terminated.
    ///     Default value is 120000 milliseconds (2 minutes)
    ///     Set to a higher value for projects that require more compilation time.
    /// </remarks>
    public int CompileProcessTimeout { get; init; } = 120000;

    /// <summary>
    ///     Gets the maximum duration allowed to connect to the Godot runtime host in milliseconds.
    /// </summary>
    /// <remarks>
    ///     Applies when connecting to the launched Godot runtime host over a named pipe.
    ///     After this timeout period expires, the connection attempt is aborted.
    ///     Default value is 10000 milliseconds (10 seconds).
    ///     Set to a higher value for environments where the Godot host needs more time to start.
    /// </remarks>
    public int GodotConnectTimeout { get; init; } = 10000;

    /// <summary>
    ///     Gets a value indicating whether a failing exit of the Godot runtime host is tolerated once all test results are reported.
    /// </summary>
    /// <remarks>
    ///     The Godot runtime host can crash during its shutdown, for example by a signal in the .NET runtime teardown,
    ///     after every test result was already reported. When enabled, such an exit is logged as a warning and does not
    ///     fail the test run. A failing exit before all test results are reported is always an error.
    ///     Default value is false.
    /// </remarks>
    public bool TolerateExitCodeAfterCompletion { get; init; }
}
