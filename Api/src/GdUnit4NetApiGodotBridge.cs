// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Api;

using Core;
using Core.Discovery;
using Core.Execution;
using Core.Extensions;

using Godot;
using Godot.Collections;

using static System.ArgumentException;

/// <summary>
///     The Godot Editor bridge to run C# tests inside the Godot Editor.
/// </summary>
public partial class GdUnit4NetApiGodotBridge : RefCounted
{
    /// <summary>
    ///     Creates a test suite based on the specified source path and line number.
    /// </summary>
    /// <param name="sourcePath">The path to the source file from which to create the test suite.</param>
    /// <param name="lineNumber">The line number in the source file where the method to test is defined.</param>
    /// <param name="testSuitePath">The path where the test suite should be created.</param>
    /// <returns>A dictionary containing information about the created test suite.</returns>
    public static Dictionary CreateTestSuite(string sourcePath, int lineNumber, string testSuitePath)
    {
        ThrowIfNullOrEmpty(sourcePath);
        ThrowIfNullOrEmpty(testSuitePath);

        var result = GdUnitTestSuiteBuilder.Build(NormalizedPath(sourcePath), lineNumber, NormalizedPath(testSuitePath));

        // we need to return the original resource name of the test suite on Godot site e.g. `res://foo/..` or `user://foo/..`
        if (result.ContainsKey("path"))
            result["path"] = testSuitePath;
        return result.ToGodotDictionary();
    }

    /// <summary>
    ///     Discovers all test cases from a given CSharpScript.
    /// </summary>
    /// <param name="sourceScript">The CSharpScript to discover test cases from.</param>
    /// <returns>A list of TestCaseDescriptor objects representing discovered test cases.</returns>
    public static IReadOnlyCollection<TestCaseDescriptor> DiscoverTestsFromScript(CSharpScript sourceScript) =>
        TestCaseDiscoverer.DiscoverTestCasesFromScript(sourceScript);

    /// <summary>
    ///     Executes test suites asynchronously.
    /// </summary>
    /// <param name="testSuiteNodes">The list of test suite nodes to execute.</param>
    /// <param name="eventListener">A callable that will receive test execution events.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous execution of the test suites.</returns>
    public static Task ExecuteAsync(IReadOnlyCollection<TestSuiteNode> testSuiteNodes, Callable eventListener, CancellationToken cancellationToken) =>
        new GdUnit4RuntimeExecutorGodotBridge().ExecuteAsync(testSuiteNodes, eventListener, cancellationToken);

    /// <summary>
    ///     Gets the version of the GdUnit4 assembly.
    /// </summary>
    /// <returns>The version string of the GdUnit4 assembly.</returns>
    public static string Version() => Assembly.GetAssembly(typeof(GdUnit4NetApiGodotBridge))!.GetName()!.Version!.ToString();

    private static string NormalizedPath(string path) =>
        path.StartsWith("res://", StringComparison.Ordinal) || path.StartsWith("user://", StringComparison.Ordinal)
            ? ProjectSettings.GlobalizePath(path)
            : path;
}
