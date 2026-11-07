// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution.Exceptions;

using System.Diagnostics;
using System.Reflection;
using System.Text;

/// <summary>
///     Exception thrown when a test assertion fails or a test cannot complete successfully.
/// </summary>
/// <remarks>
///     <para>
///         This exception is specifically designed for the GdUnit4 testing framework to provide
///         enhanced stack trace information and source location details for failed tests.
///         It captures relevant stack frames while filtering out framework-internal calls.
///     </para>
///     <para>
///         The exception automatically extracts line numbers and file names from the call stack
///         to provide precise failure location information for test debugging and reporting.
///         It also supports integration with Godot's error reporting system through push_error parsing.
///     </para>
///     <para>
///         Unlike standard exceptions, this class preserves original stack trace information
///         and provides additional metadata specific to test execution context.
///     </para>
/// </remarks>
[Serializable]
public class TestFailedException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TestFailedException" /> class.
    /// </summary>
    /// <remarks>
    ///     This parameterless constructor is primarily used for serialization scenarios.
    ///     For test failures, prefer constructors that provide failure details.
    /// </remarks>
    public TestFailedException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TestFailedException" /> class with a specified error message and optional line number.
    /// </summary>
    /// <param name="message">The message that describes the test failure.</param>
    /// <param name="lineNumber">
    ///     The line number where the failure occurred. If -1 (default), the line number
    ///     will be automatically determined from the call stack.
    /// </param>
    /// <remarks>
    ///     This constructor automatically captures stack trace information from the calling context
    ///     and determines the failure location if no explicit line number is provided.
    /// </remarks>
    public TestFailedException(string message, int lineNumber = -1)
        : base(message)
    {
        LineNumber = lineNumber == -1 ? GetRootCauseLineNumber() : lineNumber;
        var frame = new StackFrame(1, true);
        var st = new StackTrace(frame);
        OriginalStackTrace = st.ToString();
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TestFailedException" /> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the test failure.</param>
    /// <param name="innerException">The exception that caused the current test failure.</param>
    /// <remarks>
    ///     Use this constructor when a test fails due to an underlying exception that should be preserved
    ///     for debugging purposes. The inner exception's stack trace will be maintained.
    /// </remarks>
    public TestFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TestFailedException" /> class with automatic stack trace filtering.
    /// </summary>
    /// <param name="message">The message that describes the test failure.</param>
    /// <remarks>
    ///     This constructor performs intelligent stack trace filtering to show only test-related frames,
    ///     excluding GdUnit4 framework internals and system calls. It stops collecting frames when
    ///     it reaches a method marked with <see cref="TestCaseAttribute" />.
    /// </remarks>
    public TestFailedException(string message)
        : this(message, new StackTrace(true))
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TestFailedException" /> class with automatic stack trace filtering.
    /// </summary>
    /// <param name="message">The message that describes the test failure.</param>
    /// <param name="stackTrace">The current stack trace.</param>
    /// <remarks>
    ///     This constructor performs intelligent stack trace filtering to show only test-related frames,
    ///     excluding GdUnit4 framework internals and system calls. It stops collecting frames when
    ///     it reaches a method marked with <see cref="TestCaseAttribute" />.
    /// </remarks>
    public TestFailedException(string message, StackTrace stackTrace)
        : base(message)
    {
        var stackFrames = new StringBuilder();
        Debug.Assert(stackTrace != null, nameof(stackTrace) + " != null");
        foreach (var frame in stackTrace.GetFrames())
        {
            var mb = frame.GetMethod();

            // we only collect test-suite related stack frames

            // skip GdUnit4 api frames and skip system api frames do only collect test relates frames
            if (mb is MethodInfo mi
                && mi.Module.Assembly != typeof(TestFailedException).Assembly)
            {
                if (frame.GetFileLineNumber() > 0)
                {
                    LineNumber = LineNumber == -1 ? frame.GetFileLineNumber() : LineNumber;
                    FileName ??= frame.GetFileName();
                    stackFrames = stackFrames.Append(new StackTrace(frame));
                }

                // end collect frames at test case attribute
                if (mi.IsDefined(typeof(TestCaseAttribute)))
                    break;
            }
        }

        OriginalStackTrace = stackFrames.ToString();
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TestFailedException" /> class with pre-parsed Godot error details.
    /// </summary>
    /// <param name="message">The error message from Godot.</param>
    /// <param name="stackFrames">The formatted stack trace information.</param>
    /// <param name="fileName">The source file name where the error occurred.</param>
    /// <param name="lineNumber">The line number where the error occurred.</param>
    /// <remarks>
    ///     This private constructor is used internally when Godot error details have already been
    ///     parsed and the stack trace, file name, and line number have been extracted.
    ///     It directly sets the exception properties without additional parsing.
    /// </remarks>
    public TestFailedException(string message, string stackFrames, string fileName, int lineNumber)
        : base(message)
    {
        OriginalStackTrace = stackFrames;
        FileName = fileName;
        LineNumber = lineNumber;
    }

    /// <summary>
    ///     Gets or sets the original stack trace information captured when the exception was created.
    /// </summary>
    public string? OriginalStackTrace { get; set; }

    /// <summary>
    ///     Gets the stack trace for this exception, preferring the original filtered stack trace if available.
    /// </summary>
    /// <value>
    ///     The filtered original stack trace if available, otherwise the standard exception stack trace.
    /// </value>
    /// <remarks>
    ///     This override provides cleaner stack traces by filtering out framework internals,
    ///     making it easier to identify the actual test failure location.
    /// </remarks>
    public override string? StackTrace => OriginalStackTrace ?? base.StackTrace;

    /// <summary>
    ///     Gets or sets the line number in the source file where the test failure occurred.
    /// </summary>
    /// <value>
    ///     The line number where the failure occurred, or -1 if the line number could not be determined.
    /// </value>
    /// <remarks>
    ///     This property is automatically populated by analyzing the call stack or can be
    ///     explicitly set through constructor parameters for precise failure location reporting.
    /// </remarks>
    public int LineNumber { get; protected set; } = -1;

    /// <summary>
    ///     Gets the name of the source file where the test failure occurred.
    /// </summary>
    /// <value>
    ///     The full path to the source file where the failure occurred, or null if not available.
    /// </value>
    public string? FileName { get; private set; }

    /// <summary>
    ///     Determines the line number of the root cause by analyzing the call stack.
    /// </summary>
    /// <returns>The line number where the failure originated, or -1 if not determinable.</returns>
    /// <remarks>
    ///     This method walks up the call stack to find the first frame that originates
    ///     from outside the GdUnit4 framework, indicating the actual test failure location.
    /// </remarks>
    private static int GetRootCauseLineNumber()
    {
        // Navigate the stack frames to find the root cause
        for (var i = 0; i <= 15; i++)
        {
            var frame = new StackFrame(i, true);

            // Check is the frame an external assembly
            if (frame.GetFileName() != null && frame.GetMethod()?.Module.Assembly != typeof(TestFailedException).Assembly)
                return frame.GetFileLineNumber();
        }

        return -1;
    }
}
