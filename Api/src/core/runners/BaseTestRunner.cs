// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Runners;

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Api;

using Commands;

using Logging;

using Newtonsoft.Json;

/// <summary>
///     Base implementation of a test runner that manages test execution lifecycle and command processing.
/// </summary>
internal class BaseTestRunner : ITestRunner
{
    private static readonly ITestEngineLogger Logger = LoggerFactory.GetLogger<BaseTestRunner>();

    /// <summary>
    ///     Initializes a new instance of the <see cref="BaseTestRunner" /> class.
    ///     Initializes a new instance of the BaseTestRunner.
    /// </summary>
    /// <param name="executor">The command executor for test operations.</param>
    /// <param name="settings">Test engine configuration settings.</param>
    protected BaseTestRunner(ICommandExecutor executor, TestEngineSettings settings)
    {
        Executor = executor;
        Settings = settings;
    }

    /// <summary>
    ///     Gets a value indicating whether the last run executed and reported all test suites.
    /// </summary>
    protected bool IsRunCompleted { get; private set; }

    private object SyncLock { get; } = new();

    private ICommandExecutor Executor { get; }

    private TestEngineSettings Settings { get; }

    private CancellationTokenSource? RunnerCancellationToken { get; set; }

    public async ValueTask DisposeAsync()
    {
        await Executor
            .DisposeAsync()
            .ConfigureAwait(true);
        RunnerCancellationToken?.Dispose();
        GC.SuppressFinalize(this);
    }

    public virtual void Cancel()
    {
        Logger.LogInfo("Try cancelling the test run...");
        lock (SyncLock)
            RunnerCancellationToken?.Cancel();
    }

    public void RunAndWait(List<TestSuiteNode> testSuiteNodes, ITestEventListener eventListener, CancellationToken cancellationToken)
    {
        lock (SyncLock)
            RunnerCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var token = RunnerCancellationToken.Token;
        IsRunCompleted = false;
        Task.Run(
                async () =>
                {
                    try
                    {
                        await Executor
                            .StartAsync()
                            .ConfigureAwait(true);
                        foreach (var testSuite in testSuiteNodes)
                        {
                            // using (var stdoutHook = testSuiteContext.IsCaptureStdOut ? StdOutHookFactory.CreateStdOutHook() : null)
                            var response = await Executor
                                .ExecuteCommand(new ExecuteTestSuiteCommand(testSuite, Settings.CaptureStdOut, true), eventListener, token)
                                .ConfigureAwait(true);
                            ValidateResponse(response);
                        }

                        IsRunCompleted = true;
                        await Executor
                            .StopAsync()
                            .ConfigureAwait(true);
                    }
                    catch (TimeoutException)
                    {
                        Logger.LogError("Failed to connect: Connection timeout");
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.LogInfo("Running tests are cancelled.");
                    }
#pragma warning disable CA1031
                    catch (Exception ex)
#pragma warning restore CA1031
                    {
                        Logger.LogError($"{ex.Message}\n{ex.StackTrace}");
                    }
                },
                token)
            .ContinueWith(
                _ =>
                {
                    lock (SyncLock)
                    {
                        RunnerCancellationToken?.Dispose();
                        RunnerCancellationToken = null;
                    }
                },
                TaskScheduler.Default)
            .Wait(token);
    }

    private static void ValidateResponse(Response response)
    {
        if (response.StatusCode != HttpStatusCode.InternalServerError)
            return;
        var exception = JsonConvert.DeserializeObject<Exception>(response.Payload);
        throw new InvalidOperationException("The server returned an unexpected status code.", exception);
    }
}
