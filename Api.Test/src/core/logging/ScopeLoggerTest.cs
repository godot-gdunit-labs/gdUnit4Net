// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Tests.Core.Logging;

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Api;

using GdUnit4.Core.Logging;

using static Assertions;

[TestSuite]
public class ScopeLoggerTest
{
    // TypedLogger<Inner> acts as the ScopeLogger's inner logger so LogCapture
    // can intercept the already-formatted (tagged) messages.
    private sealed class Inner;

    private readonly ITestEngineLogger logger = new ScopeLogger(LoggerFactory.GetLogger<Inner>(), "abc12345");

    #region LogInfo / LogWarning / LogError routing

    [TestCase]
    public void LogInfo_TagsMessageWithScopeId()
    {
        using var capture = LogCapture.Watch<Inner>();

        logger.LogInfo("hello");

        AssertThat(capture.EntriesOf(LogLevel.Informational).Select(e => e.Message))
            .ContainsExactly("[abc12345] hello");
    }

    [TestCase]
    public void LogWarning_TagsMessageWithScopeId()
    {
        using var capture = LogCapture.Watch<Inner>();

        logger.LogWarning("watch out");

        AssertThat(capture.EntriesOf(LogLevel.Warning).Select(e => e.Message))
            .ContainsExactly("[abc12345] watch out");
    }

    [TestCase]
    public void LogError_TagsMessageWithScopeId()
    {
        using var capture = LogCapture.Watch<Inner>();

        logger.LogError("boom");

        AssertThat(capture.EntriesOf(LogLevel.Error).Select(e => e.Message))
            .ContainsExactly("[abc12345] boom");
    }

    #endregion

    #region WithSource

    [TestCase]
    public void WithSource_IncludesSourceInTag()
    {
        using var capture = LogCapture.Watch<Inner>();
        ITestEngineLogger sourceLogger = ((ScopeLogger)logger).WithSource("Godot");

        sourceLogger.LogInfo("output");

        AssertThat(capture.EntriesOf(LogLevel.Informational).Select(e => e.Message))
            .ContainsExactly("[abc12345] Godot : output");
    }

    [TestCase]
    public void WithSource_DoesNotMutateOriginalLogger()
    {
        using var capture = LogCapture.Watch<Inner>();
        _ = ((ScopeLogger)logger).WithSource("Godot");

        logger.LogInfo("plain");

        AssertThat(capture.EntriesOf(LogLevel.Informational).Select(e => e.Message))
            .ContainsExactly("[abc12345] plain");
    }

    #endregion

    #region Output / Error process stream handlers

    [TestCase]
    public void Output_TagsMessageWithScopeId()
    {
        using var capture = LogCapture.Watch<Inner>();

        ((ScopeLogger)logger).Output(this, MakeArgs("line from stdout"));

        AssertThat(capture.EntriesOf(LogLevel.Informational).Select(e => e.Message))
            .ContainsExactly("[abc12345]/out: line from stdout");
    }

    [TestCase]
    public void Output_IgnoresNullOrWhitespaceData()
    {
        using var capture = LogCapture.Watch<Inner>();

        ((ScopeLogger)logger).Output(this, MakeArgs(null));
        ((ScopeLogger)logger).Output(this, MakeArgs("   "));

        AssertThat(capture.Entries).IsEmpty();
    }

    [TestCase]
    public void Error_TagsMessageWithScopeId()
    {
        using var capture = LogCapture.Watch<Inner>();

        ((ScopeLogger)logger).Error(this, MakeArgs("line from stderr"));

        AssertThat(capture.EntriesOf(LogLevel.Informational).Select(e => e.Message))
            .ContainsExactly("[abc12345]/err: line from stderr");
    }

    [TestCase]
    public void Error_IgnoresNullOrWhitespaceData()
    {
        using var capture = LogCapture.Watch<Inner>();

        ((ScopeLogger)logger).Error(this, MakeArgs(null));
        ((ScopeLogger)logger).Error(this, MakeArgs("   "));

        AssertThat(capture.Entries).IsEmpty();
    }

    private static DataReceivedEventArgs MakeArgs(string? data)
        => (DataReceivedEventArgs)Activator.CreateInstance(
            typeof(DataReceivedEventArgs),
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [data],
            null)!;

    #endregion

    #region ScopeId

    [TestCase]
    public void ScopeId_ReturnsConstructorValue()
        => AssertThat(((ScopeLogger)logger).ScopeId).IsEqual("abc12345");

    #endregion
}
