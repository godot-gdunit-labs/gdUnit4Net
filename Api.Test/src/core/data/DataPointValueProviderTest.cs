namespace GdUnit4.Tests.Core.Data;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using GdUnit4.Core.Data;
using GdUnit4.Core.Execution;

using Godot;

using Tests.Core.Discovery;

using static Assertions;

/// <summary>
///     Provides async data points for <see cref="DataPointValueProviderTest" />.
///     It is not a test suite, so its test case is only used as input and not discovered.
/// </summary>
public sealed class AsyncDataPointSource
{
    // The expected source line numbers in DataPointValueProviderTest refer to the methods below, keep them in place.
    public static async IAsyncEnumerable<object?[]> BlockedDataPoint()
    {
        await Task.Delay(1000);
        yield return [1, 2, 3];
    }

    [TestCase]
    [DataPoint(nameof(BlockedDataPoint))]
    public void OnBlockedDataPoint(int a, int b, int expected) => AssertThat(a + b).IsEqual(expected);
}

[TestSuite]
public class DataPointValueProviderTest
{
    private const string SOURCE_FILE = "src/core/data/DataPointValueProviderTest.cs";

    #region async data point timeout source location

    [TestCase]
    public void BuildStackTraceNamesSourceLocationFromDebugSymbols()
    {
        var expectedFile = DiscoverTestUtils.GetSourceFilePath(SOURCE_FILE);

        AssertThat(DataPointValueProvider.BuildStackTrace(AsyncDataPointSource.BlockedDataPoint()))
            .IsEqual($"at GdUnit4.Tests.Core.Data.AsyncDataPointSource.BlockedDataPoint() in {expectedFile}:line 25");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void BuildStackTraceNamesSourceLocationInGodotRuntime()
    {
        // Godot loads the test assembly from memory, the debug symbols are read from the Godot build directory
        var expectedFile = Path.GetFullPath(ProjectSettings.GlobalizePath($"res://{SOURCE_FILE}"));

        AssertThat(DataPointValueProvider.BuildStackTrace(AsyncDataPointSource.BlockedDataPoint()))
            .IsEqual($"at GdUnit4.Tests.Core.Data.AsyncDataPointSource.BlockedDataPoint() in {expectedFile}:line 25");
    }

    [TestCase]
    public void BuildStackTraceOnNoStateMachine()
        => AssertThat(DataPointValueProvider.BuildStackTrace(new List<object?[]>()))
            .IsEqual("at unknown location");

    [TestCase]
    public async Task TimeoutReportsSourceLocationOfDataPoint()
    {
        var expectedFile = DiscoverTestUtils.GetSourceFilePath(SOURCE_FILE);
        var testCase = new TestCase(Guid.NewGuid(), typeof(AsyncDataPointSource).GetMethod(nameof(AsyncDataPointSource.OnBlockedDataPoint))!, 32, 0);

        (await AssertThrown(ConsumeAll(DataPointValueProvider.GetDataAsync(testCase, TimeSpan.FromMilliseconds(100)))))!
            .IsInstanceOf<AsyncDataPointCanceledException>()
            .HasMessage("The execution has timed out after 100ms.")
            .HasPropertyValue("StackTrace", $"at GdUnit4.Tests.Core.Data.AsyncDataPointSource.BlockedDataPoint() in {expectedFile}:line 25");
    }

    #endregion

    private static async Task ConsumeAll(IAsyncEnumerable<object?[]> data)
    {
        // consume all values to run the data point until its end or timeout
        await using var enumerator = data.GetAsyncEnumerator();
        while (await enumerator.MoveNextAsync())
        {
        }
    }
}
