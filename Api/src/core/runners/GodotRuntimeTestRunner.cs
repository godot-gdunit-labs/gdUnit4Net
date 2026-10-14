// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Runners;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

using Api;

using Execution;

using Logging;

/// <summary>
///     Test runner implementation that executes tests in a separate Godot runtime process.
///     Handles process management, IPC, and test execution coordination with the Godot engine.
/// </summary>
[SuppressMessage(
    "Reliability",
    "CA2000:Dispose objects before losing scope",
    Justification = "GodotRuntimeExecutor ownership is transferred to base class which handles disposal")]
internal sealed class GodotRuntimeTestRunner : BaseTestRunner
{
    /// <summary>
    ///     Directory name for temporary test runner files.
    /// </summary>
    internal const string TEMP_TEST_RUNNER_DIR = "gdunit4_testadapter_v5";

    /// <summary>
    ///     Number of the last error output lines of the Godot runtime host that are reported on a failing exit.
    /// </summary>
    internal const int STD_ERR_TAIL_LINES = 30;

    /// <summary>
    ///     Maximum time in milliseconds to wait for `godot --help` to exit when checking the C# support of the Godot binary.
    ///     It covers a slow first start of Godot, for example on a fresh CI runner.
    /// </summary>
    internal const int VERIFY_GODOT_TIMEOUT = 30000;

    private static readonly ITestEngineLogger Logger = LoggerFactory.GetLogger<GodotRuntimeTestRunner>();
    private readonly string pipeName;

    private readonly ScopeLogger scope;
    private readonly TestEngineSettings settings;
    private readonly ProcessOutputTail stdErrTail = new(STD_ERR_TAIL_LINES);
    private TaskCompletionSource stdErrClosed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Process? process;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GodotRuntimeTestRunner" /> class.
    /// </summary>
    /// <param name="assemblyId">Assembly identifier used to create a unique pipe name, preventing collisions when running multiple assemblies in parallel.</param>
    /// <param name="debuggerFramework">Framework for debugging support.</param>
    /// <param name="settings">Test engine configuration settings.</param>
    internal GodotRuntimeTestRunner(string assemblyId, IDebuggerFramework debuggerFramework, TestEngineSettings settings)
        : base(new GodotRuntimeExecutor($"gdunit4-{assemblyId}", settings.GodotConnectTimeout), settings)
    {
        pipeName = $"gdunit4-{assemblyId}";
        scope = LoggerFactory.Instance.GetScope() ?? new ScopeLogger(Logger, assemblyId);
        this.settings = settings;
        DebuggerFramework = debuggerFramework;
    }

    private object ProcessLock { get; } = new();

    private IDebuggerFramework DebuggerFramework { get; }

    /// <summary>
    ///     Gets the path to the Godot executable from environment variables.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when Godot executable path is not configured or found.</exception>
    private string GodotBin
    {
        get
        {
            var godotPath = Environment.GetEnvironmentVariable("GODOT_BIN");
            if (string.IsNullOrEmpty(godotPath))
            {
                var message = "Godot runtime is not configured. The environment variable 'GODOT_BIN' is not set or empty. Please set it to the Godot executable path.";
                Logger.LogError(message);
                throw new InvalidOperationException(message);
            }

            if (File.Exists(godotPath))
                return godotPath;

            var errorMessage = $"The Godot executable was not found at path: {godotPath}";
            Logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }
    }

    public override void Cancel()
    {
        base.Cancel();
        lock (ProcessLock)
        {
            process?.Kill(true);
            _ = process?.WaitForExit(1000);
            CloseProcess(process);
        }
    }

    public new void RunAndWait(List<TestSuiteNode> testSuiteNodes, ITestEventListener eventListener, CancellationToken cancellationToken)
    {
        lock (ProcessLock)
        {
            var godotBinary = GodotBin;
            if (!VerifyGodotCSharpSupport(godotBinary))
                return;

            if (!InstallTestRunnerClasses(Environment.CurrentDirectory))
                return;

            if (!ReCompileGodotProject(Environment.CurrentDirectory, godotBinary))
                return;
            Logger.LogInfo("Starting GodotRuntimeTestRunner");

            var processStartInfo =
                new ProcessStartInfo(godotBinary, BuildGodotArguments())
                {
                    StandardOutputEncoding = Encoding.Default,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = false,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = Environment.CurrentDirectory
                };

            Logger.LogInfo($"Arguments: {processStartInfo.Arguments}");

            stdErrTail.Clear();
            stdErrClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            if (DebuggerFramework.IsDebugProcess)
                process = DebuggerFramework.LaunchProcessWithDebuggerAttached(processStartInfo);
            else
            {
                process = new Process
                {
                    StartInfo = processStartInfo,
                    EnableRaisingEvents = true
                };
                var godotLogger = scope.WithSource("Godot");
                process.OutputDataReceived += godotLogger.Output;
                process.ErrorDataReceived += godotLogger.Error;
                process.ErrorDataReceived += CollectStdErr;
                _ = process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                if (DebuggerFramework.IsDebugAttach)
                    _ = DebuggerFramework.AttachDebuggerToProcess(process);
            }

            base.RunAndWait(testSuiteNodes, eventListener, cancellationToken);

            _ = process.WaitForExit(2000);

            // wait until the process has finished
            var waitRetry = 0;
            while (!process.HasExited && waitRetry++ < 10)
                Thread.Sleep(100);

            // If the process not finished until 10 retries, we kill it manually
            if (!process.HasExited)
            {
                Logger.LogWarning("GodotRuntimeTestRunner is not terminated, force process kill.");
                process.Kill(true);
            }

            if (!DebuggerFramework.IsDebugProcess && process.WaitForExit(2000))
            {
                // read the error output to the end, a crash backtrace is written right before the exit
                _ = stdErrClosed.Task.Wait(2000, CancellationToken.None);
                ReportProcessExit(process.ExitCode);
            }

            CloseProcess(process);
        }
    }

    /// <summary>
    ///     Describes how the exit of the Godot runtime host is reported.
    /// </summary>
    /// <param name="exitCode">The exit code of the Godot runtime host.</param>
    /// <param name="isRunCompleted">Whether all test suites were executed and reported before the exit.</param>
    /// <param name="tolerateExitCodeAfterCompletion">Whether a failing exit after all test results is reported as a warning.</param>
    /// <param name="isWindows">Whether the host runs on Windows, where exit codes do not encode signals.</param>
    /// <param name="errorOutput">The last error output lines of the Godot runtime host.</param>
    /// <returns>The log level and the message to report.</returns>
    internal static (LogLevel Level, string Message) DescribeProcessExit(
        int exitCode,
        bool isRunCompleted,
        bool tolerateExitCodeAfterCompletion,
        bool isWindows,
        IReadOnlyCollection<string> errorOutput)
    {
        var exit = $"GodotRuntimeTestRunner ends with exit code: {exitCode}";
        if (exitCode == 0)
            return (LogLevel.Informational, exit);

        exit += DescribeSignal(exitCode, isWindows);
        var (level, message) = (isRunCompleted, tolerateExitCodeAfterCompletion) switch
        {
            (false, _) => (LogLevel.Error, $"{exit}, before all test results were reported."),
            (true, true) => (LogLevel.Warning,
                $"{exit}, after all test results were reported. The exit is tolerated because 'TolerateExitCodeAfterCompletion' is enabled."),
            (true, false) => (LogLevel.Error,
                $"{exit}, after all test results were reported. To report this exit as a warning, set <TolerateExitCodeAfterCompletion>true</TolerateExitCodeAfterCompletion> in the <GdUnit4> section of your .runsettings.")
        };

        if (errorOutput.Count > 0)
            message += $"\nLast error output of the Godot runtime host:\n{string.Join('\n', errorOutput.Select(line => $"  {line}"))}";
        return (level, message);
    }

    /// <summary>
    ///     Names the signal that terminated a process, as encoded by .NET on Unix in the exit code (128 + signal number).
    /// </summary>
    /// <param name="exitCode">The exit code of the process.</param>
    /// <param name="isWindows">Whether the process ran on Windows, where exit codes do not encode signals.</param>
    /// <returns>The signal in parentheses, or an empty string when the exit code does not encode a signal.</returns>
    internal static string DescribeSignal(int exitCode, bool isWindows)
    {
        var signal = exitCode - 128;
        if (isWindows || signal is < 1 or > 64)
            return string.Empty;

        var name = signal switch
        {
            2 => "SIGINT",
            4 => "SIGILL",
            6 => "SIGABRT",
            7 => "SIGBUS",
            8 => "SIGFPE",
            9 => "SIGKILL",
            11 => "SIGSEGV",
            13 => "SIGPIPE",
            15 => "SIGTERM",
            _ => $"signal {signal}"
        };
        return $" ({name})";
    }

    internal bool VerifyGodotCSharpSupport(string godotBinary, int timeout = VERIFY_GODOT_TIMEOUT)
    {
        using var godotProcess = new Process();
        using var outputComplete = new ManualResetEventSlim(false);
        try
        {
            // recompile the project
            var processStartInfo = new ProcessStartInfo($"{godotBinary}", "--help")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                RedirectStandardInput = false,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Environment.CurrentDirectory
            };

            var hasCSharpOptions = false;
            godotProcess.StartInfo = processStartInfo;
            godotProcess.EnableRaisingEvents = true;
            godotProcess.OutputDataReceived += (_, args) =>
            {
                if (args.Data == null)
                    outputComplete.Set();
                var message = args.Data?.Trim();
                if (!string.IsNullOrEmpty(message))
                    hasCSharpOptions = hasCSharpOptions || message.Contains("--build-solutions", StringComparison.OrdinalIgnoreCase);
            };

            if (!godotProcess.Start())
            {
                Logger.LogError("Checking Godot C# supports fails on process start, exit ..");
                return false;
            }

            godotProcess.BeginOutputReadLine();

            // the exit code is only available after the process has exited, a slow first Godot start needs more time
            if (!godotProcess.WaitForExit(timeout))
            {
                Logger.LogError($"Checking Godot C# support fails, '{godotBinary} --help' did not exit within {timeout}ms.");
                godotProcess.Kill(true);
                _ = godotProcess.WaitForExit(1000);
                return false;
            }

            // the output can still be processed after the exit, wait for its end before evaluating it
            _ = outputComplete.Wait(2000);

            if (!hasCSharpOptions)
            {
                Logger.LogWarning(
                    $"""

                     ╔═════════════════════ NO Godot C# SUPPORT NOT DETECTED ═══════════════════════════════╗

                       The Godot binary at '{godotBinary}' does not appear to support C# development.

                       SOLUTION:
                       Please ensure you're using a Godot build with C# support:

                        1. Verify your .runsettings file (Recommended for projects):
                           Add the GODOT_BIN setting to <EnvironmentVariables>:

                             <RunSettings>
                                 <RunConfiguration>
                                     <EnvironmentVariables>
                                         <GODOT_BIN>D:\path\to\Godot_v4.x-stable_mono_win64.exe</GODOT_BIN>
                                     </EnvironmentVariables>
                                 </RunConfiguration>
                             </RunSettings>

                       2. Or set the GODOT_BIN environment variable:
                          - Windows: set GODOT_BIN=C:\path\to\Godot_v4.x-stable_mono_win64.exe
                          - Linux:   export GODOT_BIN=/path/to/Godot_v4.x-stable_mono_linux.x86_64
                          - macOS:   export GODOT_BIN=/path/to/Godot.app/Contents/MacOS/Godot

                       RESULT:
                       All Godot runtime tests will be skipped until C# support is available.

                     ╚═══════════════════════════════════════════════════════════════════════════════════════╝

                     """);
                return false;
            }

            return godotProcess.ExitCode == 0;
        }
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            Logger.LogError($"Verifying the Godot binary fails with: {e.Message}\n {e.StackTrace}");
            return false;
        }
        finally
        {
            CloseProcess(godotProcess);
        }
    }

    internal bool InstallTestRunnerClasses(string workingDirectory, bool reCompile = true)
    {
        var destinationFolderPath = Path.Combine(workingDirectory, @$"{TEMP_TEST_RUNNER_DIR}");
        if (!Directory.Exists(destinationFolderPath))
            _ = Directory.CreateDirectory(destinationFolderPath);

        var sceneRunnerSource = Path.Combine(destinationFolderPath, "GdUnit4TestRunnerScene.cs");

        // check if the scene runner already installed
        if (File.Exists(sceneRunnerSource))
            return true;

        Logger.LogInfo("Installing GdUnit4TestRunner at");
        Logger.LogInfo(destinationFolderPath);

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("GdUnit4.src.core.runners.GdUnit4TestRunnerSceneTemplate.cs");
        using var reader = new StreamReader(stream!);
        var content = reader.ReadToEnd();
        content = content.Replace("GdUnit4TestRunnerSceneTemplate", "GdUnit4TestRunnerScene", StringComparison.Ordinal);
        File.WriteAllText(sceneRunnerSource, content, Encoding.UTF8);

        if (!reCompile)
            return true;
        var isSuccess = RunDotnetRestore(workingDirectory);
        if (!isSuccess)
            CleanupRunnerOnFailure(sceneRunnerSource);
        return isSuccess;
    }

    internal bool ReCompileGodotProject(string workingDirectory, string godotBinary)
    {
        using var compileProcess = new Process();
        try
        {
            // recompile the project
            var processStartInfo = new ProcessStartInfo($"{godotBinary}", @"--path . -e --headless --quit-after 100 --verbose")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = false,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = workingDirectory
            };

            Logger.LogInfo($"Set working dir {workingDirectory}");
            Logger.LogInfo("Rebuilding Godot Project ...");
            Logger.LogInfo($"{godotBinary} {processStartInfo.Arguments}");
            compileProcess.StartInfo = processStartInfo;
            compileProcess.EnableRaisingEvents = true;
            var compileLogger = scope.WithSource("Godot");
            compileProcess.OutputDataReceived += compileLogger.Output;
            compileProcess.ErrorDataReceived += compileLogger.Error;
            compileProcess.Exited += ExitHandler("Rebuilding Godot Project");
            if (!compileProcess.Start())
            {
                Logger.LogError("Rebuilding Godot Project fails on process start, exit ..");
                return false;
            }

            compileProcess.BeginErrorReadLine();
            compileProcess.BeginOutputReadLine();
            _ = compileProcess.WaitForExit(100);

            // The compile project can take a while, and we need to wait until it finishes
            // Calculate how many iterations we need based on the compile process timeout
            const int checkIntervalMs = 100; // Check every 100ms
            var maxRetries = settings.CompileProcessTimeout / checkIntervalMs;

            var waitRetry = 0;
            while (!compileProcess.HasExited && waitRetry++ < maxRetries)
                Thread.Sleep(checkIntervalMs);

            // If the process has not finished within the timeout period, we kill it manually
            if (!compileProcess.HasExited)
            {
                Logger.LogError("Godot compilation TIMEOUT");
                Logger.LogError(
                    $"""

                     ╔═══════════════════════ Godot compilation TIMEOUT ═════════════════════════════════════════════════════════════════════╗

                       Godot project compilation did not complete within the configured timeout of {settings.CompileProcessTimeout}ms.

                       Possible reasons:
                       - Your Godot project may be large or complex, requiring more time to compile
                       - Your system may be under heavy load or has limited resources
                       - There might be a compilation issue causing Godot to hang

                       ACTION REQUIRED:
                       To increase the compilation timeout, set the 'CompileProcessTimeout' property in your GdUnit4 settings.

                       Add or modify the following in your .runsettings file:
                       <GdUnit4>
                           <CompileProcessTimeout>60000</CompileProcessTimeout>  <!-- 60 seconds -->
                       </GdUnit4>

                       The process will now be forcefully terminated, which may result in incomplete compilation.

                     ╚══════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
                     """);

                compileProcess.Kill(true);
            }

            // Ensure all async events (including Exited) are fully processed before reading ExitCode
            compileProcess.WaitForExit();
            return compileProcess.ExitCode == 0;
        }
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            Logger.LogError($"Rebuilding Godot Project fails with: {e.Message}\n {e.StackTrace}");

            return false;
        }
        finally
        {
            CloseProcess(compileProcess);
        }
    }

    private string BuildGodotArguments()
        => $"--path . -s res://{TEMP_TEST_RUNNER_DIR}/GdUnit4TestRunnerScene.cs {settings.Parameters} --pipe-name {pipeName}";

    private bool RunDotnetRestore(string workingDirectory)
    {
        try
        {
            Logger.LogInfo("Running dotnet build to ensure dependencies are available...");
            const string arguments = "build --configuration Debug "
                                     + "--verbosity normal "
                                     + "/p:BuildProjectReferences=false " // Don't rebuild project refs
                                     + "/p:_GetChildProjectCopyToOutputDirectoryItems=false " // Don't copy child project items
                                     + "/p:SkipCopyingFrameworkReferences=true " // Skip framework refs (already present)
                                     + "/p:EnforceCodeStyleInBuild=false "
                                     + "/p:TreatWarningsAsErrors=false ";
            var processStartInfo = new ProcessStartInfo("dotnet", arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = workingDirectory
            };

            using var restoreProcess = new Process();
            restoreProcess.StartInfo = processStartInfo;
            restoreProcess.EnableRaisingEvents = true;

            var dotnetLogger = scope.WithSource("dotnet");
            restoreProcess.OutputDataReceived += dotnetLogger.Output;
            restoreProcess.ErrorDataReceived += dotnetLogger.Error;

            if (!restoreProcess.Start())
            {
                Logger.LogError("Failed to start dotnet build process");
                return false;
            }

            restoreProcess.BeginErrorReadLine();
            restoreProcess.BeginOutputReadLine();

            // The rebuild project can take a while, and we need to wait until it finishes
            // Calculate how many iterations we need based on the compile process timeout
            const int checkIntervalMs = 100; // Check every 100ms
            var maxRetries = settings.CompileProcessTimeout / checkIntervalMs;

            var waitRetry = 0;
            while (!restoreProcess.HasExited && waitRetry++ < maxRetries)
                Thread.Sleep(checkIntervalMs);

            // If the process has not finished within the timeout period, we kill it manually
            if (!restoreProcess.HasExited)
            {
                Logger.LogError($"`dotnet build` did not complete within the configured timeout of {settings.CompileProcessTimeout}ms.");
                Logger.LogError(
                    $"""

                     ╔═══════════════════════ dotnet build TIMEOUT ═════════════════════════════════════════════════════════════════════════╗

                       `dotnet build` did not complete within the configured timeout of {settings.CompileProcessTimeout}ms.

                       Possible reasons:
                       - Your project may be large or complex, requiring more time to build
                       - Your system may be under heavy load or has limited resources

                       ACTION REQUIRED:
                       To increase the compilation timeout, set the 'CompileProcessTimeout' property in your GdUnit4 settings.

                       Add or modify the following in your .runsettings file:
                       <GdUnit4>
                           <CompileProcessTimeout>60000</CompileProcessTimeout>  <!-- 60 seconds -->
                       </GdUnit4>

                       The process will now be forcefully terminated, which may result in incomplete compilation.

                     ╚══════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
                     """);

                restoreProcess.Kill(true);
                _ = restoreProcess.WaitForExit(5000);
            }

            if (restoreProcess.ExitCode == 0)
            {
                Logger.LogInfo("dotnet build completed successfully");
                return true;
            }

            Logger.LogError($"dotnet build failed with exit code: {restoreProcess.ExitCode}");
            return false;
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Logger.LogError($"Error: restore: {ex.Message}");
            return false;
        }
    }

    private void CollectStdErr(object sender, DataReceivedEventArgs args)
    {
        if (args.Data == null)
        {
            _ = stdErrClosed.TrySetResult();
            return;
        }

        stdErrTail.Add(args.Data);
    }

    private void ReportProcessExit(int exitCode)
    {
        var (level, message) = DescribeProcessExit(
            exitCode,
            IsRunCompleted,
            settings.TolerateExitCodeAfterCompletion,
            OperatingSystem.IsWindows(),
            stdErrTail.ToArray());
        switch (level)
        {
            case LogLevel.Error:
                Logger.LogError(message);
                break;
            case LogLevel.Warning:
                Logger.LogWarning(message);
                break;
            default:
                Logger.LogInfo(message);
                break;
        }
    }

    private EventHandler ExitHandler(string source) => (sender, _) =>
    {
        Console.Out.Flush();
        if (sender is Process p)
        {
            if (p.ExitCode == 0)
                Logger.LogInfo($"{source} ends with exit code: {p.ExitCode}");
            else
                Logger.LogError($"{source} ends with exit code: {p.ExitCode}");
        }
    };

    private void CloseProcess(Process? processToClose)
    {
        if (processToClose == null)
            return;
        try
        {
            processToClose.CancelErrorRead();
            processToClose.CancelOutputRead();
            processToClose.Dispose();
        }
#pragma warning disable CA1031
        catch (Exception)
#pragma warning restore CA1031
        {
            // ignore
        }
        finally
        {
            lock (ProcessLock)
                process = null;
        }
    }

    /// <summary>
    ///     Cleans up the installed runner file when compilation fails to ensure
    ///     a fresh installation on the next run.
    /// </summary>
    /// <param name="runnerFilePath">Path to the runner file to remove.</param>
    private void CleanupRunnerOnFailure(string runnerFilePath)
    {
        try
        {
            if (File.Exists(runnerFilePath))
            {
                Logger.LogInfo($"Cleaning up runner file at {runnerFilePath} due to compilation failure");
                File.Delete(runnerFilePath);
            }
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Logger.LogError($"Failed to clean up runner file: {ex.Message}");

            // We don't want to throw here as this is just cleanup
        }
    }
}
