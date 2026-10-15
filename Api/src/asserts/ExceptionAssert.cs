// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Asserts;

using System.Diagnostics;
using System.Runtime.ExceptionServices;

using Core.Execution.Exceptions;
using Core.Extensions;

/// <inheritdoc />
public sealed class ExceptionAssert<TException> : IExceptionAssert
    where TException : Exception
{
    internal ExceptionAssert(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            action.Invoke();
        }

        // User test code boundary: the assertion captures any exception the action under test throws
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            var capturedException = ExceptionDispatchInfo.Capture(e.InnerException ?? e);
            Current = (TException)capturedException.SourceException;
        }
    }

    internal ExceptionAssert(TException e) => Current = e;

    private TException? Current { get; }

    private string? CustomFailureMessage { get; set; }

    /// <inheritdoc />
    public IExceptionAssert IsInstanceOf<TExpectedType>()
    {
        if (Current is not TExpectedType)
            ThrowTestFailureReport(AssertFailures.IsInstanceOf(Current?.GetType(), typeof(TExpectedType)));
        return this;
    }

    /// <inheritdoc />
    public IExceptionAssert HasMessage(string expected)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        expected = expected.UnixFormat();
        var current = Current?.Message.RichTextNormalize() ?? string.Empty;
        if (!current.Equals(expected, StringComparison.Ordinal))
            ThrowTestFailureReport(AssertFailures.IsEqual(current, expected));
        return this;
    }

    /// <inheritdoc />
    public IExceptionAssert HasFileLineNumber(int lineNumber)
    {
        int currentLine;
        if (Current is TestFailedException e)
            currentLine = e.LineNumber;
        else if (Current is null)
            currentLine = -1;
        else
        {
            var stackFrame = new StackTrace(Current, true).GetFrame(0);
            currentLine = stackFrame?.GetFileLineNumber() ?? -1;
        }

        if (currentLine != lineNumber)
            ThrowTestFailureReport(AssertFailures.IsEqual(currentLine, lineNumber));
        return this;
    }

    /// <inheritdoc />
    public IExceptionAssert HasFileName(string fileName)
    {
        var fullPath = Path.GetFullPath(fileName);
        string currentFileName;
        if (Current is TestFailedException e)
            currentFileName = e.FileName ?? string.Empty;
        else if (Current is null)
            currentFileName = string.Empty;
        else
        {
            var stackFrame = new StackTrace(Current, true).GetFrame(0);
            currentFileName = stackFrame?.GetFileName() ?? string.Empty;
        }

        if (!currentFileName.Equals(fullPath, StringComparison.Ordinal))
            ThrowTestFailureReport(AssertFailures.IsEqual(currentFileName, fullPath));
        return this;
    }

    /// <inheritdoc />
    public IExceptionAssert HasPropertyValue(string propertyName, object expected)
    {
        var value = Current?.GetType().GetProperty(propertyName)?.GetValue(Current);
        if (!Comparable.IsEqual(value, expected).Valid)
            ThrowTestFailureReport(AssertFailures.HasValue(propertyName, value, expected));
        return this;
    }

    /// <inheritdoc />
    public IExceptionAssert StartsWithMessage(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        value = value.UnixFormat();
        var current = Current?.Message.RichTextNormalize() ?? string.Empty;
        if (!current.StartsWith(value, StringComparison.InvariantCulture))
            ThrowTestFailureReport(AssertFailures.IsEqual(current, value));
        return this;
    }

    internal string? GetExceptionStackTrace()
    {
        if (Current is TestFailedException tfe)
            return tfe.StackTrace;
        return Current?.StackTrace ?? null;
    }

    private void ThrowTestFailureReport(string message)
    {
        var failureMessage = CustomFailureMessage ?? message;
        throw new TestFailedException(failureMessage);
    }
}
#pragma warning restore CS1591, SA1600
