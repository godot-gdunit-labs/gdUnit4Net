// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.TestAdapter;

using System.Xml;

using Api;

using Discovery;

using Execution;

using Extensions;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

using Settings;

using Utilities;

/// <summary>
///     Visual Studio Test Platform adapter executor for GdUnit4 test framework.
///     This class implements the VSTest adapter pattern by implementing <see cref="ITestExecutor2" />
///     to execute GdUnit4 tests through Visual Studio Test Explorer, dotnet test, and other VSTest runners.
/// </summary>
/// <remarks>
///     This adapter executor:
///     - Executes GdUnit4 tests discovered by <see cref="GdUnit4TestDiscoverer" />
///     - Integrates with VSTest execution lifecycle (run, cancel, debug)
///     - Supports both individual test execution and full assembly scanning
///     - Handles test filtering, grouping, and result reporting
///     - Manages GdUnit4 test engine lifecycle and configuration
///     - Provides debugging support for different IDEs (JetBrains Rider detection)
///     - Supports parallel test execution and timeout configuration
///     - Converts VSTest TestCase objects back to GdUnit4 test nodes for execution
///     The executor works in conjunction with the discoverer to provide a complete
///     VSTest adapter implementation that bridges GdUnit4 tests with the VSTest platform.
/// </remarks>
[ExtensionUri(EXECUTOR_URI)]
public class GdUnit4TestExecutor : ITestExecutor2, IDisposable
{
    /// <summary>
    ///     The Uri used to identify the GdUnit4 Executor.
    /// </summary>
    public const string EXECUTOR_URI = "executor://GdUnit4.TestAdapter";

    /// <summary>
    ///     The default test session timeout is set to 10min (600000ms).
    /// </summary>
    private const int DEFAULT_SESSION_TIMEOUT = 600000;

    private IFrameworkHandle? fh;
    private ITestEngine? testEngine;
    private volatile bool isCanceled;

#pragma warning disable CA1859
    private ITestEngineLogger? Log { get; set; }
#pragma warning restore CA1859

    /// <summary>
    ///     Releases resources used by the test executor, including the GdUnit4 test engine.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Executes specific GdUnit4 tests provided as VSTest TestCase objects.
    ///     This method is called when running selected tests from Test Explorer or filtered test runs.
    /// </summary>
    /// <param name="tests">Collection of VSTest TestCase objects to execute.</param>
    /// <param name="runContext">VSTest run context containing settings and configuration.</param>
    /// <param name="frameworkHandle">VSTest framework handle for result reporting and operations.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when any of the required parameters (tests, runContext, frameworkHandle) are null.
    /// </exception>
    public void RunTests(IEnumerable<TestCase>? tests, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        _ = tests ?? throw new ArgumentNullException(nameof(tests), "Argument 'tests' is null, abort!");
        _ = runContext ?? throw new ArgumentNullException(nameof(runContext), "Argument 'runContext' is null, abort!");
        fh = frameworkHandle ?? throw new ArgumentNullException(nameof(frameworkHandle), "Argument 'frameworkHandle' is null, abort!");

        var testCases = tests.ToList();
        if (testCases.Count == 0)
            return;

        Log = new Logger(frameworkHandle);
        isCanceled = false;
        if (ITestEngine.EngineVersion() < GdUnit4TestDiscoverer.MinRequiredGdUnit4ApiVersion)
        {
            Log.LogError(
                $"Wrong GdUnit4Api, Version={ITestEngine.EngineVersion()} found, you need to upgrade to minimum version: '{GdUnit4TestDiscoverer.MinRequiredGdUnit4ApiVersion}'");
            Log.LogError("Abort the test discovery.");
            return;
        }

        var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(runContext.RunSettings?.SettingsXml);
        var settings = GdUnit4SettingsProvider.LoadSettings(runContext);
        var engineSettings = new TestEngineSettings
        {
            CaptureStdOut = settings.CaptureStdOut,
            Parameters = settings.Parameters,
            MaxCpuCount = Math.Max(1, runConfiguration.MaxCpuCount),
            SessionTimeout = (int)(runConfiguration.TestSessionTimeout == 0
                ? DEFAULT_SESSION_TIMEOUT
                : runConfiguration.TestSessionTimeout),
            CompileProcessTimeout = settings.CompileProcessTimeout,
            GodotConnectTimeout = settings.GodotConnectTimeout,
            TolerateExitCodeAfterCompletion = settings.TolerateExitCodeAfterCompletion
        };

        testEngine = ITestEngine.GetInstance(engineSettings, Log);
        Log.LogInfo(
            runConfiguration.TestSessionTimeout == 0
                ? $"No 'TestSessionTimeout' is specified. Set default test session timeout to: {TimeSpan.FromMilliseconds(DEFAULT_SESSION_TIMEOUT)}"
                : $"Set test session timeout to: {TimeSpan.FromMilliseconds(engineSettings.SessionTimeout)}");

        try
        {
            SetupRunnerEnvironment(runContext, frameworkHandle);
            testCases = new TestCaseFilter(runContext, Log).Execute(testCases);
            var testsByAssembly = ToGdUnitTestNodes(testCases);
            var testEventListener = new TestEventReportListener(frameworkHandle, testCases);
            var debuggerFramework = GetDebuggerFramework(frameworkHandle);
            testEngine.Execute(testsByAssembly, testEventListener, debuggerFramework);

            // a test without a result was dropped by the test engine, report it instead of letting the test run pass silently
            if (!isCanceled)
                testEventListener.ReportNotExecutedTests();
            Log.LogInfo("Test execution stopped.");
        }
        catch (Exception ex)
        {
            Log.LogError($"Test execution failed: {ex.Message}");
            Log.LogError(ex.StackTrace ?? "No stack trace available");
            throw;
        }
    }

    /// <summary>
    ///     Discovers and executes all GdUnit4 tests found in the specified assembly sources.
    ///     This method is called when running all tests in assemblies or when no specific tests are selected.
    /// </summary>
    /// <param name="sources">Collection of assembly file paths to scan for tests.</param>
    /// <param name="runContext">VSTest run context containing settings and configuration.</param>
    /// <param name="frameworkHandle">VSTest framework handle for result reporting and operations.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when any of the required parameters (sources, runContext, frameworkHandle) are null.
    /// </exception>
    /// <remarks>
    ///     This method combines discovery and execution by:
    ///     1. Using <see cref="GdUnit4TestDiscoverer" /> to find all tests in the provided assemblies
    ///     2. Delegating to the specific test execution method with the discovered tests
    ///     This approach ensures consistent behavior between "Run All" and "Run Selected" scenarios
    ///     while avoiding code duplication.
    /// </remarks>
    public void RunTests(IEnumerable<string>? sources, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        _ = sources ?? throw new ArgumentNullException(nameof(sources), "Argument 'containers' is null, abort!");
        _ = runContext ?? throw new ArgumentNullException(nameof(runContext), "Argument 'runContext' is null, abort!");
        fh = frameworkHandle ?? throw new ArgumentNullException(nameof(frameworkHandle), "Argument 'frameworkHandle' is null, abort!");

        TestCaseDiscoverySink discoverySink = new();
        new GdUnit4TestDiscoverer().DiscoverTests(sources, runContext, fh, discoverySink);
        if (discoverySink.TestCases.Count == 0)
            return;
        RunTests(discoverySink.TestCases, runContext, frameworkHandle);
    }

    /// <summary>
    ///     Cancels the currently running test execution.
    ///     This method is called when the user requests cancellation through the VSTest platform.
    /// </summary>
    public void Cancel()
    {
        Log?.LogInfo("User has canceled the test execution.");
        isCanceled = true;
        testEngine?.Cancel();
    }

    /// <summary>
    ///     Determines whether the test host should attach to a debugger when running tests from assembly sources.
    ///     Always returns true to enable debugging support.
    /// </summary>
    /// <param name="sources">Assembly sources being tested (not used in decision).</param>
    /// <param name="runContext">Run context (not used in decision).</param>
    /// <returns>Always returns true to enable debugger attachment.</returns>
    public bool ShouldAttachToTestHost(IEnumerable<string>? sources, IRunContext runContext) => true;

    /// <summary>
    ///     Determines whether the test host should attach to a debugger when running specific tests.
    ///     Always returns true to enable debugging support.
    /// </summary>
    /// <param name="tests">Test cases being executed (not used in decision).</param>
    /// <param name="runContext">Run context (not used in decision).</param>
    /// <returns>Always returns true to enable debugger attachment.</returns>
    public bool ShouldAttachToTestHost(IEnumerable<TestCase>? tests, IRunContext runContext) => true;

    /// <summary>
    ///     Sets up environment variables from VSTest run settings for the test execution environment.
    /// </summary>
    /// <param name="runContext">VSTest run context containing settings with environment variables.</param>
    /// <param name="frameworkHandle">Framework handle for error reporting.</param>
    /// <remarks>
    ///     Environment variables configured in the test run settings (.runsettings file) are applied
    ///     to the current process before test execution. This allows tests to access custom configuration
    ///     through environment variables.
    /// </remarks>
    internal static void SetupRunnerEnvironment(IRunContext runContext, IFrameworkHandle frameworkHandle)
    {
        try
        {
            foreach (var variable in RunSettingsProvider.GetEnvironmentVariables(runContext.RunSettings?.SettingsXml))
                Environment.SetEnvironmentVariable(variable.Key, variable.Value);
        }
        catch (XmlException ex)
        {
            frameworkHandle.SendMessage(TestMessageLevel.Error, "Error while setting environment variables: " + ex.Message);
        }
    }

    /// <summary>
    ///     Converts VSTest TestCase objects to GdUnit4 test node hierarchy for execution.
    /// </summary>
    /// <param name="testCases">Collection of VSTest TestCase objects to convert.</param>
    /// <returns>List of test assembly nodes organized in GdUnit4's hierarchical test structure.</returns>
    internal static List<TestAssemblyNode> ToGdUnitTestNodes(IEnumerable<TestCase> testCases) =>

        // Group test cases by assembly path
        [
            .. testCases
                .GroupBy(tc => tc.Source)
                .Select(assemblyGroup =>
                {
                    // Create the assembly node
                    var assembly = new TestAssemblyNode
                    {
                        Id = Guid.NewGuid(),
                        ParentId = Guid.Empty,
                        AssemblyPath = assemblyGroup.Key,
                        Suites = []
                    };

                    // Group test cases by managed type (suites), the source file alone is not unique
                    // because it is 'unknown' for all test cases when the assembly is built with full PDBs
                    var suites = assemblyGroup
                        .GroupBy(t => (ManagedType: t.GetPropertyValue(TestCaseExtensions.ManagedTypeProperty, string.Empty), t.CodeFilePath))
                        .Select(tests =>
                        {
                            var suite = new TestSuiteNode
                            {
                                Id = Guid.NewGuid(),
                                ParentId = assembly.Id,
                                ManagedType = tests.Key.ManagedType,
                                AssemblyPath = assembly.AssemblyPath,
                                SourceFile = tests.Key.CodeFilePath ?? "Unknown",
                                Tests = []
                            };

                            suite.Tests.AddRange(
                            [
                                .. tests
                                    .Select(test => new TestCaseNode
                                    {
                                        Id = test.Id,
                                        ParentId = suite.Id,
                                        ManagedMethod = test.GetPropertyValue(TestCaseExtensions.ManagedMethodProperty, string.Empty),
                                        AttributeIndex = test.GetPropertyValue(TestCaseExtensions.ManagedMethodAttributeIndexProperty, 0),
                                        LineNumber = test.LineNumber,
                                        RequireRunningGodotEngine = test.GetPropertyValue(TestCaseExtensions.RequireRunningGodotEngineProperty, false)
                                    })
                            ]);

                            return suite;
                        });

                    assembly.Suites.AddRange(suites);
                    return assembly;
                })
        ];

    /// <summary>
    ///     Releases resources used by the test executor, including the GdUnit4 test engine.
    /// </summary>
    /// <param name="disposing">.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            testEngine?.Dispose();
            testEngine = null;
        }
    }

    /// <summary>
    ///     Creates an appropriate debugger framework implementation based on the detected IDE.
    /// </summary>
    /// <param name="frameworkHandle">VSTest framework handle for IDE detection.</param>
    /// <returns>IDE-specific debugger framework implementation.</returns>
    /// <remarks>
    ///     Currently supports special handling for JetBrains Rider, with fallback to default implementation.
    ///     Different IDEs may require different debugging integration approaches.
    /// </remarks>
    private static IDebuggerFramework GetDebuggerFramework(IFrameworkHandle frameworkHandle)
        => IsJetBrainsRider(frameworkHandle)
            ? new RiderDebuggerFramework(frameworkHandle)
            : new DefaultDebuggerFramework(frameworkHandle);

    /// <summary>
    ///     Detects if the test execution is running within JetBrains Rider IDE.
    /// </summary>
    /// <param name="frameworkHandle">VSTest framework handle for type inspection.</param>
    /// <returns>True if JetBrains Rider is detected; otherwise, false.</returns>
    private static bool IsJetBrainsRider(IFrameworkHandle frameworkHandle)
    {
        var version = frameworkHandle.GetType().Assembly.GetName().Version;
        if (frameworkHandle is not IFrameworkHandle2
            || !frameworkHandle.GetType().ToString().Contains("JetBrains", StringComparison.Ordinal)
            || version < new Version("2.16.1.14"))
            return false;

        frameworkHandle.SendMessage(TestMessageLevel.Informational, $"JetBrains Rider detected {version}");
        return true;
    }
}
