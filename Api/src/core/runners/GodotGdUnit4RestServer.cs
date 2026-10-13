// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Runners;

using System.IO.Pipes;
using System.Net;

using Api;

using Commands;

using Extensions;

using Godot;

using Newtonsoft.Json;

internal sealed class GodotGdUnit4RestServer : InOutPipeProxy<NamedPipeServerStream>, ITestEventListener
{
    private readonly SemaphoreSlim processLock = new(1, 1);

    // set once the client requested the termination, so no further command is read while the engine shuts down
    private volatile bool isTerminating;

    // set once the termination response is sent, no pipe operation is pending from then on
    private volatile bool isTerminated;

    public GodotGdUnit4RestServer(ITestEngineLogger logger, string pipeName)
        : base(new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous), logger)
    {
    }

    public bool IsFailed { get; set; }

    public int CompletedTests { get; set; }

    public void PublishEvent(ITestEvent testEvent)
        => Task.Run(async () => await WriteAsync(testEvent)
                .ConfigureAwait(false))
            .Wait();

    public new async ValueTask DisposeAsync()
    {
        Logger.LogInfo("Closing GdUnit4 RestApi.");
        processLock.Dispose();
        await base
            .DisposeAsync()
            .ConfigureAwait(false);
    }

    public async Task Process()
    {
        if (isTerminating || !IsConnected)
            return;

        _ = await GodotObjectExtensions.SyncProcessFrame;
        if (!await processLock
                .WaitAsync(TimeSpan.FromSeconds(1))
                .ConfigureAwait(true))
            return;

        try
        {
            if (isTerminating)
                return;

            using CancellationTokenSource tokenSource = new(TimeSpan.FromMinutes(10));
            if (tokenSource.Token.IsCancellationRequested)
            {
                Logger.LogWarning("GodotGdUnit4RestApi:: Operation timed out.");
                return;
            }

            var command = await ReadCommand<BaseCommand>(tokenSource.Token)
                .ConfigureAwait(true);

            // stop reading before the response is sent, the client disconnects as soon as it is received
            isTerminating = command is TerminateGodotInstanceCommand;
            var response = await ProcessCommand(command, this)
                .ConfigureAwait(true);
            await WriteResponse(response)
                .ConfigureAwait(true);

            // quit only after the response is sent, the runner node then closes the pipe while it is deleted
            if (isTerminating)
            {
                isTerminated = true;
                (Engine.GetMainLoop() as SceneTree)?.Quit();
            }
        }
        catch (IOException e)
        {
            Logger.LogError($"GodotGdUnit4RestApi:: Client has disconnected by '{e.Message}'");
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Logger.LogError($"GodotGdUnit4RestApi:: {ex.Message} \n{ex.StackTrace}");
        }
        finally
        {
            _ = processLock.Release();
        }
    }

    internal async Task Start()
    {
        Logger.LogInfo("GodotGdUnit4RestApi:: Waiting for client connecting.");
        await Proxy
            .WaitForConnectionAsync()
            .ConfigureAwait(false);
        Logger.LogInfo($"GodotGdUnit4RestApi:: Client connected. User:{Proxy.GetImpersonationUserName()}");
    }

    /// <summary>
    ///     Closes the pipe on the calling thread. It is called while the engine deletes the runner node,
    ///     so the pipe is closed before the engine and the .NET runtime shut down, not by a background task during the shutdown.
    /// </summary>
    internal void Stop()
    {
        isTerminating = true;

        // after the termination the lock can still be granted to a queued call that only returns on its next frame,
        // otherwise do not wait, a pending command continues on this thread and would never complete
        if (!isTerminated && !processLock.Wait(0))
        {
            Logger.LogWarning("GodotGdUnit4RestApi:: Stop requested but processing is in progress.");
            return;
        }

        Logger.LogInfo("Closing GdUnit4 RestApi.");
        Proxy.Dispose();
    }

    private async Task<Response> ProcessCommand(BaseCommand command, ITestEventListener testEventListener)
    {
        try
        {
            // Logger.LogInfo($"GodotGdUnit4RestApi:: Processing command {command}.");
            return await command
                .Execute(testEventListener)
                .ConfigureAwait(false);
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Logger.LogError($"GodotGdUnit4RestApi:: Processing command failed {ex.Message}.");
            return new Response
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Payload = JsonConvert.SerializeObject(ex)
            };
        }
    }
}
