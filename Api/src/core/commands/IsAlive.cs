// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Commands;

using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Api;

/// <summary>
///     Command to check if the test engine is alive and responding.
/// </summary>
internal class IsAlive : BaseCommand
{
    [JsonConstructor]
    private IsAlive()
    {
    }

    /// <summary>
    ///     Executes a health check and returns an "alive" status response.
    /// </summary>
#pragma warning disable SA1611
    public override Task<Response> Execute(ITestEventListener testEventListener) => Task.FromResult(
        new Response
#pragma warning restore SA1611
        {
            StatusCode = HttpStatusCode.OK,
            Payload = "alive: true"
        });
}
