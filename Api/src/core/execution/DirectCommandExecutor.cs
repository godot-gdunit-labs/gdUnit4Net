// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution;

using System;
using System.Threading;
using System.Threading.Tasks;

using Api;

using Commands;

/// <summary>
///     Implements a direct command executor that executes commands without additional runtime overhead.
///     Used for direct test execution without interprocess communication.
/// </summary>
internal class DirectCommandExecutor : ICommandExecutor
{
    public Task StartAsync() => Task.CompletedTask;

    public Task StopAsync() => Task.CompletedTask;

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    public async Task<Response> ExecuteCommand<T>(T command, ITestEventListener testEventListener, CancellationToken cancellationToken)
        where T : BaseCommand
        => await command
            .Execute(testEventListener)
            .ConfigureAwait(true);
}
