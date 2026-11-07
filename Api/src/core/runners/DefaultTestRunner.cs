// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Runners;

using System.Diagnostics.CodeAnalysis;

using Api;

using Execution;

using Logging;

/// <summary>
///     Default test runner implementation that executes tests directly in the current process.
/// </summary>
[SuppressMessage(
    "Reliability",
    "CA2000:Dispose objects before losing scope",
    Justification = "DirectCommandExecutor ownership is transferred to base class which handles disposal")]
internal sealed class DefaultTestRunner : BaseTestRunner
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultTestRunner" /> class.
    ///     Initializes a new instance of the DefaultTestRunner.
    /// </summary>
    private static readonly ITestEngineLogger Logger = LoggerFactory.GetLogger<DefaultTestRunner>();

    /// <param name="settings">Test engine configuration settings.</param>
    internal DefaultTestRunner(TestEngineSettings settings)
        : base(new DirectCommandExecutor(), settings)
    {
    }

    public new void RunAndWait(List<TestSuiteNode> testSuiteNodes, ITestEventListener eventListener, CancellationToken cancellationToken)
    {
        Logger.LogInfo("Starting DefaultTestRunner");
        base.RunAndWait(testSuiteNodes, eventListener, cancellationToken);
    }
}
