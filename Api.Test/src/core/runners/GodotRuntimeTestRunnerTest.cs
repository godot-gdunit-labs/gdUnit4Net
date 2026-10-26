namespace GdUnit4.Tests.Core.Runners;

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

using Api;

using GdUnit4.Core.Projects;
using GdUnit4.Core.Runners;

using Moq;

using Projects;

using static Assertions;
using static GdUnit4.Asserts.IStringAssert.Compare;

/// <summary>
///     Tests for GodotRuntimeTestRunner's InstallTestRunnerClasses method.
/// </summary>
[TestSuite]
public class GodotRuntimeTestRunnerTest
{
    // TODO implement the TempDirectory annotation
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

    /// <summary>
    ///     Test a Godot binary that cannot be started, the process is closed without an asynchronous output read to cancel.
    /// </summary>
    [TestCase]
    public void ReCompileGodotProjectWithMissingBinary()
    {
        var workingDirectory = Path.Combine(TestTempDirectory!, "working_dir_missing_binary");
        Directory.CreateDirectory(workingDirectory);

        using var capture = LogCapture.Watch<GodotRuntimeTestRunner>();

        var result = CreateTestRunner(1000).ReCompileGodotProject(workingDirectory, Path.Combine(TestTempDirectory!, "missing_godot_binary"));

        AssertThat(result).OverrideFailureMessage("ReCompileGodotProject should return false when the binary cannot be started").IsFalse();
        AssertThat(capture.EntriesOf(LogLevel.Error).Any(e => e.Message.StartsWith("Rebuilding Godot Project fails with:", StringComparison.Ordinal)))
            .OverrideFailureMessage(FormatFailure(capture, LogLevel.Error, "Rebuilding Godot Project fails with:"))
            .IsTrue();
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

    [TestCase("4.6.stable.mono.official.89cea1439", true)]
    [TestCase("4.7.stable.mono.official.5b4e0cb0f\n", true)]
    [TestCase("4.8.dev7.mono.custom_build.c971f93e7", true)]
    [TestCase("4.6.2.stable.official.71f334935", false)]
    [TestCase("4.8.dev7.official.c971f93e7", false)]
    [TestCase("4.6.stable.official.monotonic", false)]
    [TestCase("", false)]
    public void IsGodotCSharpBuild(string version, bool expected)
        => AssertThat(GodotRuntimeTestRunner.IsGodotCSharpBuild(version)).IsEqual(expected);

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

        // the build is killed while it runs, so it must not hand work to build servers outside its process tree
        using var buildServers = DisableBuildServers();

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

    [TestCase(139, false, "the Godot runtime exited with exit code 139 (SIGSEGV) before all test results were reported")]
    [TestCase(139, true, "the Godot runtime exited with exit code 139 before all test results were reported")]
    [TestCase(0, false, "the Godot runtime exited with exit code 0 before all test results were reported")]
    public void DescribeNotExecutedCause(int exitCode, bool isWindows, string expected)
        => AssertThat(GodotRuntimeTestRunner.DescribeNotExecutedCause(exitCode, isWindows)).IsEqual(expected);

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

    [TestCase]
    public void NewPipeNameIsUniquePerRun()
    {
        var pipeNames = Enumerable.Range(0, 1000).Select(_ => GodotRuntimeTestRunner.NewPipeName()).ToList();

        AssertThat(pipeNames.Distinct()).HasSize(1000);
    }

    [TestCase]
    public void NewPipeNameFitsTheMacOSSocketPathLimit()
    {
        var pipeName = GodotRuntimeTestRunner.NewPipeName();

        AssertThat(pipeName)
            .StartsWith("gdunit4-")
            .HasLength(8 + GodotRuntimeTestRunner.PIPE_NAME_RANDOM_LENGTH);

        // .NET creates the pipe socket in the macOS temp directory, its path is limited to 104 bytes
        AssertThat($"/var/folders/xx/{new string('x', 32)}/T/CoreFxPipe_{pipeName}")
            .HasLength(104, LESS_THAN);
    }

    #region PrepareReferencedGodotProjects

    [TestCase]
    public void PrepareReferencedGodotProjectsWithoutReferences()
    {
        var assemblyPath = Path.Combine(TestTempDirectory!, "Test.dll");
        ProjectReferenceAssembly.Write(assemblyPath);
        var runner = CreateTestRunner(1000);

        using var packs = runner.PrepareReferencedGodotProjects([assemblyPath], TestTempDirectory!, MockGodotBinPath);
        AssertThat(packs!.Files).IsEmpty();
        AssertThat(Directory.Exists(GodotProjectReference.WorkingDir(TestTempDirectory!))).IsFalse();
    }

    [TestCase]
    public void PrepareReferencedGodotProjectsFailsOnAMissingProject()
    {
        var testProjectDir = Path.Combine(TestTempDirectory!, "Game.Test");
        var assemblyPath = Path.Combine(TestTempDirectory!, "Test.dll");
        ProjectReferenceAssembly.Write(assemblyPath, "../Game");
        var runner = CreateTestRunner(1000);
        using var capture = LogCapture.Watch<GodotRuntimeTestRunner>();

        AssertObject(runner.PrepareReferencedGodotProjects([assemblyPath], testProjectDir, MockGodotBinPath)).IsNull();
        VerifyLoggerError(capture, $"The referenced Godot project '{Path.Combine(TestTempDirectory!, "Game")}' has no 'project.godot'.");
    }

    [TestCase]
    public void PrepareReferencedGodotProjectsSkipsAnUnreadableAssembly()
    {
        var assemblyPath = Path.Combine(TestTempDirectory!, "Test.dll");
        File.WriteAllText(assemblyPath, "no assembly");
        var runner = CreateTestRunner(1000);
        using var capture = LogCapture.Watch<GodotRuntimeTestRunner>();

        using var packs = runner.PrepareReferencedGodotProjects([assemblyPath], TestTempDirectory!, MockGodotBinPath);

        AssertThat(packs!.Files).IsEmpty();
        AssertThat(capture.EntriesOf(LogLevel.Error)).IsEmpty();
        AssertThat(capture.EntriesOf(LogLevel.Warning).Any(entry => entry.Message.StartsWith($"The referenced Godot projects of '{assemblyPath}' can't be read: ", StringComparison.Ordinal)))
            .OverrideFailureMessage(FormatFailure(capture, LogLevel.Warning, $"The referenced Godot projects of '{assemblyPath}' can't be read: ..."))
            .IsTrue();
    }

    [TestCase]
    public void PrepareReferencedGodotProjectsPacksAnImportedProjectWithoutImport()
    {
        var testProjectDir = Path.Combine(TestTempDirectory!, "Game.Test");
        var reference = new GodotProjectReference(Path.Combine(TestTempDirectory!, "Game"));
        foreach (var entry in new[] { "project.godot", "icon.svg", ".godot/imported/icon.svg-1234.ctex", ".godot/uid_cache.bin" })
        {
            var path = Path.Combine(reference.ProjectDir, entry);
            _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, string.Empty);
        }

        GodotProjectPacker.WriteImportStamp(reference, GodotProjectPacker.Scan(reference.ProjectDir), MockGodotBinPath);
        var assemblyPath = Path.Combine(TestTempDirectory!, "Test.dll");
        ProjectReferenceAssembly.Write(assemblyPath, "../Game");
        var runner = CreateTestRunner(1000);
        using var capture = LogCapture.Watch<GodotRuntimeTestRunner>();

        // the mock Godot binary does not exist, an import would fail
        using var packs = runner.PrepareReferencedGodotProjects([assemblyPath], testProjectDir, MockGodotBinPath);
        AssertThat(packs!.Files).HasSize(1);
        AssertThat(File.Exists(packs.Files[0])).IsTrue();
        AssertThat(Directory.GetFiles(GodotProjectReference.WorkingDir(testProjectDir), "*.inuse")).HasSize(1);

        packs.Dispose();

        AssertThat(Directory.GetFiles(GodotProjectReference.WorkingDir(testProjectDir), "*.inuse")).IsEmpty();
        VerifyLoggerInfo(capture, $"The assets of the referenced Godot project '{reference.ProjectDir}' are up to date.");
    }

    [TestCase]
    public void PrepareReferencedGodotProjectsReadsTheTestProjectAssemblyOnly()
    {
        var testProjectDir = Path.Combine(TestTempDirectory!, "Core.Test");
        _ = Directory.CreateDirectory(testProjectDir);
        File.WriteAllText(Path.Combine(testProjectDir, "project.godot"), "[dotnet]\nproject/assembly_name=\"Core.Test\"\n");
        var helperAssembly = Path.Combine(TestTempDirectory!, "Helper.dll");
        ProjectReferenceAssembly.Write(helperAssembly, "../Core");
        var runner = CreateTestRunner(1000);
        using var capture = LogCapture.Watch<GodotRuntimeTestRunner>();

        using var packs = runner.PrepareReferencedGodotProjects([helperAssembly], testProjectDir, MockGodotBinPath);

        AssertThat(packs!.Files).IsEmpty();
        AssertThat(capture.EntriesOf(LogLevel.Error)).IsEmpty();
    }

    [TestCase]
    public void PrepareReferencedGodotProjectsReadsTheTestProjectAssemblyNextToAFilteredHelperAssembly()
    {
        var testProjectDir = Path.Combine(TestTempDirectory!, "Level.Test");
        _ = Directory.CreateDirectory(testProjectDir);
        File.WriteAllText(Path.Combine(testProjectDir, "project.godot"), "[dotnet]\nproject/assembly_name=\"Level.Test\"\n");
        var reference = new GodotProjectReference(Path.Combine(TestTempDirectory!, "Level"));
        _ = Directory.CreateDirectory(reference.ProjectDir);
        File.WriteAllText(Path.Combine(reference.ProjectDir, "project.godot"), string.Empty);
        GodotProjectPacker.WriteImportStamp(reference, GodotProjectPacker.Scan(reference.ProjectDir), MockGodotBinPath);
        var outputDir = Path.Combine(TestTempDirectory!, "bin");
        _ = Directory.CreateDirectory(outputDir);
        ProjectReferenceAssembly.Write(Path.Combine(outputDir, "Level.Test.dll"), "../Level");
        var helperAssembly = Path.Combine(outputDir, "Helper.dll");
        ProjectReferenceAssembly.Write(helperAssembly, "../Other");
        var runner = CreateTestRunner(1000);
        using var capture = LogCapture.Watch<GodotRuntimeTestRunner>();

        using var packs = runner.PrepareReferencedGodotProjects([helperAssembly], testProjectDir, MockGodotBinPath);

        AssertThat(packs!.Files).HasSize(1);
        AssertThat(Path.GetFileName(packs.Files[0])).StartsWith($"{reference.Id}-");
        AssertThat(capture.EntriesOf(LogLevel.Error)).IsEmpty();
    }

    [TestCase]
    public void PrepareReferencedGodotProjectsListsTheFirstCollisions()
    {
        var testProjectDir = Path.Combine(TestTempDirectory!, "Game.Test");
        var reference = new GodotProjectReference(Path.Combine(TestTempDirectory!, "Game"));
        var shared = Enumerable.Range(1, 12).Select(index => $"Items/item_{index:00}.tres").ToList();
        foreach (var entry in shared.Concat(["project.godot", ".godot/uid_cache.bin"]))
        {
            var path = Path.Combine(reference.ProjectDir, entry);
            _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, string.Empty);
        }

        foreach (var entry in shared)
        {
            var path = Path.Combine(testProjectDir, entry);
            _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, string.Empty);
        }

        GodotProjectPacker.WriteImportStamp(reference, GodotProjectPacker.Scan(reference.ProjectDir), MockGodotBinPath);
        var assemblyPath = Path.Combine(TestTempDirectory!, "Test.dll");
        ProjectReferenceAssembly.Write(assemblyPath, "../Game");
        var runner = CreateTestRunner(1000);
        using var capture = LogCapture.Watch<GodotRuntimeTestRunner>();

        using var packs = runner.PrepareReferencedGodotProjects([assemblyPath], testProjectDir, MockGodotBinPath);

        var listed = string.Join('\n', shared.Take(10).Select(entry => $"  res://{entry}"));
        AssertThat(capture.EntriesOf(LogLevel.Warning).Select(entry => entry.Message)).ContainsExactly(
            $"The referenced Godot project '{reference.ProjectDir}' contains 12 files that also exist in the test project, "
            + $"the files of the test project are used:\n{listed}\n  ... and 2 more");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    ///     Runs the `dotnet build` started by the runner without the persistent build servers until the returned scope is disposed.
    ///     The shared compiler server and reused MSBuild nodes are no child processes of the build, so the timeout kill does not stop them,
    ///     and they keep build output files like `ExampleProject.sourcelink.json` open while the test deletes the working directory.
    ///     The build inherits the environment of the test process, and MSBuild reads environment variables as properties.
    /// </summary>
    private static EnvironmentScope DisableBuildServers()
        => new(("MSBUILDDISABLENODEREUSE", "1"), ("UseSharedCompilation", "false"));

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

    /// <summary>
    ///     Sets environment variables of the test process and restores their previous values on dispose.
    /// </summary>
    private sealed class EnvironmentScope : IDisposable
    {
        private readonly (string Name, string? Value)[] previous;

        public EnvironmentScope(params (string Name, string Value)[] variables)
        {
            previous = variables.Select(variable => (variable.Name, Environment.GetEnvironmentVariable(variable.Name))).ToArray();
            foreach (var (name, value) in variables)
                Environment.SetEnvironmentVariable(name, value);
        }

        public void Dispose()
        {
            foreach (var (name, value) in previous)
                Environment.SetEnvironmentVariable(name, value);
        }
    }

    #endregion
}
