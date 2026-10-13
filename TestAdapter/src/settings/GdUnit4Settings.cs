// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.TestAdapter.Settings;

using System.Xml;
using System.Xml.Serialization;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

/// <summary>
///     Specifies how test case names are displayed in test results and Test Explorer.
/// </summary>
public enum DisplayNameOptions
{
    /// <summary>
    ///     Display only the simple method name (e.g., "TestMethod").
    /// </summary>
    SimpleName,

    /// <summary>
    ///     Display the fully qualified name including namespace, class, and method (e.g., "MyNamespace.MyClass.TestMethod").
    /// </summary>
    FullyQualifiedName
}

/// <summary>
///     Configuration settings for GdUnit4 test adapter that can be specified in .runsettings files.
///     This class extends VSTest's TestRunSettings to provide GdUnit4-specific configuration options
///     for test execution, display formatting, and Godot engine integration.
/// </summary>
/// <remarks>
///     These settings can be configured in a .runsettings file using the following format:
///     <code>
/// &lt;RunSettings&gt;
///   &lt;GdUnit4&gt;
///     &lt;DisplayName&gt;SimpleName&lt;/DisplayName&gt;
///     &lt;CaptureStdOut&gt;true&lt;/CaptureStdOut&gt;
///     &lt;Parameters&gt;--verbose --headless&lt;/Parameters&gt;
///     &lt;CompileProcessTimeout&gt;30000&lt;/CompileProcessTimeout&gt;
///     &lt;GodotConnectTimeout&gt;10000&lt;/GodotConnectTimeout&gt;
///     &lt;TolerateExitCodeAfterCompletion&gt;false&lt;/TolerateExitCodeAfterCompletion&gt;
///   &lt;/GdUnit4&gt;
/// &lt;/RunSettings&gt;
/// </code>
///     The settings control:
///     - Test case display formatting in Test Explorer and results
///     - Standard output capture for debugging purposes
///     - Additional parameters passed to the Godot engine during test execution
///     - Compilation timeout for projects that require extended build times
///     - Connect timeout for the Godot runtime host on slow or variable environments
///     - Tolerating a failing exit of the Godot runtime host after all test results are reported
///     These settings are loaded by <see cref="GdUnit4SettingsProvider" /> during test discovery and execution.
/// </remarks>
[XmlRoot(RUN_SETTINGS_XML_NODE)]
public class GdUnit4Settings : TestRunSettings
{
    /// <summary>
    ///     The XML node name used to identify GdUnit4 settings in .runsettings files.
    /// </summary>
    public const string RUN_SETTINGS_XML_NODE = "GdUnit4";

    private static readonly XmlSerializer Serializer = new(typeof(GdUnit4Settings));

    /// <summary>
    ///     Initializes a new instance of the <see cref="GdUnit4Settings" /> class.
    /// </summary>
    public GdUnit4Settings()
        : base(RUN_SETTINGS_XML_NODE)
    {
    }

    /// <summary>
    ///     Gets or sets additional Godot runtime parameters. These are passed to the Godot executable when running tests.
    /// </summary>
    /// <value>
    ///     A string containing command-line parameters for the Godot engine, such as "--verbose", "--headless", or custom flags.
    ///     Can be null or empty if no additional parameters are needed.
    /// </value>
    /// <example>
    ///     <code>
    /// Parameters = "--verbose --headless"
    /// </code>
    /// </example>
    public string? Parameters { get; set; }

    /// <summary>
    ///     Gets or sets the display name format for test cases in test results and Test Explorer.
    /// </summary>
    /// <value>
    ///     A <see cref="DisplayNameOptions" /> value that controls how test names appear.
    ///     Default is <see cref="DisplayNameOptions.SimpleName" />.
    /// </value>
    /// <remarks>
    ///     <para><see cref="DisplayNameOptions.SimpleName" />: Shows only the method name (e.g., "TestMethod").</para>
    ///     <para><see cref="DisplayNameOptions.FullyQualifiedName" />: Shows the complete path (e.g., "MyNamespace.MyClass.TestMethod").</para>
    /// </remarks>
    public DisplayNameOptions DisplayName { get; set; } = DisplayNameOptions.SimpleName;

    /// <summary>
    ///     Gets or sets a value indicating whether standard output (stdout) from test cases is captured and included in test results.
    /// </summary>
    /// <value>
    ///     <c>true</c> to capture standard output for debugging purposes; <c>false</c> to ignore stdout.
    ///     Default is <c>false</c>.
    /// </value>
    /// <remarks>
    ///     When enabled, any console output from tests (Console.WriteLine, Godot print statements, etc.)
    ///     will be captured and displayed in test results. This can be useful for debugging test failures
    ///     but may impact performance for tests with extensive output.
    /// </remarks>
    public bool CaptureStdOut { get; set; }

    /// <summary>
    ///     Gets the maximum duration allowed for a compilation process in milliseconds.
    /// </summary>
    /// <value>
    ///     The timeout value in milliseconds. Default is 120000 milliseconds (2 minutes).
    /// </value>
    /// <remarks>
    ///     <para>
    ///         This timeout applies to the compilation phase before test execution. If compilation takes longer
    ///         than this duration, the process will be forcefully terminated.
    ///     </para>
    ///     <para>
    ///         Increase this value for larger projects or slower build environments that require more time
    ///         to compile. Very large Godot projects with many dependencies may need timeouts of 60+ seconds.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     For a large project that requires more compilation time:
    ///     <code>
    /// CompileProcessTimeout = 60000; // 60 seconds
    /// </code>
    /// </example>
    public int CompileProcessTimeout { get; init; } = 120000;

    /// <summary>
    ///     Gets the maximum duration allowed to connect to the Godot runtime host in milliseconds.
    /// </summary>
    /// <value>
    ///     The timeout value in milliseconds. Default is 10000 milliseconds (10 seconds).
    /// </value>
    /// <remarks>
    ///     This timeout applies when connecting to the launched Godot runtime host over a named pipe
    ///     before running tests that require the Godot runtime. Increase it for environments where the
    ///     Godot host needs more time to start.
    /// </remarks>
    public int GodotConnectTimeout { get; init; } = 10000;

    /// <summary>
    ///     Gets or sets a value indicating whether a failing exit of the Godot runtime host is tolerated once all test results are reported.
    /// </summary>
    /// <value>
    ///     <c>true</c> to log such an exit as a warning; <c>false</c> to fail the test run.
    ///     Default is <c>false</c>.
    /// </value>
    /// <remarks>
    ///     The Godot runtime host can crash during its shutdown, for example with <c>SIGSEGV</c> or <c>SIGABRT</c>
    ///     in the .NET runtime teardown, after every test result was already reported. A failing exit before all
    ///     test results are reported always fails the test run.
    /// </remarks>
    public bool TolerateExitCodeAfterCompletion { get; set; }

    /// <summary>
    ///     Converts the current settings instance to an XML element for inclusion in .runsettings files.
    /// </summary>
    /// <returns>
    ///     An <see cref="XmlElement" /> representing the serialized settings that can be embedded
    ///     in VSTest run settings configuration.
    /// </returns>
    /// <remarks>
    ///     This method is called by the VSTest framework when processing .runsettings files.
    ///     The returned XML element will be used to reconstruct the settings during test execution.
    /// </remarks>
    public override XmlElement ToXml()
    {
        using var stringWriter = new StringWriter();
        Serializer.Serialize(stringWriter, this);

        var document = new XmlDocument();
        document.LoadXml(stringWriter.ToString());

        return document.DocumentElement!;
    }
}
