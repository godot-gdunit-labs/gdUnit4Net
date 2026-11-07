// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Api;

using System;
using System.Collections.Generic;

using Core;
using Core.Discovery;

/// <summary>
///     Interface for the GdUnit4 test engine responsible for discovering and executing tests.
///     Provides core functionality for test discovery, execution, and version information.
/// </summary>
/// <remarks>
///     The test engine is the main entry point for:
///     <list type="bullet">
///         <item>Test discovery in assemblies</item>
///         <item>Test execution with event reporting</item>
///         <item>Test cancellation</item>
///         <item>Version information</item>
///     </list>
/// </remarks>
public interface ITestEngine : IDisposable
{
    /// <summary>
    ///     Creates a new instance of the test engine with specified settings and logger.
    /// </summary>
    /// <param name="settings">Configuration settings for test execution behavior.</param>
    /// <param name="logger">Logger for capturing test engine diagnostics and operations.</param>
    /// <returns>A new ITestEngine instance configured with the specified settings.</returns>
    /// <remarks>
    ///     This is the primary factory method for creating test engine instances.
    ///     Each instance maintains its own execution context and state.
    /// </remarks>
    static ITestEngine GetInstance(TestEngineSettings settings, ITestEngineLogger logger) => new GdUnit4TestEngine(settings, logger);

    /// <summary>
    ///     Discovers test cases in the specified test assembly.
    /// </summary>
    /// <param name="testAssembly">Full path to the test assembly file to scan.</param>
    /// <returns>List of discovered test case descriptors.</returns>
    /// <remarks>
    ///     The discovery process scans the assembly for test cases using reflection.
    ///     Only public test methods decorated with appropriate test attributes are discovered.
    /// </remarks>
    IReadOnlyCollection<TestCaseDescriptor> Discover(string testAssembly);

    /// <summary>
    ///     Executes the specified test cases asynchronously with debugging support.
    /// </summary>
    /// <param name="testAssemblyNodes">Collection of test assemblies and their test cases to execute.</param>
    /// <param name="eventListener">Listener that receives test execution progress and results.</param>
    /// <param name="debuggerFramework">Framework providing debugging capabilities during test execution.</param>
    /// <remarks>
    ///     Test execution occurs asynchronously with progress reported through the event listener.
    ///     The debugger framework enables debugging of tests during execution.
    ///     Tests can be canceled via the Cancel() method.
    /// </remarks>
    void Execute(IReadOnlyCollection<TestAssemblyNode> testAssemblyNodes, ITestEventListener eventListener, IDebuggerFramework debuggerFramework);

    /// <summary>
    ///     Gets the version of the GdUnit4 test engine.
    /// </summary>
    /// <returns>Version information of the engine, or null if the gdUnit4Api is not installed.</returns>
    /// <exception cref="InvalidOperationException">Thrown when GdUnit4Api is not properly installed.</exception>
    /// <remarks>
    ///     The version is retrieved from the assembly containing the GdUnit4TestEngine type.
    ///     This method helps verify the correct installation and versioning of the test engine.
    /// </remarks>
    static Version? EngineVersion()
    {
        var assembly = typeof(GdUnit4TestEngine).Assembly ?? throw new InvalidOperationException("No 'GdUnit4Api' is installed!");
        return assembly.GetName().Version;
    }

    /// <summary>
    ///     Cancels any currently executing test operations.
    /// </summary>
    /// <remarks>
    ///     This method safely terminates ongoing test execution.
    ///     Resources are properly cleaned up, and in-progress tests are marked as canceled.
    /// </remarks>
    void Cancel();
}
