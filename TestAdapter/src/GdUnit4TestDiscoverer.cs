// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.TestAdapter;

using System.ComponentModel;

using Api;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

using Settings;

using Utilities;

using static Extensions.TestCaseExtensions;

using TestCaseDescriptor = Core.Discovery.TestCaseDescriptor;

/// <summary>
///     Visual Studio Test Platform adapter discoverer for GdUnit4 test framework.
///     This class implements the VSTest adapter pattern by implementing <see cref="ITestDiscoverer" />
///     to integrate GdUnit4 tests with Visual Studio Test Explorer, dotnet test, and other VSTest runners.
/// </summary>
/// <remarks>
///     This adapter discoverer:
///     - Enables GdUnit4 tests to appear in Visual Studio Test Explorer
///     - Allows running GdUnit4 tests via 'dotnet test' command
///     - Integrates with CI/CD pipelines using VSTest platform
///     - Converts GdUnit4 test descriptors into VSTest TestCase objects
///     - Filters out conflicting Microsoft test framework assemblies
///     - Requires minimum GdUnit4 engine version 4.4.0 for compatibility
///     The class is decorated with VSTest adapter attributes that register it as a test discoverer
///     for .dll and .exe files in the managed code category.
/// </remarks>
[DefaultExecutorUri(GdUnit4TestExecutor.EXECUTOR_URI)]
[ExtensionUri(GdUnit4TestExecutor.EXECUTOR_URI)]
[FileExtension(".dll")]
[FileExtension(".exe")]
[Category("managed")]
public sealed class GdUnit4TestDiscoverer : ITestDiscoverer
{
    /// <summary>
    ///     Gets the minimum required GdUnit4 API version needed for test discovery.
    /// </summary>
    /// <value>The minimum version (5.0.0) required for compatibility.</value>
    internal static Version MinRequiredGdUnit4ApiVersion { get; } = new("5.0.0");

#pragma warning disable CA1859
    private ITestEngineLogger Logger { get; set; } = null!;

#pragma warning restore CA1859

    /// <summary>
    ///     Discovers GdUnit4 tests in assemblies and reports them to the VSTest platform.
    ///     This method is called by VSTest runners (Visual Studio, dotnet test, etc.) during test discovery phase.
    /// </summary>
    /// <param name="sources">Collection of assembly paths to discover tests from.</param>
    /// <param name="discoveryContext">VSTest discovery context containing settings and configuration.</param>
    /// <param name="logger">VSTest logger instance for reporting discovery messages and errors.</param>
    /// <param name="discoverySink">VSTest sink for sending discovered test cases back to the test platform.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when any of the required parameters (logger, discoveryContext, discoverySink) are null.
    /// </exception>
    /// <remarks>
    ///     VSTest discovery workflow:
    ///     1. Validates GdUnit4 engine version compatibility
    ///     2. Loads GdUnit4-specific test discovery settings from VSTest context
    ///     3. Filters assemblies to exclude conflicting Microsoft test frameworks
    ///     4. Uses GdUnit4 test engine to discover test cases in each valid assembly
    ///     5. Converts GdUnit4 test descriptors to VSTest TestCase objects
    ///     6. Sends discovered tests back to VSTest platform via the discovery sink
    ///     This enables GdUnit4 tests to appear in Test Explorer and be executed by VSTest runners.
    /// </remarks>
    public void DiscoverTests(
        IEnumerable<string> sources,
        IDiscoveryContext discoveryContext,
        IMessageLogger logger,
        ITestCaseDiscoverySink discoverySink)
    {
        _ = logger ?? throw new ArgumentNullException(nameof(logger), "Argument 'logger' is null, abort!");
        _ = discoveryContext ?? throw new ArgumentNullException(nameof(discoveryContext), "Argument 'discoveryContext' is null, abort!");
        _ = discoverySink ?? throw new ArgumentNullException(nameof(discoverySink), "Argument 'discoverySink' is null, abort!");

        try
        {
            Logger = new Logger(logger);
            var assembly = typeof(GdUnit4TestDiscoverer).Assembly;
            var name = assembly.GetName();
            var version = name.Version;
            var bitness = Environment.Is64BitProcess ? "64-bit" : "32-bit";
            var runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            Logger.LogInfo($"VSTest Adapter: {name.Name} {version} ({bitness} {runtime})");
            if (ITestEngine.EngineVersion() < MinRequiredGdUnit4ApiVersion)
            {
                Logger.LogError($"Wrong GdUnit4Api, Version={ITestEngine.EngineVersion()} found, you need to upgrade to minimum version: '{MinRequiredGdUnit4ApiVersion}'");
                Logger.LogError("Abort the test discovery.");
                return;
            }

            var settings = GdUnit4SettingsProvider.LoadSettings(discoveryContext);
            var engineSettings = new TestEngineSettings
            {
                CaptureStdOut = settings.CaptureStdOut,
                Parameters = settings.Parameters
            };
            var testEngine = ITestEngine.GetInstance(engineSettings, Logger);
            Logger.LogInfo($"Running on GdUnit4 test engine version: {ITestEngine.EngineVersion()}");
            var filteredAssembles = FilterWithoutTestAdapter(sources);

            foreach (var assemblyPath in filteredAssembles)
            {
                try
                {
                    testEngine.Discover(assemblyPath)
                        .ToList()
                        .ConvertAll(testCase => BuildTestCase(testCase, settings))
                        .OrderBy(t => t.FullyQualifiedName)
                        .ToList()
                        .ForEach(discoverySink.SendTestCase);
                }

                // User test code boundary: discovering a test assembly of the user fails in many ways, the other assemblies are still discovered
#pragma warning disable CA1031
                catch (Exception e)
#pragma warning restore CA1031
                {
                    Logger.LogError($"Error discovering tests for assembly {assemblyPath}: {e}");
                }
            }
        }

        // VSTest boundary: the test platform calls the discovery, an exception escaping it aborts the discovery without a message to the user
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            logger.SendMessage(TestMessageLevel.Error, $"Error loading test settings: {e}");
        }
    }

    /// <summary>
    ///     Builds a VSTest TestCase object from a GdUnit4 test descriptor.
    ///     This conversion enables GdUnit4 tests to be executed by the VSTest platform.
    /// </summary>
    /// <param name="descriptor">The GdUnit4 test case descriptor containing test metadata.</param>
    /// <param name="settings">GdUnit4 settings that influence TestCase creation and display.</param>
    /// <returns>A VSTest TestCase object configured for execution by the test platform.</returns>
    /// <remarks>
    ///     The VSTest TestCase includes:
    ///     - Fully qualified name for unique identification
    ///     - GdUnit4 executor URI for proper test execution routing
    ///     - Assembly path for test location and loading
    ///     - Display name formatted according to user preferences
    ///     - Source code location (file path and line number) for Test Explorer navigation
    ///     - Additional GdUnit4-specific properties for test execution context
    ///     This mapping allows VSTest runners to properly display, filter, and execute GdUnit4 tests.
    /// </remarks>
    internal static TestCase BuildTestCase(TestCaseDescriptor descriptor, GdUnit4Settings settings)
    {
        TestCase testCase = new(descriptor.FullyQualifiedName, new Uri(GdUnit4TestExecutor.EXECUTOR_URI), descriptor.AssemblyPath)
        {
            Id = descriptor.Id,
            DisplayName = GetDisplayName(descriptor, settings),
            CodeFilePath = descriptor.CodeFilePath,
            LineNumber = descriptor.LineNumber
        };

        testCase.SetPropertyValues(descriptor);

        return testCase;
    }

    /// <summary>
    ///     Determines the display name for a test case based on user settings.
    /// </summary>
    /// <param name="input">The test case descriptor containing name information.</param>
    /// <param name="gdUnitSettings">Settings that specify how test names should be displayed.</param>
    /// <returns>The formatted display name according to the specified settings.</returns>
    /// <remarks>
    ///     Display name options:
    ///     - SimpleName: Uses the simple name from the descriptor
    ///     - FullyQualifiedName: Uses the last part of the fully qualified name (after the last dot)
    ///     - Default: Uses the managed method name.
    /// </remarks>
    private static string GetDisplayName(TestCaseDescriptor input, GdUnit4Settings gdUnitSettings)
        => gdUnitSettings.DisplayName switch
        {
            DisplayNameOptions.SimpleName => input.SimpleName,
            DisplayNameOptions.FullyQualifiedName => input.FullyQualifiedName[(input.FullyQualifiedName.LastIndexOf('.') + 1)..],
            _ => input.ManagedMethod
        };

    /// <summary>
    ///     Filters out Microsoft and MSTest assemblies from the list of assemblies to discover.
    /// </summary>
    /// <param name="assemblyPaths">Collection of assembly paths to filter.</param>
    /// <returns>Filtered collection excluding Microsoft test framework assemblies.</returns>
    /// <remarks>
    ///     This method excludes assemblies containing "MSTest" or "Microsoft" in their path
    ///     to avoid conflicts with other test frameworks and focus on GdUnit4 tests only.
    /// </remarks>
    private static IEnumerable<string> FilterWithoutTestAdapter(IEnumerable<string> assemblyPaths) =>
        assemblyPaths.Where(assembly =>
            !assembly.Contains("MSTest", StringComparison.Ordinal)
            && !assembly.Contains("Microsoft", StringComparison.Ordinal));
}
