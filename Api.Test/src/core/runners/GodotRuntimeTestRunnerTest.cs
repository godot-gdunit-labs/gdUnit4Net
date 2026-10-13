namespace GdUnit4.Tests.Core.Runners;

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

using Api;

using GdUnit4.Core.Runners;

using Moq;

using static Assertions;

/// <summary>
///     Tests for GodotRuntimeTestRunner's InstallTestRunnerClasses method.
/// </summary>
[TestSuite]
public class GodotRuntimeTestRunnerTest
{
    // TODO implement the TempDirectory annotation
    // [TempDirectory]
    private string? TestTempDirectory { get; set; }

    public required string MockGodotBinPath { get; set; }

    public required Mock<IDebuggerFramework> DebuggerFrameworkMock { get; set; }

    /// <summary>
    ///     Set up test environment and create mocks.
    /// </summary>
    [Before]
    public void Before()
    {
        // TODO remove if [TempDirectory] implemented
        TestTempDirectory = Path.Combine(Path.GetTempPath(), "test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(TestTempDirectory);

        // Create mock Godot executable - we'll use a batch script for Windows or shell script for Unix
        // Windows batch script
        MockGodotBinPath = Path.Combine(TestTempDirectory, Environment.OSVersion.Platform == PlatformID.Win32NT
            ? "mock_godot.bat"

            // Unix shell script
            : "mock_godot.sh");

        DebuggerFrameworkMock = new Mock<IDebuggerFramework>();
    }

    private GodotRuntimeTestRunner CreateTestRunner(int compileTimeout) => new(
        "assemblyId",
        DebuggerFrameworkMock.Object,
        new TestEngineSettings { CompileProcessTimeout = compileTimeout });

    /// <summary>
    ///     Clean up after tests.
    /// </summary>
    [After]
    public void After()
    {
        try
        {
            // Clean up temp directory if it wasn't handled by the annotation
            // TODO remove if [TempDirectory] implemented
            if (TestTempDirectory == null || !Directory.Exists(TestTempDirectory))
                return;

            Directory.Delete(TestTempDirectory, true);
        }
        catch
        {
            // Ignore cleanup failures
        }
    }

    [AfterTest]
    public void AfterTest() => DebuggerFrameworkMock.Reset();

    /// <summary>
    ///     Test successful execution of InstallTestRunnerClasses.
    /// </summary>
    [TestCase]
    public void ReCompileGodotProject()
    {
        // Arrange
        CreateSuccessScript();

        // Create a separate temp working directory
        var workingDirectory = Path.Combine(TestTempDirectory!, "working_dir");
        Directory.CreateDirectory(workingDirectory);

        using var capture = LogCapture.Watch<GodotRuntimeTestRunner>();

        // Act
        var result = CreateTestRunner(1000).ReCompileGodotProject(workingDirectory, MockGodotBinPath);

        // Assert
        AssertThat(result).OverrideFailureMessage("ReCompileGodotProject should return true for successful compilation").IsTrue();

        // Verify logger was called with success messages
        VerifyLoggerInfo(capture, "Rebuilding Godot Project ...");
        VerifyLoggerInfo(capture, "Rebuilding Godot Project ends with exit code: 0");
    }

    /// <summary>
    ///     Test a process that takes nearly the full timeout but still completes successfully.
    /// </summary>
    [TestCase]
    public void ReCompileGodotProjectNearTimeout()
    {
        // Create a script that runs for 4 seconds (near the timeout but should complete)
        CreateNearTimeoutScript();

        // Create a separate temp working directory
        var workingDirectory = Path.Combine(TestTempDirectory!, "working_dir_near_timeout");
        Directory.CreateDirectory(workingDirectory);

        using var capture = LogCapture.Watch<GodotRuntimeTestRunner>();

        // Act, Set a longer timeout for this test
        var result = CreateTestRunner(5000).ReCompileGodotProject(workingDirectory, MockGodotBinPath);

        // Assert
        AssertThat(result).OverrideFailureMessage("ReCompileGodotProject should return true for a process that completes just before timeout").IsTrue();

        // Verify success messages were logged
        VerifyLoggerInfo(capture, "Rebuilding Godot Project ...");
        VerifyLoggerInfo(capture, "Rebuilding Godot Project ends with exit code: 0");

        // Verify that no timeout error was logged
        AssertThat(capture.EntriesOf(LogLevel.Error).Any(e => e.Message.Contains("Godot compilation TIMEOUT")))
            .OverrideFailureMessage("Timeout error should not have been logged").IsFalse();
    }

    /// <summary>
    ///     Test timeout scenario in InstallTestRunnerClasses.
    /// </summary>
    [TestCase]
    public void ReCompileGodotProjectTimeout()
    {
        // Arrange
        CreateTimeoutScript();

        // Create a separate temp working directory
        var workingDirectory = Path.Combine(TestTempDirectory!, "working_dir_timeout");
        Directory.CreateDirectory(workingDirectory);

        using var capture = LogCapture.Watch<GodotRuntimeTestRunner>();

        // Act
        var result = CreateTestRunner(1000).ReCompileGodotProject(workingDirectory, MockGodotBinPath);

        // Assert
        AssertThat(result).OverrideFailureMessage("ReCompileGodotProject should return false on timeout").IsFalse();

        // Verify timeout error was logged
        VerifyLoggerError(capture, "Godot compilation TIMEOUT");
        var errorCode = Environment.OSVersion.Platform == PlatformID.Win32NT ? -1 : 137;
        VerifyLoggerError(capture, $"Rebuilding Godot Project ends with exit code: {errorCode}");

        // Verify the runner file was cleaned up after timeout
        var runnerPath = Path.Combine(workingDirectory, GodotRuntimeTestRunner.TEMP_TEST_RUNNER_DIR, "GdUnit4TestRunnerScene.cs");
        AssertThat(File.Exists(runnerPath)).OverrideFailureMessage("Runner file should be cleaned up after timeout").IsFalse();
    }

    [TestCase]
    public void VerifyGodotCSharpSupport()
    {
        // Create a separate temp working directory
        var workingDirectory = Path.Combine(TestTempDirectory!, "working_dir_timeout");
        Directory.CreateDirectory(workingDirectory);

        // Act
        var godotPath = Environment.GetEnvironmentVariable("GODOT_BIN");
        AssertThat(File.Exists(godotPath)).IsTrue();
        var result = CreateTestRunner(1000).VerifyGodotCSharpSupport(godotPath!);

        // Assert
        AssertThat(result).OverrideFailureMessage("VerifyGodotCSharpSupport should succeed").IsTrue();
    }

    /// <summary>
    ///     Test compilation failure scenario.
    /// </summary>
    [TestCase]
    public void TestInstallTestRunnerCompilationFailure()
    {
        // Create a separate temp working directory
        var workingDirectory = Path.Combine(TestTempDirectory!, "working_dir_failure");
        Directory.CreateDirectory(workingDirectory);

        using var capture = LogCapture.Watch<GodotRuntimeTestRunner>();

        // Act, the working directory has no project file, so `dotnet build` fails on its own.
        // The timeout is generous so a slow `dotnet` start is not killed and reported as a timeout instead.
        var result = CreateTestRunner(30000).InstallTestRunnerClasses(workingDirectory);

        // Assert
        AssertThat(result).OverrideFailureMessage("InstallTestRunnerClasses should return false on compilation failure").IsFalse();

        // Verify the build failed by itself and was not killed by the timeout
        AssertThat(capture.EntriesOf(LogLevel.Error).Any(e => e.Message.Contains("did not complete within the configured timeout", StringComparison.Ordinal)))
            .OverrideFailureMessage("Timeout error should not have been logged").IsFalse();
        VerifyLoggerError(capture, "dotnet build failed with exit code: 1");

        // Verify the runner file was cleaned up after failure
        var runnerPath = Path.Combine(workingDirectory, GodotRuntimeTestRunner.TEMP_TEST_RUNNER_DIR, "GdUnit4TestRunnerScene.cs");
        AssertThat(File.Exists(runnerPath)).OverrideFailureMessage("Runner file should be cleaned up after compilation failure").IsFalse();
    }

    [TestCase]
    public void TestInstallTestRunnerSuccess()
    {
        // Create a separate temp working directory
        var workingDirectory = Path.Combine(Environment.CurrentDirectory, "../TestExample");

        try
        {
            // Copy Example Project into working directory
            CopyExampleProject(workingDirectory);

            using var capture = LogCapture.Watch<GodotRuntimeTestRunner>();

            // Act
            var result = CreateTestRunner(30000).InstallTestRunnerClasses(workingDirectory);

            // Assert
            AssertThat(result).OverrideFailureMessage("InstallTestRunnerClasses should return true").IsTrue();

            // Verify log message
            VerifyLoggerInfo(capture, "Installing GdUnit4TestRunner at");
            VerifyLoggerInfo(capture, "dotnet build completed successfully");

            // Verify the runner file was created in the correct location
            var runnerPath = Path.Combine(workingDirectory, GodotRuntimeTestRunner.TEMP_TEST_RUNNER_DIR, "GdUnit4TestRunnerScene.cs");
            AssertThat(File.Exists(runnerPath)).OverrideFailureMessage($"Runner file should exist at {runnerPath}").IsTrue();
        }
        finally
        {
            // Cleanup
            Directory.Delete(workingDirectory, true);
        }
    }

    [TestCase]
    public void TestInstallTestRunnerFailsByBuildTimeout()
    {
        // Create a separate temp working directory
        var workingDirectory = Path.Combine(Environment.CurrentDirectory, "../TestExample");

        try
        {
            // Copy Example Project into working directory
            CopyExampleProject(workingDirectory);

            using var capture = LogCapture.Watch<GodotRuntimeTestRunner>();

            // Act
            var result = CreateTestRunner(1000).InstallTestRunnerClasses(workingDirectory);

            // Assert
            AssertThat(result).OverrideFailureMessage("InstallTestRunnerClasses should fail").IsFalse();

            VerifyLoggerInfo(capture, "Installing GdUnit4TestRunner at");

            // Verify error messages
            VerifyLoggerError(capture, "`dotnet build` did not complete within the configured timeout of 1000ms.");
            var dotnetExitCode = Environment.OSVersion.Platform == PlatformID.Win32NT ? -1 : 137;
            VerifyLoggerError(capture, $"dotnet build failed with exit code: {dotnetExitCode}");
        }
        finally
        {
            // Cleanup
            Directory.Delete(workingDirectory, true);
        }
    }

    [TestCase]
    public void DescribeProcessExitSuccess()
    {
        var (level, message) = GodotRuntimeTestRunner.DescribeProcessExit(0, false, false, false, ["some error output"]);

        AssertThat(level).IsEqual(LogLevel.Informational);
        AssertThat(message).IsEqual("GodotRuntimeTestRunner ends with exit code: 0");
    }

    [TestCase]
    public void DescribeProcessExitBeforeCompletion()
    {
        var (level, message) = GodotRuntimeTestRunner.DescribeProcessExit(139, false, true, false, []);

        // a failing exit before all results are reported is an error, even when tolerated after completion
        AssertThat(level).IsEqual(LogLevel.Error);
        AssertThat(message).IsEqual("GodotRuntimeTestRunner ends with exit code: 139 (SIGSEGV), before all test results were reported.");
    }

    [TestCase]
    public void DescribeProcessExitAfterCompletion()
    {
        var (level, message) = GodotRuntimeTestRunner.DescribeProcessExit(134, true, false, false, []);

        AssertThat(level).IsEqual(LogLevel.Error);
        AssertThat(message).IsEqual(
            "GodotRuntimeTestRunner ends with exit code: 134 (SIGABRT), after all test results were reported. "
            + "To report this exit as a warning, set <TolerateExitCodeAfterCompletion>true</TolerateExitCodeAfterCompletion> "
            + "in the <GdUnit4> section of your .runsettings.");
    }

    [TestCase]
    public void DescribeProcessExitAfterCompletionTolerated()
    {
        var (level, message) = GodotRuntimeTestRunner.DescribeProcessExit(134, true, true, false, []);

        AssertThat(level).IsEqual(LogLevel.Warning);
        AssertThat(message).IsEqual(
            "GodotRuntimeTestRunner ends with exit code: 134 (SIGABRT), after all test results were reported. "
            + "The exit is tolerated because 'TolerateExitCodeAfterCompletion' is enabled.");
    }

    [TestCase]
    public void DescribeProcessExitOnWindows()
    {
        var (level, message) = GodotRuntimeTestRunner.DescribeProcessExit(139, false, false, true, []);

        // Windows exit codes do not encode a signal
        AssertThat(level).IsEqual(LogLevel.Error);
        AssertThat(message).IsEqual("GodotRuntimeTestRunner ends with exit code: 139, before all test results were reported.");
    }

    [TestCase]
    public void DescribeProcessExitWithErrorOutput()
    {
        var (_, message) = GodotRuntimeTestRunner.DescribeProcessExit(
            139,
            true,
            true,
            false,
            ["handle_crash: Program crashed with signal 11", "[1] /lib/x86_64-linux-gnu/libc.so.6(+0x42520)"]);

        AssertThat(message).IsEqual(
            """
            GodotRuntimeTestRunner ends with exit code: 139 (SIGSEGV), after all test results were reported. The exit is tolerated because 'TolerateExitCodeAfterCompletion' is enabled.
            Last error output of the Godot runtime host:
              handle_crash: Program crashed with signal 11
              [1] /lib/x86_64-linux-gnu/libc.so.6(+0x42520)
            """.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [TestCase(134, false, " (SIGABRT)")]
    [TestCase(137, false, " (SIGKILL)")]
    [TestCase(139, false, " (SIGSEGV)")]
    [TestCase(143, false, " (SIGTERM)")]
    [TestCase(150, false, " (signal 22)")]
    [TestCase(139, true, "")]
    [TestCase(1, false, "")]
    [TestCase(128, false, "")]
    [TestCase(255, false, "")]
    public void DescribeSignal(int exitCode, bool isWindows, string expected)
        => AssertThat(GodotRuntimeTestRunner.DescribeSignal(exitCode, isWindows)).IsEqual(expected);

    #region Helper Methods

    private static void CopyExampleProject(string destDir)
    {
        var sourceDir = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "../Example"));
        Directory.CreateDirectory(destDir);
        var files = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            if (relativePath.StartsWith(".godot") || relativePath.StartsWith("gdunit4_testadapter_v5"))
                continue;

            var destFile = Path.Combine(destDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
            File.Copy(file, destFile, true);
        }
    }

    /// <summary>
    ///     Create a script that succeeds quickly.
    /// </summary>
    private void CreateSuccessScript()
    {
        var content = Environment.OSVersion.Platform == PlatformID.Win32NT
            ?

            // Windows batch file that succeeds immediately
            """
            @echo off

            echo Godot Engine v4.1.stable.mono - https://godotengine.org
            echo Godot Engine v4.1.stable.mono
            echo Running...
            echo Compilation successful!
            exit 0
            """
            :

            // Unix shell script that succeeds immediately
            """
            #!/bin/bash

            echo 'Godot Engine v4.1.stable.mono - https://godotengine.org'
            echo 'Godot Engine v4.1.stable.mono'
            echo 'Running...'
            echo 'Compilation successful!'
            exit 0
            """;

        File.WriteAllText(MockGodotBinPath, content);
        SetAsExecutable(MockGodotBinPath);
    }

    /// <summary>
    ///     Create a script that takes nearly the full timeout (4 seconds) but still completes.
    /// </summary>
    private void CreateNearTimeoutScript()
    {
        var content = Environment.OSVersion.Platform == PlatformID.Win32NT
            ?

            // Windows batch file that sleeps for 4 seconds
            """
            @echo off

            echo Godot Engine v4.1.stable.mono - https://godotengine.org
            echo Godot Engine v4.1.stable.mono
            echo Running...
            echo Starting compilation (will take 4 seconds)...
            ping 127.0.0.1 -n 5 > nul
            echo Compilation complete!
            exit 0
            """
            :

            // Unix shell script that sleeps for 4 seconds
            """
            #!/bin/bash

            echo 'Godot Engine v4.1.stable.mono - https://godotengine.org'
            echo 'Godot Engine v4.1.stable.mono'
            echo 'Running...'
            echo 'Starting compilation (will take 4 seconds)...'
            sleep 5
            echo 'Compilation complete!'
            exit 0
            """;

        File.WriteAllText(MockGodotBinPath, content);
        SetAsExecutable(MockGodotBinPath);
    }

    /// <summary>
    ///     Create a script that times out.
    /// </summary>
    private void CreateTimeoutScript()
    {
        var content = Environment.OSVersion.Platform == PlatformID.Win32NT
            ?

            // Windows batch file that sleeps longer than our timeout
            """
            @echo off

            echo Godot Engine v4.1.stable.mono - https://godotengine.org
            echo Godot Engine v4.1.stable.mono
            echo Running...
            ping 127.0.0.1 -n 10 > nul
            exit 0
            """
            :

            // Unix shell script that sleeps longer than our timeout
            """
            #!/bin/bash

            echo 'Godot Engine v4.1.stable.mono - https://godotengine.org'
            echo 'Godot Engine v4.1.stable.mono'
            echo 'Running...'
            sleep 10
            exit 0
            """;

        File.WriteAllText(MockGodotBinPath, content);
        SetAsExecutable(MockGodotBinPath);
    }

    [SuppressMessage("Interoperability", "CA1416", Justification = "Guarded by the platform check, runs only on Unix")]
    private static void SetAsExecutable(string filePath)
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            File.SetUnixFileMode(
                filePath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }
    }

    private static void VerifyLoggerInfo(LogCapture capture, string expectedText)
        => AssertThat(capture.EntriesOf(LogLevel.Informational).Any(e => e.Message.Equals(expectedText, StringComparison.Ordinal)))
            .OverrideFailureMessage(FormatFailure(capture, LogLevel.Informational, expectedText))
            .IsTrue();

    private static void VerifyLoggerError(LogCapture capture, string expectedText)
        => AssertThat(capture.EntriesOf(LogLevel.Error).Any(e => e.Message.Equals(expectedText, StringComparison.Ordinal)))
            .OverrideFailureMessage(FormatFailure(capture, LogLevel.Error, expectedText))
            .IsTrue();

    private static string FormatFailure(LogCapture capture, LogLevel level, string expectedText)
    {
        var entries = capture.EntriesOf(level);
        return $"Expected {level} message equal to '{expectedText}' was not found.\n" +
               $"Actual {level} messages ({entries.Count}):\n" +
               string.Join("\n", entries.Select((e, i) => $"  {i + 1}. {e.Message}"));
    }

    #endregion
}
