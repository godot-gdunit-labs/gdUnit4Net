// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution.Exceptions;

using System.Diagnostics;

#pragma warning disable CA1064
internal sealed class ExecutionTimeoutException : TestFailedException
#pragma warning restore CA1064
{
    public ExecutionTimeoutException()
        : base("Execution timed out", new StackTrace(true))
    {
    }

    public ExecutionTimeoutException(string message)
        : base(message, new StackTrace(true))
    {
    }

    public ExecutionTimeoutException(string message, StackTrace stackTrace)
        : base(message, stackTrace)
    {
    }

    public ExecutionTimeoutException(string message, Exception innerException)
        : base(message, innerException)
        => SetCurrentStackTrace(innerException);

    public ExecutionTimeoutException(string message, int line)
        : base(message)
        => LineNumber = line;

    [StackTraceHidden]
    private void SetCurrentStackTrace(Exception innerException)
    {
        LineNumber = innerException is ExecutionTimeoutException ete ? ete.LineNumber : -1;
        OriginalStackTrace = innerException.StackTrace;
    }
}
