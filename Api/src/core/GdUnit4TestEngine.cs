// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core;

using System.Diagnostics;

using Api;

using Discovery;

using Extensions;

using Logging;

using Runners;

internal sealed class GdUnit4TestEngine(TestEngineSettings settings, ITestEngineLogger logger) : ITestEngine
{
    private static readonly ITestEngineLogger Logger = LoggerFactory.GetLogger<GdUnit4TestEngine>();

    private readonly LoggerFactory loggerFactory = LoggerFactory.WithRootLogger(logger).Build();
    private readonly object taskLock = new();
    private CancellationTokenSource? cancellationSource;

    private TestEngineSettings Settings { get; } = settings;

    private List<ITestRunner> ActiveTestRunners { get; } = [];

    public void Dispose()
    {
        loggerFactory.Dispose();
        cancellationSource?.Dispose();
    }

    public void Cancel()
    {
        lock (taskLock)
            cancellationSource?.Cancel();
        foreach (var activeTestRunner in ActiveTestRunners)
            activeTestRunner.Cancel();
    }

    public IReadOnlyCollection<TestCaseDescriptor> Discover(string testAssembly)
        => TestCaseDiscoverer.Discover(Settings, Logger, testAssembly);

    public void Execute(
        IReadOnlyCollection<TestAssemblyNode> testAssemblyNodes,
        ITestEventListener eventListener,
        IDebuggerFramework debuggerFramework)
    {
        using var sessionTimeoutCancellationSource = new CancellationTokenSource(Settings.SessionTimeout);
        lock (taskLock)
            cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(sessionTimeoutCancellationSource.Token);

        var tasks = new List<Task>();
        var semaphore = new SemaphoreSlim(Settings.MaxCpuCount);
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        try
        {
            foreach (var assemblyNode in testAssemblyNodes)
            {
                semaphore.Wait(cancellationSource.Token);

                // ReSharper disable once AccessToDisposedClosure
                var task = ExecuteTestsInAssembly(assemblyNode, eventListener, debuggerFramework, cancellationSource.Token)
                    .ContinueWith(_ => semaphore.Release(), cancellationSource.Token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                tasks.Add(task);
            }

            Task.WaitAll([.. tasks], cancellationSource.Token);
        }
        catch (OperationCanceledException)
        {
            // is running into session timeout we need to manually cancel the current test run
            if (sessionTimeoutCancellationSource.IsCancellationRequested)
            {
                stopwatch.Stop();
                Logger.LogInfo($"Test execution is stopped because of running into session timeout of {TimeSpan.FromMilliseconds(Settings.SessionTimeout)}.!");
                Logger.LogInfo(
                    $"""

                     ╔═══════════════════════ TEST SESSION TIMEOUT ═══════════════════════════════════════╗

                       Test execution exceeded maximum allowed time:
                         • Timeout: {TimeSpan.FromMilliseconds(Settings.SessionTimeout).Humanize()}
                         • Total tests: {TotalTests(testAssemblyNodes)}
                         • Completed tests: {eventListener.CompletedTests}
                         • Time elapsed: {stopwatch.Elapsed.Humanize()}

                       ACTION REQUIRED: Please increase 'TestSessionTimeout' in your '.runsettings' file

                     ╚════════════════════════════════════════════════════════════════════════════════════╝
                     """);

                // report the cause before cancelling, the cancelled Godot runtime would otherwise report its kill as the cause
                eventListener.ReportNotExecutedCause(TestIds(testAssemblyNodes), DescribeSessionTimeoutCause(Settings.SessionTimeout));
                Cancel();
            }

            try
            {
                // Wait for tasks to complete cancellation
                _ = Task.WaitAll([.. tasks], TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during cancellation cleanup: {ex.Message}");
                throw;
            }
        }
        catch (AggregateException ae)
        {
            foreach (var ex in ae.InnerExceptions)
                Logger.LogError($"Error executing tests: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error executing tests: {ex.Message}");
            throw;
        }
        finally
        {
            semaphore.Dispose();
            lock (taskLock)
            {
                cancellationSource.Dispose();
                cancellationSource = null;
            }
        }
    }

    internal static string DescribeSessionTimeoutCause(int sessionTimeout)
        => $"the test session ran into its timeout of {TimeSpan.FromMilliseconds(sessionTimeout).Humanize()}, increase 'TestSessionTimeout' in your '.runsettings' file";

    private static (List<TestSuiteNode> DirectExecutorTestSuites, List<TestSuiteNode> GodotExecutorTestSuites) SplitTestSuitesByRequiredRuntime(List<TestSuiteNode> testSuiteNodes)
    {
        var directExecutorTestSuites = new List<TestSuiteNode>();
        var godotExecutorTestSuites = new List<TestSuiteNode>();

        foreach (var suite in testSuiteNodes)
        {
            var godotTests = suite.Tests.FindAll(test => test.RequireRunningGodotEngine);
            var directTests = suite.Tests.FindAll(test => !test.RequireRunningGodotEngine);

            if (godotTests.Count > 0)
                godotExecutorTestSuites.Add(suite with { Tests = godotTests });
            if (directTests.Count > 0)
                directExecutorTestSuites.Add(suite with { Tests = directTests });
        }

        return (directExecutorTestSuites, godotExecutorTestSuites);
    }

    private static int TotalTests(IReadOnlyCollection<TestAssemblyNode> testAssemblyNodes)
    {
        var totalTests = 0;
        foreach (var assemblyNode in testAssemblyNodes)
            totalTests += assemblyNode.Suites.Sum(ts => ts.Tests.Count);
        return totalTests;
    }

    private static IEnumerable<Guid> TestIds(IReadOnlyCollection<TestAssemblyNode> testAssemblyNodes)
        => testAssemblyNodes.SelectMany(assemblyNode => assemblyNode.Suites).SelectMany(suite => suite.Tests).Select(test => test.Id);

    private Task ExecuteTestsInAssembly(
        TestAssemblyNode testAssemblyNode,
        ITestEventListener eventListener,
        IDebuggerFramework debuggerFramework,
        CancellationToken cancellationToken)
        => Task.Run(
            () =>
            {
                var assemblyId = Path.GetFileNameWithoutExtension(testAssemblyNode.AssemblyPath);
                using (loggerFactory.CreateScope(assemblyId))
                {
                    Logger.LogInfo($"Starting tests for assembly: {testAssemblyNode.AssemblyPath}");

                    var projectWorkingDir = LookupProjectPath(testAssemblyNode.AssemblyPath);
                    Directory.SetCurrentDirectory(projectWorkingDir);
                    Logger.LogInfo($"Set current working directory to: {projectWorkingDir}");

                    ExecuteEngineTests(assemblyId, testAssemblyNode.Suites, eventListener, debuggerFramework, cancellationToken);

                    Logger.LogInfo($"Completed tests for assembly: {testAssemblyNode.AssemblyPath}");
                }
            },
            cancellationToken);

    private void ExecuteEngineTests(
        string assemblyId,
        List<TestSuiteNode> testSuiteNodes,
        ITestEventListener eventListener,
        IDebuggerFramework debuggerFramework,
        CancellationToken cancellationToken)
    {
        var (directExecutorTestSuites, godotExecutorTestSuites) = SplitTestSuitesByRequiredRuntime(testSuiteNodes);

        // Run tests that require Godot runtime
        if (godotExecutorTestSuites.Count > 0)
        {
            var godotRunner = new GodotRuntimeTestRunner(assemblyId, debuggerFramework, Settings);
            ActiveTestRunners.Add(godotRunner);
            godotRunner.RunAndWait(godotExecutorTestSuites, eventListener, cancellationToken);
            _ = ActiveTestRunners.Remove(godotRunner);
        }

        // Run tests that don't require Godot runtime
        if (directExecutorTestSuites.Count > 0)
        {
            var directRunner = new DefaultTestRunner(Settings);
            ActiveTestRunners.Add(directRunner);
            directRunner.RunAndWait(directExecutorTestSuites, eventListener, cancellationToken);
            _ = ActiveTestRunners.Remove(directRunner);
        }
    }

    private string LookupProjectPath(string assemblyPath)
    {
        try
        {
            Logger.LogInfo($"Search '.csproj' at {assemblyPath}");
            var currentDir = new DirectoryInfo(assemblyPath).Parent;
            while (currentDir != null)
            {
                if (currentDir.EnumerateFiles("*.csproj").Any())
                    return currentDir.FullName;
                currentDir = currentDir.Parent;
            }

            throw new FileNotFoundException("Project file does not exist");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Unable to locate .csproj file: {ex.Message}");
            throw new FileNotFoundException("Project file does not exist");
        }
    }
}
