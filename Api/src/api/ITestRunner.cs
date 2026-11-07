// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Api;

using System;
using System.Collections.Generic;
using System.Threading;

/// <summary>
///     Defines a test runner interface for executing GdUnit4 test suites.
/// </summary>
internal interface ITestRunner : IAsyncDisposable
{
    /// <summary>
    ///     Executes a list of test suites synchronously and waits for completion.
    /// </summary>
    /// <param name="testSuiteNodes">The list of test suites to execute.</param>
    /// <param name="eventListener">The listener for test execution events.</param>
    /// <param name="cancellationToken">Token to support cancellation of the test run.</param>
    internal void RunAndWait(List<TestSuiteNode> testSuiteNodes, ITestEventListener eventListener, CancellationToken cancellationToken);

    /// <summary>
    ///     Cancels the current test execution.
    /// </summary>
    internal void Cancel();
}
