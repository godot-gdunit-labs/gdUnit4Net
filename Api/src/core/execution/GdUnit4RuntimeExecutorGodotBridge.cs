// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution;

using System.Net;

using Api;

using Commands;

using Extensions;

using Godot;
using Godot.Collections;

using Logging;

internal class GdUnit4RuntimeExecutorGodotBridge
{
    private TestEngineSettings Settings { get; } = new();

#pragma warning disable CA1859
    private ITestEngineLogger Logger { get; } = new GodotLogger();
#pragma warning restore CA1859

    public async Task ExecuteAsync(IReadOnlyCollection<TestSuiteNode> testSuiteNodes, Callable listener, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(
                    async () =>
                    {
                        var testListener = new GdUnit4TestEventListener(listener, Logger);

                        foreach (var testSuiteNode in testSuiteNodes)
                        {
                            var response = await new ExecuteTestSuiteCommand(testSuiteNode, Settings.CaptureStdOut, true)
                                .Execute(testListener)
                                .ConfigureAwait(true);
                            ValidateResponse(response);
                        }
                    },
                    cancellationToken)
                .WaitAsync(cancellationToken)
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

        // Godot callback boundary: the GDScript plugin starts the run and cannot receive .NET exceptions, so every failure is logged to the Godot output
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Logger.LogError($"{ex.Message}\n{ex.StackTrace}");
        }
    }

    private static void ValidateResponse(Response response)
    {
        if (response.StatusCode != HttpStatusCode.InternalServerError)
            return;
        var exception = ExceptionPayload.Deserialize(response.Payload);
        throw new InvalidOperationException("The GdUnit test server returned an unexpected status code.", exception);
    }
}

#pragma warning disable SA1402
internal class GdUnit4TestEventListener : ITestEventListener
#pragma warning restore SA1402
{
    private readonly Callable listener;
    private readonly ITestEngineLogger logger;

    internal GdUnit4TestEventListener(Callable listener, ITestEngineLogger logger)
    {
        this.listener = listener;
        this.logger = logger;
    }

    public int CompletedTests { get; set; }

    public bool IsFailed
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public void PublishEvent(ITestEvent testEvent) => EmitTestEvent(testEvent as TestEvent);

    private static Dictionary<Variant, Variant> ToGdUnitEventStatistics(IDictionary<TestEvent.StatisticKey, object> statistics)
    {
        var converted = new Dictionary<Variant, Variant>();
        foreach (var (key, value) in statistics)
            converted[GdUnitExtensions.ToSnakeCase(key.ToString()).ToVariant()] = value.ToVariant();
        return converted;
    }

    private void EmitTestEvent(TestEvent? testEvent)
    {
        if (testEvent == null)
            return;

        using var data = new Dictionary();
        try
        {
            data.Add("type", testEvent.Type.ToVariant());
            data.Add("guid", testEvent.Id.ToString());
            data.Add("resource_path", testEvent.ResourcePath.ToVariant());
            data.Add("suite_name", testEvent.SuiteName.ToVariant());
            data.Add("test_name", testEvent.TestName.ToVariant());
            data.Add("total_count", testEvent.TotalCount.ToVariant());
            data.Add("statistics", ToGdUnitEventStatistics(testEvent.Statistics));

            if (testEvent.Reports.Count != 0)
            {
                var serializedReports = testEvent.Reports.Select(report => report.Serialize().ToGodotTypedDictionary()).ToGodotArray();
                data.Add("reports", serializedReports);
            }

            _ = listener.Call(data);
        }

        // Godot callback boundary: the event is converted to Godot types and passed to the GDScript listener,
        // a failure to send one event is logged and must not stop the test run
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogError($"{ex.Message}\n{ex.StackTrace}");
        }
    }
}
