// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Commands;

using System.Net;
using System.Text.Json.Serialization;

using Api;

using Execution;

using Extensions;

/// <summary>
///     Command to execute a test suite with configurable execution options.
/// </summary>
internal class ExecuteTestSuiteCommand : BaseCommand
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ExecuteTestSuiteCommand" /> class.
    ///     Initializes a new instance of the ExecuteTestSuiteCommand.
    /// </summary>
    /// <param name="testSuite">The test suite to execute.</param>
    /// <param name="isCaptureStdOut">Whether to capture standard output during test execution.</param>
    /// <param name="isReportOrphanNodesEnabled">Whether to report orphaned nodes after test execution.</param>
    public ExecuteTestSuiteCommand(TestSuiteNode testSuite, bool isCaptureStdOut, bool isReportOrphanNodesEnabled)
    {
        Suite = testSuite;
        IsCaptureStdOut = isCaptureStdOut;
        IsReportOrphanNodesEnabled = isReportOrphanNodesEnabled;
        IsEngineMode = Suite.Tests.First().RequireRunningGodotEngine;
    }

    [JsonConstructor]
    private ExecuteTestSuiteCommand()
    {
    }

    [JsonInclude]
    private TestSuiteNode Suite { get; set; } = null!;

    [JsonInclude]
    private bool IsCaptureStdOut { get; set; }

    [JsonInclude]
    private bool IsEngineMode { get; set; }

    [JsonInclude]
    private bool IsReportOrphanNodesEnabled { get; set; }

    public override async Task<Response> Execute(ITestEventListener testEventListener)
    {
        try
        {
            var testSuite = new TestSuite(Suite);

            try
            {
                if (!IsReportOrphanNodesEnabled)
                    Console.WriteLine("Warning!!! Reporting orphan nodes is disabled. Please check GdUnit settings.");

                using ExecutionContext context = new(
                    testSuite,
                    [testEventListener],
                    IsReportOrphanNodesEnabled,
                    IsEngineMode);
                context.IsCaptureStdOut = IsCaptureStdOut;
                if (context.IsEngineMode)
                    _ = await GodotObjectExtensions.SyncProcessFrame;
                await new TestSuiteExecutionStage(testSuite)
                    .Execute(context)
                    .ConfigureAwait(true);
            }

            // handle unexpected exceptions
#pragma warning disable CA1031
            catch (Exception e)
#pragma warning restore CA1031
            {
                await Console.Error
                    .WriteLineAsync($"Unexpected Exception: {e.Message} \nStackTrace: {e.StackTrace}")
                    .ConfigureAwait(true);
            }
            finally
            {
                testSuite.Dispose();
            }

            return new Response
            {
                StatusCode = HttpStatusCode.OK,
                Payload = $"Test suite {Suite.ManagedType} executed successfully."
            };
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
}
