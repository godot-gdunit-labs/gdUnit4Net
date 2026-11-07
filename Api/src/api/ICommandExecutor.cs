// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Api;

using System;
using System.Threading;
using System.Threading.Tasks;

using Core.Commands;

/// <summary>
///     Defines an interface for executing test commands asynchronously.
///     Provides functionality to start/stop the executor and execute test commands with event handling.
/// </summary>
public interface ICommandExecutor : IAsyncDisposable
{
    /// <summary>
    ///     Starts the command executor asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync();

    /// <summary>
    ///     Stops the command executor asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync();

    /// <summary>
    ///     Executes a test command with event handling and cancellation support.
    /// </summary>
    /// <typeparam name="T">The type of command to execute, must inherit from BaseCommand.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="testEventListener">The listener for test events.</param>
    /// <param name="cancellationToken">Token to support cancellation of the operation.</param>
    /// <returns>A task containing the command execution response.</returns>
    Task<Response> ExecuteCommand<T>(T command, ITestEventListener testEventListener, CancellationToken cancellationToken)
        where T : BaseCommand;
}
