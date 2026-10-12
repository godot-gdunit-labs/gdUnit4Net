// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Tests.Core.Logging;

using Api;
using GdUnit4.Core.Logging;
using static GdUnit4.Assertions;

[TestSuite]
public class LogCaptureTest
{
    #region Log-source stubs

    // Each class owns its logger, mirroring real production code.
    private class SourceA
    {
        internal static readonly ITestEngineLogger Logger = LoggerFactory.GetLogger<SourceA>();
    }

    private class SourceB
    {
        internal static readonly ITestEngineLogger Logger = LoggerFactory.GetLogger<SourceB>();
    }

    private class SourceC
    {
        internal static readonly ITestEngineLogger Logger = LoggerFactory.GetLogger<SourceC>();
    }

    #endregion

    #region Watch

    [TestCase]
    public void Watch_SingleType_CapturesMessagesForThatType()
    {
        using var capture = LogCapture.Watch<SourceA>();

        SourceA.Logger.LogInfo("hello");

        AssertThat(capture.Entries)
            .ContainsExactly(new LogEntry(LogLevel.Informational, "hello", typeof(SourceA)));
    }

    [TestCase]
    public void Watch_DoesNotCaptureMessagesForUnregisteredType()
    {
        using var capture = LogCapture.Watch<SourceA>();

        SourceB.Logger.LogInfo("unrelated");

        AssertThat(capture.Entries).IsEmpty();
    }

    [TestCase]
    public void Watch_TwoTypes_CapturesBothSources()
    {
        using var capture = LogCapture.Watch<SourceA, SourceB>();

        SourceA.Logger.LogInfo("from-A");
        SourceB.Logger.LogInfo("from-B");

        AssertThat(capture.Entries)
            .ContainsExactly(
                new LogEntry(LogLevel.Informational, "from-A", typeof(SourceA)),
                new LogEntry(LogLevel.Informational, "from-B", typeof(SourceB)));
    }

    [TestCase]
    public void Watch_ThreeTypes_CapturesAllThreeSources()
    {
        using var capture = LogCapture.Watch<SourceA, SourceB, SourceC>();

        SourceA.Logger.LogInfo("A");
        SourceB.Logger.LogInfo("B");
        SourceC.Logger.LogInfo("C");

        AssertThat(capture.Entries)
            .ContainsExactly(
                new LogEntry(LogLevel.Informational, "A", typeof(SourceA)),
                new LogEntry(LogLevel.Informational, "B", typeof(SourceB)),
                new LogEntry(LogLevel.Informational, "C", typeof(SourceC)));
    }

    [TestCase]
    public void Watch_ParamsOverload_CapturesAllSpecifiedTypes()
    {
        using var capture = LogCapture.Watch(typeof(SourceA), typeof(SourceB), typeof(SourceC));

        SourceA.Logger.LogInfo("A");
        SourceB.Logger.LogInfo("B");
        SourceC.Logger.LogInfo("C");

        AssertThat(capture.Entries)
            .ContainsExactly(
                new LogEntry(LogLevel.Informational, "A", typeof(SourceA)),
                new LogEntry(LogLevel.Informational, "B", typeof(SourceB)),
                new LogEntry(LogLevel.Informational, "C", typeof(SourceC)));
    }

    #endregion

    #region Dispose

    [TestCase]
    public void Dispose_RemovesCaptureFromRegistry()
    {
        var capture = LogCapture.Watch<SourceA>();

        capture.Dispose();

        AssertThat(LogCapture.GetCaptures(typeof(SourceA))).IsEmpty();
    }

    [TestCase]
    public void Dispose_RemovesOnlyTheDisposedCapture_WhenTwoCapturesAreActive()
    {
        var captureA = LogCapture.Watch<SourceA>();
        var captureB = LogCapture.Watch<SourceA>();

        captureA.Dispose();

        AssertThat(LogCapture.GetCaptures(typeof(SourceA)))
            .ContainsExactly(captureB);

        captureB.Dispose();
    }

    [TestCase]
    public void Dispose_StopsCapturingMessages()
    {
        LogCapture capture;
        using (capture = LogCapture.Watch<SourceA>())
        {
            SourceA.Logger.LogInfo("inside");
        }

        SourceA.Logger.LogInfo("outside");

        AssertThat(capture.Entries)
            .ContainsExactly(new LogEntry(LogLevel.Informational, "inside", typeof(SourceA)));
    }

    [TestCase]
    public void Dispose_IsIdempotent_DoesNotThrow()
    {
        var capture = LogCapture.Watch<SourceA>();

        capture.Dispose();
        capture.Dispose();

        AssertThat(LogCapture.GetCaptures(typeof(SourceA))).IsEmpty();
    }

    #endregion

    #region Entries

    [TestCase]
    public void Entries_ReturnsEntriesInEmissionOrder()
    {
        using var capture = LogCapture.Watch<SourceA>();

        SourceA.Logger.LogInfo("first");
        SourceA.Logger.LogWarning("second");
        SourceA.Logger.LogError("third");

        AssertThat(capture.Entries)
            .ContainsExactly(
                new LogEntry(LogLevel.Informational, "first", typeof(SourceA)),
                new LogEntry(LogLevel.Warning, "second", typeof(SourceA)),
                new LogEntry(LogLevel.Error, "third", typeof(SourceA)));
    }

    [TestCase]
    public void Entries_IsEmptyWhenNothingLogged()
    {
        using var capture = LogCapture.Watch<SourceA>();

        AssertThat(capture.Entries).IsEmpty();
    }

    #endregion

    #region Clear

    [TestCase]
    public void Clear_RemovesAllCapturedEntries()
    {
        using var capture = LogCapture.Watch<SourceA>();

        SourceA.Logger.LogInfo("before");
        capture.Clear();

        AssertThat(capture.Entries).IsEmpty();
    }

    [TestCase]
    public void Clear_AllowsCapturingNewEntriesAfterReset()
    {
        using var capture = LogCapture.Watch<SourceA>();

        SourceA.Logger.LogInfo("before");
        capture.Clear();
        SourceA.Logger.LogInfo("after");

        AssertThat(capture.Entries)
            .ContainsExactly(new LogEntry(LogLevel.Informational, "after", typeof(SourceA)));
    }

    #endregion

    #region Count

    [TestCase]
    public void Count_ReturnsCorrectCountPerLevel()
    {
        using var capture = LogCapture.Watch<SourceA>();

        SourceA.Logger.LogInfo("info");
        SourceA.Logger.LogWarning("warning");
        SourceA.Logger.LogError("error");

        AssertThat(capture.Count(LogLevel.Informational)).IsEqual(1);
        AssertThat(capture.Count(LogLevel.Warning)).IsEqual(1);
        AssertThat(capture.Count(LogLevel.Error)).IsEqual(1);
    }

    [TestCase]
    public void Count_ReturnsZeroForLevelWithNoMessages()
    {
        using var capture = LogCapture.Watch<SourceA>();

        SourceA.Logger.LogInfo("only info");

        AssertThat(capture.Count(LogLevel.Warning)).IsEqual(0);
        AssertThat(capture.Count(LogLevel.Error)).IsEqual(0);
    }

    [TestCase]
    public void Count_AccumulatesAcrossMultipleEmissions()
    {
        using var capture = LogCapture.Watch<SourceA>();

        SourceA.Logger.LogInfo("a");
        SourceA.Logger.LogInfo("b");
        SourceA.Logger.LogInfo("c");

        AssertThat(capture.Count(LogLevel.Informational)).IsEqual(3);
    }

    #endregion

    #region EntriesOf

    [TestCase]
    public void EntriesOf_ReturnsOnlyMatchingLevel()
    {
        using var capture = LogCapture.Watch<SourceA>();

        SourceA.Logger.LogInfo("info");
        SourceA.Logger.LogWarning("warning");
        SourceA.Logger.LogError("error");

        AssertThat(capture.EntriesOf(LogLevel.Warning))
            .ContainsExactly(new LogEntry(LogLevel.Warning, "warning", typeof(SourceA)));
    }

    [TestCase]
    public void EntriesOf_ReturnsEmptyListWhenNoneMatch()
    {
        using var capture = LogCapture.Watch<SourceA>();

        SourceA.Logger.LogInfo("info only");

        AssertThat(capture.EntriesOf(LogLevel.Error)).IsEmpty();
    }

    [TestCase]
    public void EntriesOf_RetainsEmissionOrder()
    {
        using var capture = LogCapture.Watch<SourceA>();

        SourceA.Logger.LogWarning("w1");
        SourceA.Logger.LogInfo("skip");
        SourceA.Logger.LogWarning("w2");

        AssertThat(capture.EntriesOf(LogLevel.Warning))
            .ContainsExactly(
                new LogEntry(LogLevel.Warning, "w1", typeof(SourceA)),
                new LogEntry(LogLevel.Warning, "w2", typeof(SourceA)));
    }

    #endregion

    #region Parallel captures for the same type

    [TestCase]
    public void TwoCaptures_ForSameType_BothReceiveMessages()
    {
        using var captureA = LogCapture.Watch<SourceA>();
        using var captureB = LogCapture.Watch<SourceA>();

        SourceA.Logger.LogInfo("shared");

        AssertThat(captureA.Entries)
            .ContainsExactly(new LogEntry(LogLevel.Informational, "shared", typeof(SourceA)));
        AssertThat(captureB.Entries)
            .ContainsExactly(new LogEntry(LogLevel.Informational, "shared", typeof(SourceA)));
    }

    [TestCase]
    public void DisposingOneCapture_DoesNotAffectOther()
    {
        var captureA = LogCapture.Watch<SourceA>();
        using var captureB = LogCapture.Watch<SourceA>();

        captureA.Dispose();
        SourceA.Logger.LogInfo("after-dispose");

        AssertThat(captureA.Entries).IsEmpty();
        AssertThat(captureB.Entries)
            .ContainsExactly(new LogEntry(LogLevel.Informational, "after-dispose", typeof(SourceA)));
    }

    #endregion

    #region LogEntry record

    [TestCase]
    public void LogEntry_EqualWhenAllFieldsMatch()
    {
        var a = new LogEntry(LogLevel.Warning, "oops", typeof(SourceA));
        var b = new LogEntry(LogLevel.Warning, "oops", typeof(SourceA));
        AssertThat(a).IsEqual(b);
    }

    [TestCase]
    public void LogEntry_NotEqualWhenLevelDiffers()
    {
        var a = new LogEntry(LogLevel.Warning, "msg", typeof(SourceA));
        var b = new LogEntry(LogLevel.Error, "msg", typeof(SourceA));
        AssertThat(a).IsNotEqual(b);
    }

    [TestCase]
    public void LogEntry_NotEqualWhenMessageDiffers()
    {
        var a = new LogEntry(LogLevel.Informational, "msg-a", typeof(SourceA));
        var b = new LogEntry(LogLevel.Informational, "msg-b", typeof(SourceA));
        AssertThat(a).IsNotEqual(b);
    }

    [TestCase]
    public void LogEntry_NotEqualWhenSourceDiffers()
    {
        var a = new LogEntry(LogLevel.Informational, "msg", typeof(SourceA));
        var b = new LogEntry(LogLevel.Informational, "msg", typeof(SourceB));
        AssertThat(a).IsNotEqual(b);
    }

    #endregion
}
