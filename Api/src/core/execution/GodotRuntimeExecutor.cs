// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution;

using System.IO.Pipes;
using System.Net;
using System.Security.Principal;

using Api;

using Commands;

using Logging;

using Reporting;

using Runners;

using static Api.ReportType;

/// <summary>
///     Implements a command executor that communicates with the Godot runtime through named pipes.
///     Handles test command execution, event processing, and interprocess communication with the Godot engine.
/// </summary>
/// <remarks>
///     This executor uses a named pipe for bidirectional communication with the Godot process.
///     It manages the connection lifecycle and handles command execution with event propagation.
/// </remarks>
internal sealed class GodotRuntimeExecutor : InOutPipeProxy<NamedPipeClientStream>, ICommandExecutor
{
    private static new readonly ITestEngineLogger Logger = LoggerFactory.GetLogger<GodotRuntimeExecutor>();

    private readonly int connectTimeout;

    public GodotRuntimeExecutor(string pipeName, int connectTimeout)
        : base(new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous, TokenImpersonationLevel.Impersonation), Logger)
        => this.connectTimeout = connectTimeout;

    public async Task StartAsync()
    {
        try
        {
            await Proxy
                .ConnectAsync(connectTimeout)
                .ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Logger.LogError($"Starting GodotRuntimeExecutor failed.\n\t {e.Message}");
            throw;
        }
    }

    public async Task StopAsync()
    {
        try
        {
            _ = await ExecuteCommand(new TerminateGodotInstanceCommand(), new NoInteractTestEventListener(), CancellationToken.None)
                .ConfigureAwait(true);

            // Give server time to process shutdown
            await Task
                .Delay(100)
                .ConfigureAwait(true);
            await DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Logger.LogError($"Stop GodotRuntimeExecutor failed.\n{e}");
            throw;
        }
    }

    public async Task<Response> ExecuteCommand<T>(T command, ITestEventListener testEventListener, CancellationToken cancellationToken)
        where T : BaseCommand
    {
        try
        {
            if (!IsConnected)
                throw new InvalidOperationException("Client is not connected");

            // do not run the command if cancellation requested
            cancellationToken.ThrowIfCancellationRequested();

            // commit command
            await WriteCommand(command)
                .ConfigureAwait(false);
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return new Response
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Payload = ExceptionPayload.Serialize(ex)
            };
        }

        // read incoming data until is command response or canceled
        TestEvent? lastTestEvent = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var data = await ReadInData(cancellationToken)
                    .ConfigureAwait(false);
                switch (data)
                {
                    case TestEvent testEvent:
                        // save last event to be used for test cancellation report
                        lastTestEvent = testEvent;
                        testEventListener.PublishEvent(testEvent);
                        break;
                    case Response response:
                        if (response.StatusCode != HttpStatusCode.Gone || lastTestEvent == null)
                            return response;

                        // if connection gone we report at interrupted to the actual test
                        var testCanceledEvent = TestEvent
                            .AfterTest(lastTestEvent.Id, lastTestEvent.ResourcePath, lastTestEvent.SuiteName, lastTestEvent.TestName)
                            .WithStatistic(TestEvent.StatisticKey.Errors, 1)
                            .WithReport(new TestReport(Interrupted, 0, response.Payload));
                        testEventListener.PublishEvent(testCanceledEvent);
                        return response;
                    default:
                        continue;
                }
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                return new Response
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Payload = ExceptionPayload.Serialize(ex)
                };
            }
        }

        return new Response
        {
            StatusCode = HttpStatusCode.InternalServerError,
            Payload = string.Empty
        };
    }
}

#pragma warning disable SA1402
internal class NoInteractTestEventListener : ITestEventListener
#pragma warning restore SA1402
{
    public bool IsFailed { get; set; }

    public int CompletedTests { get; set; }

    public void PublishEvent(ITestEvent testEvent)
    {
    }
}
