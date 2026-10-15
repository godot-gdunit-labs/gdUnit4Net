// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Commands;

using System;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Api;

/// <summary>
///     Command to safely terminate a running Godot instance.
/// </summary>
internal class TerminateGodotInstanceCommand : BaseCommand
{
    [JsonConstructor]
    public TerminateGodotInstanceCommand()
    {
    }

    /// <summary>
    ///     Executes the termination command, acknowledging the termination of the Godot instance.
    /// </summary>
    /// <remarks>
    ///     The server quits the Godot scene tree and engine after it has sent the response,
    ///     so the engine shutdown does not cut off the response.
    /// </remarks>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public override Task<Response> Execute(ITestEventListener testEventListener)
    {
        Console.WriteLine("Terminating Godot instance.");
        return Task.FromResult(
            new Response
            {
                StatusCode = HttpStatusCode.OK,
                Payload = string.Empty
            });
    }
}
