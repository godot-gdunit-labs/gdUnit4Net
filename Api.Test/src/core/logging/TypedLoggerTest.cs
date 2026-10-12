// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Tests.Core.Logging;

using Api;
using GdUnit4.Core.Logging;
using static GdUnit4.Assertions;

[TestSuite]
public class TypedLoggerTest
{
    // Subject owns its logger, mirroring real production code.
    private class Subject
    {
        internal static readonly ITestEngineLogger Logger = LoggerFactory.GetLogger<Subject>();
    }

    #region Capture routing

    [TestCase]
    public void Watch_CapturesErrorAtOriginalLevel()
    {
        using var capture = LogCapture.Watch<Subject>();

        Subject.Logger.LogError("boom");

        AssertThat(capture.EntriesOf(LogLevel.Error).Select(e => e.Message))
            .ContainsExactly("boom");
    }

    [TestCase]
    public void Watch_CapturesWarningAtOriginalLevel()
    {
        using var capture = LogCapture.Watch<Subject>();

        Subject.Logger.LogWarning("caution");

        AssertThat(capture.EntriesOf(LogLevel.Warning).Select(e => e.Message))
            .ContainsExactly("caution");
    }

    [TestCase]
    public void Watch_CapturesInformationalAtOriginalLevel()
    {
        using var capture = LogCapture.Watch<Subject>();

        Subject.Logger.LogInfo("info");

        AssertThat(capture.EntriesOf(LogLevel.Informational).Select(e => e.Message))
            .ContainsExactly("info");
    }

    [TestCase]
    public void Watch_DoesNotCaptureMessagesAfterDispose()
    {
        LogCapture capture;
        using (capture = LogCapture.Watch<Subject>())
            Subject.Logger.LogError("captured");

        Subject.Logger.LogError("not captured");

        AssertThat(capture.EntriesOf(LogLevel.Error).Select(e => e.Message))
            .ContainsExactly("captured");
    }

    [TestCase]
    public void Watch_DoesNotCaptureMessagesFromOtherSources()
    {
        using var capture = LogCapture.Watch<Subject>();

        LoggerFactory.GetLogger<TypedLoggerTest>().LogError("unrelated");

        AssertThat(capture.Entries).IsEmpty();
    }

    #endregion
}
